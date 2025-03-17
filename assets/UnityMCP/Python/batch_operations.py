import time
import json
import logging
import traceback
import uuid
import threading
from typing import List, Dict, Any, Optional

from .error_reporter import error_reporter

def batch_execute(mcp, commands):
    """
    Execute multiple commands in a single request to improve performance.
    
    Args:
        mcp: The MCP instance
        commands (list): List of command objects, each containing 'type' and 'parameters'
        
    Returns:
        dict: Results of all executed commands
    """
    if not commands or not isinstance(commands, list):
        return {
            "error": "Invalid commands parameter. Expected a non-empty array of command objects.",
            "success": False
        }
    
    # Start timing the batch execution
    start_time = time.time()
    
    # Prepare result containers
    results = []
    errors = []
    
    # Execute each command
    for i, command in enumerate(commands):
        try:
            # Validate command format
            if not isinstance(command, dict):
                errors.append({
                    "error": f"Command at index {i} is not a valid object",
                    "command_index": i
                })
                continue
                
            if "type" not in command:
                errors.append({
                    "error": f"Command at index {i} is missing required 'type' property",
                    "command_index": i
                })
                continue
                
            command_type = command["type"]
            command_params = command.get("parameters", {})
            
            # Start timing the individual command execution
            command_start_time = time.time()
            
            # Execute the command based on its type
            # Check if the command has a subsystem prefix (e.g., "core.GetSystemInfo")
            if "." in command_type:
                subsystem, action = command_type.split(".", 1)
                subsystem = subsystem.lower()
            else:
                subsystem = "core"  # Default to core subsystem
                action = command_type
            
            # Find the appropriate handler function
            handler_fn = None
            for tool in mcp.tools:
                if tool.name == action and getattr(tool, "subsystem", "core") == subsystem:
                    handler_fn = tool.fn
                    break
            
            if not handler_fn:
                errors.append({
                    "error": f"Unknown command type: {command_type}",
                    "command_index": i
                })
                continue
            
            # Execute the command
            try:
                # Convert parameters to the expected format
                if isinstance(command_params, dict):
                    result = handler_fn(**command_params)
                else:
                    result = handler_fn(command_params)
                
                # Calculate execution time
                command_execution_time = time.time() - command_start_time
                
                # Add command info and execution time to result
                if isinstance(result, dict):
                    result["commandType"] = command_type
                    result["executionTime"] = command_execution_time
                else:
                    # Wrap non-dict results
                    result = {
                        "result": result,
                        "commandType": command_type,
                        "executionTime": command_execution_time
                    }
                
                # Add to results
                results.append(result)
                
                # Log execution
                logging.info(f"Batch executed command: {command_type} in {command_execution_time:.4f}s")
                
            except Exception as e:
                # Log the error
                logging.error(f"Error executing command {command_type}: {str(e)}")
                logging.error(traceback.format_exc())
                
                # Add to errors
                errors.append({
                    "error": f"Error executing command: {str(e)}",
                    "stack": traceback.format_exc(),
                    "command_index": i,
                    "command_type": command_type
                })
                
                # Record error in telemetry if available
                try:
                    error_reporter.report_error(
                        error_type="BatchCommandExecutionError",
                        message=str(e),
                        context={
                            "command_type": command_type,
                            "command_index": i,
                            "parameters": command_params
                        }
                    )
                except:
                    pass
        
        except Exception as e:
            # Handle any unexpected errors in the batch processing logic
            logging.error(f"Unexpected error in batch processing: {str(e)}")
            logging.error(traceback.format_exc())
            
            errors.append({
                "error": f"Unexpected error: {str(e)}",
                "stack": traceback.format_exc(),
                "command_index": i
            })
    
    # Calculate total execution time
    total_execution_time = time.time() - start_time
    
    # Return the combined results
    return {
        "results": results,
        "errors": errors,
        "commandCount": len(commands),
        "successCount": len(results),
        "errorCount": len(errors),
        "executionTime": total_execution_time,
        "success": len(errors) == 0
    }

