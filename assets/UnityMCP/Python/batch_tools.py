"""
Batch and async operation tools for Unity MCP.
"""

import time
import json
import logging
import traceback
import uuid
import threading
from typing import Dict, List, Any, Optional

# Import error reporter
try:
    from .error_reporter import error_reporter
except ImportError:
    # Mock error reporter if not available
    class MockErrorReporter:
        def report_error(self, error_type, message, context=None, stack_trace=None):
            logging.error(f"{error_type}: {message}")
            return "mock-error-id"
        
        def format_exception(self, exception):
            return {
                "type": type(exception).__name__,
                "message": str(exception),
                "stack_trace": traceback.format_exc()
            }
    
    error_reporter = MockErrorReporter()

# Initialize logger
logger = logging.getLogger(__name__)

def register_batch_tools(mcp):
    """
    Register batch and async operation tools with MCP.
    
    Args:
        mcp: The MCP instance
    """
    # Create async operation manager
    if not hasattr(mcp, "_async_manager"):
        mcp._async_manager = AsyncOperationManager(mcp)
    
    # Register batch_execute tool
    @mcp.tool(
        name="batch_execute",
        description="Execute multiple commands in a single request for improved performance",
        parameters=[
            {
                "name": "commands",
                "type": "array",
                "description": "Array of command objects, each containing 'type' and 'parameters'"
            }
        ],
        returns={
            "type": "object",
            "description": "Results of all executed commands"
        }
    )
    def batch_execute(commands):
        """
        Execute multiple commands in a single request to improve performance.
        
        Args:
            commands (list): List of command objects, each containing 'type' and 'parameters'
            
        Returns:
            dict: Results of all executed commands
        """
        return _batch_execute(mcp, commands)
    
    # Register async_execute tool
    @mcp.tool(
        name="async_execute",
        description="Execute a command asynchronously without blocking",
        parameters=[
            {
                "name": "command_type",
                "type": "string",
                "description": "The type of command to execute"
            },
            {
                "name": "parameters",
                "type": "object",
                "description": "Parameters for the command"
            }
        ],
        returns={
            "type": "object",
            "description": "Operation ID and status"
        }
    )
    def async_execute(command_type, parameters=None):
        """
        Execute a command asynchronously without blocking.
        
        Args:
            command_type (str): The type of command to execute
            parameters (dict, optional): Parameters for the command
            
        Returns:
            dict: Operation ID and status
        """
        try:
            if not command_type:
                return {
                    "error": "Command type is required",
                    "success": False
                }
            
            # Start the async operation
            operation_id = mcp._async_manager.start_operation(command_type, parameters)
            
            # Return the operation ID
            return {
                "operation_id": operation_id,
                "status": "pending",
                "message": f"Async operation started for command: {command_type}",
                "success": True
            }
        except Exception as e:
            error_info = error_reporter.format_exception(e)
            error_id = error_reporter.report_error(
                error_info["type"],
                error_info["message"],
                {"command_type": command_type},
                error_info["stack_trace"]
            )
            
            logger.error(f"Error starting async operation: {str(e)}")
            return {
                "error": f"Error starting async operation: {str(e)}",
                "error_id": error_id,
                "success": False
            }
    
    # Register get_async_status tool
    @mcp.tool(
        name="get_async_status",
        description="Get the status of an asynchronous operation",
        parameters=[
            {
                "name": "operation_id",
                "type": "string",
                "description": "The ID of the operation to check"
            }
        ],
        returns={
            "type": "object",
            "description": "Operation status and result if completed"
        }
    )
    def get_async_status(operation_id):
        """
        Get the status of an asynchronous operation.
        
        Args:
            operation_id (str): The ID of the operation to check
            
        Returns:
            dict: Operation status and result if completed
        """
        try:
            if not operation_id:
                return {
                    "error": "Operation ID is required",
                    "success": False
                }
            
            # Get the operation
            operation = mcp._async_manager.get_operation(operation_id)
            
            # Calculate runtime
            runtime = 0
            if operation["start_time"]:
                end_time = operation["completion_time"] or time.time()
                runtime = end_time - operation["start_time"]
            
            # Return the operation status
            return {
                "operation": {
                    "id": operation["id"],
                    "command_type": operation["command_type"],
                    "parameters": operation["parameters"],
                    "status": operation["status"],
                    "creation_time": operation["creation_time"],
                    "start_time": operation["start_time"],
                    "completion_time": operation["completion_time"],
                    "result": operation["result"],
                    "error": operation["error"],
                    "progress": operation["progress"],
                    "runtime": runtime
                },
                "success": True
            }
        except Exception as e:
            error_info = error_reporter.format_exception(e)
            error_id = error_reporter.report_error(
                error_info["type"],
                error_info["message"],
                {"operation_id": operation_id},
                error_info["stack_trace"]
            )
            
            logger.error(f"Error getting async status: {str(e)}")
            return {
                "error": f"Error getting async status: {str(e)}",
                "error_id": error_id,
                "success": False
            }
    
    # Register cancel_async_operation tool
    @mcp.tool(
        name="cancel_async_operation",
        description="Cancel an asynchronous operation",
        parameters=[
            {
                "name": "operation_id",
                "type": "string",
                "description": "The ID of the operation to cancel"
            }
        ],
        returns={
            "type": "object",
            "description": "Success status"
        }
    )
    def cancel_async_operation(operation_id):
        """
        Cancel an asynchronous operation.
        
        Args:
            operation_id (str): The ID of the operation to cancel
            
        Returns:
            dict: Success status
        """
        try:
            if not operation_id:
                return {
                    "error": "Operation ID is required",
                    "success": False
                }
            
            # Cancel the operation
            cancelled = mcp._async_manager.cancel_operation(operation_id)
            
            if not cancelled:
                return {
                    "error": f"No running operation found with ID: {operation_id}",
                    "success": False
                }
            
            # Return success
            return {
                "operation_id": operation_id,
                "message": "Operation cancelled successfully",
                "success": True
            }
        except Exception as e:
            error_info = error_reporter.format_exception(e)
            error_id = error_reporter.report_error(
                error_info["type"],
                error_info["message"],
                {"operation_id": operation_id},
                error_info["stack_trace"]
            )
            
            logger.error(f"Error cancelling async operation: {str(e)}")
            return {
                "error": f"Error cancelling async operation: {str(e)}",
                "error_id": error_id,
                "success": False
            }
    
    # Register list_async_operations tool
    @mcp.tool(
        name="list_async_operations",
        description="List all asynchronous operations",
        parameters=[
            {
                "name": "status",
                "type": "string",
                "description": "Filter operations by status (pending, running, completed, failed, cancelled)"
            }
        ],
        returns={
            "type": "object",
            "description": "List of operations"
        }
    )
    def list_async_operations(status=None):
        """
        List all asynchronous operations.
        
        Args:
            status (str, optional): Filter operations by status
            
        Returns:
            dict: List of operations
        """
        try:
            # Get all operations
            operations = mcp._async_manager.operations
            filtered_operations = []
            
            # Filter by status if provided
            for op_id, operation in operations.items():
                if not status or operation["status"] == status:
                    # Calculate runtime
                    runtime = 0
                    if operation["start_time"]:
                        end_time = operation["completion_time"] or time.time()
                        runtime = end_time - operation["start_time"]
                    
                    # Add operation to filtered list
                    filtered_operations.append({
                        "id": operation["id"],
                        "command_type": operation["command_type"],
                        "status": operation["status"],
                        "creation_time": operation["creation_time"],
                        "progress": operation["progress"],
                        "runtime": runtime
                    })
            
            # Return the list of operations
            return {
                "operations": filtered_operations,
                "count": len(filtered_operations),
                "success": True
            }
        except Exception as e:
            error_info = error_reporter.format_exception(e)
            error_id = error_reporter.report_error(
                error_info["type"],
                error_info["message"],
                {"status": status},
                error_info["stack_trace"]
            )
            
            logger.error(f"Error listing async operations: {str(e)}")
            return {
                "error": f"Error listing async operations: {str(e)}",
                "error_id": error_id,
                "success": False
            }

def _batch_execute(mcp, commands):
    """
    Execute multiple commands in a single request.
    
    Args:
        mcp: The MCP instance
        commands: List of command objects
        
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
    
    def start_operation(self, command_type, parameters=None):
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
    
    def get_operation(self, operation_id):
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
    
    def cancel_operation(self, operation_id):
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
    
    def _execute_async_operation(self, operation_id, command_type, parameters):
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