class AsyncOperationManager:
    """Manager for asynchronous operations"""
    
    def __init__(self, mcp):
        self.mcp = mcp
        self.operations = {}
    
    def start_operation(self, command_type: str, parameters: Optional[Dict[str, Any]] = None) -> str:
        """
        Start a new asynchronous operation
        
        Args:
            command_type: The type of command to execute
            parameters: Parameters for the command
            
        Returns:
            str: The operation ID
        """
        if not command_type:
            raise ValueError("Command type is required")
        
        # Generate a unique operation ID
        operation_id = str(uuid.uuid4())
        
        # Default to empty parameters if none provided
        if parameters is None:
            parameters = {}
        
        # Store the operation
        self.operations[operation_id] = {
            "id": operation_id,
            "command_type": command_type,
            "parameters": parameters,
            "status": "pending",
            "creation_time": time.time(),
            "start_time": None,
            "completion_time": None,
            "result": None,
            "error": None,
            "progress": 0.0
        }
        
        # Start the operation in a background thread
        threading.Thread(
            target=self._execute_async_operation,
            args=(operation_id, command_type, parameters),
            daemon=True
        ).start()
        
        return operation_id
    
    def get_operation(self, operation_id: str) -> Dict[str, Any]:
        """
        Get the status of an operation
        
        Args:
            operation_id: The operation ID
            
        Returns:
            dict: The operation status
        """
        if operation_id not in self.operations:
            raise ValueError(f"No operation found with ID: {operation_id}")
        
        return self.operations[operation_id]
    
    def cancel_operation(self, operation_id: str) -> bool:
        """
        Cancel an operation
        
        Args:
            operation_id: The operation ID
            
        Returns:
            bool: True if the operation was cancelled, False otherwise
        """
        if operation_id not in self.operations:
            return False
        
        operation = self.operations[operation_id]
        
        if operation["status"] in ["pending", "running"]:
            operation["status"] = "cancelled"
            operation["completion_time"] = time.time()
            return True
        
        return False
    
    def _execute_async_operation(self, operation_id: str, command_type: str, parameters: Dict[str, Any]):
        """
        Execute an async operation in a background thread
        
        Args:
            operation_id: The operation ID
            command_type: The command type
            parameters: The command parameters
        """
        # Update operation status
        self.operations[operation_id]["status"] = "running"
        self.operations[operation_id]["start_time"] = time.time()
        
        try:
            # Check if the command has a subsystem prefix
            if "." in command_type:
                subsystem, action = command_type.split(".", 1)
                subsystem = subsystem.lower()
            else:
                subsystem = "core"  # Default to core subsystem
                action = command_type
            
            # Find the appropriate handler function
            handler_fn = None
            for tool in self.mcp.tools:
                if tool.name == action and getattr(tool, "subsystem", "core") == subsystem:
                    handler_fn = tool.fn
                    break
            
            if not handler_fn:
                raise ValueError(f"Unknown command type: {command_type}")
            
            # Simulate progress updates (for demo purposes)
            for i in range(1, 10):
                if self.operations[operation_id]["status"] == "cancelled":
                    return
                
                time.sleep(0.2)  # Simulate work
                self.operations[operation_id]["progress"] = i / 10.0
            
            # Check if the operation was cancelled
            if self.operations[operation_id]["status"] == "cancelled":
                return
            
            # Execute the command
            if isinstance(parameters, dict):
                result = handler_fn(**parameters)
            else:
                result = handler_fn(parameters)
            
            # Update operation with result
            self.operations[operation_id]["status"] = "completed"
            self.operations[operation_id]["completion_time"] = time.time()
            self.operations[operation_id]["result"] = result
            self.operations[operation_id]["progress"] = 1.0
            
        except Exception as e:
            # Update operation with error
            self.operations[operation_id]["status"] = "failed"
            self.operations[operation_id]["completion_time"] = time.time()
            self.operations[operation_id]["error"] = str(e)
            self.operations[operation_id]["progress"] = 1.0
            
            # Log the error
            logging.error(f"Error in async operation {operation_id}: {str(e)}")
            logging.error(traceback.format_exc())
            
            # Record error in telemetry if available
            try:
                error_reporter.report_error(
                    error_type="AsyncCommandExecutionError",
                    message=str(e),
                    context={
                        "operation_id": operation_id,
                        "command_type": command_type,
                        "parameters": parameters
                    }
                )
            except:
                pass
