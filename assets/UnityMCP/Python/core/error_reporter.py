"""
Unity MCP Error Reporter Module

This module provides error reporting functionality for Unity MCP.
It centralizes error handling, logging, and telemetry for errors.
"""

import json
import logging
import traceback
from typing import Dict, Any, Optional, List, Union
from datetime import datetime

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler("unity_mcp_errors.log"),
        logging.StreamHandler()
    ]
)

logger = logging.getLogger("unity_mcp")


class ErrorReporter:
    """
    Error reporter for Unity MCP.
    
    This class provides methods for reporting errors, logging them,
    and collecting telemetry data about errors.
    """
    
    def __init__(self):
        """Initialize the error reporter."""
        self.error_count = 0
        self.error_history: List[Dict[str, Any]] = []
        self.max_history_size = 100  # Maximum number of errors to keep in history
        self.telemetry_enabled = True
        
    def report_error(self, error_type: str, message: str, 
                    context: Optional[Dict[str, Any]] = None, 
                    send_telemetry: bool = True) -> Dict[str, Any]:
        """
        Report an error.
        
        Args:
            error_type: Type of the error (e.g., ValueError, ConnectionError)
            message: Error message
            context: Additional context about the error
            send_telemetry: Whether to send telemetry data about the error
            
        Returns:
            Dictionary with error information
        """
        # Get stack trace
        stack_trace = traceback.format_exc()
        
        # Create error record
        error_record = {
            "timestamp": datetime.now().isoformat(),
            "error_type": error_type,
            "message": message,
            "stack_trace": stack_trace,
            "context": context or {}
        }
        
        # Log the error
        self._log_error(error_record)
        
        # Add to history
        self._add_to_history(error_record)
        
        # Send telemetry if enabled
        if send_telemetry and self.telemetry_enabled:
            self._send_telemetry(error_record)
        
        # Increment error count
        self.error_count += 1
        
        return error_record
    
    def _log_error(self, error_record: Dict[str, Any]) -> None:
        """
        Log an error.
        
        Args:
            error_record: Error record to log
        """
        # Format the log message
        log_message = f"[{error_record['error_type']}] {error_record['message']}"
        
        # Add context if available
        if error_record["context"]:
            context_str = json.dumps(error_record["context"], indent=2)
            log_message += f"\nContext: {context_str}"
        
        # Log the error
        logger.error(log_message)
        
        # Log stack trace if available
        if error_record["stack_trace"] and error_record["stack_trace"] != "NoneType: None\n":
            logger.error(f"Stack trace:\n{error_record['stack_trace']}")
    
    def _add_to_history(self, error_record: Dict[str, Any]) -> None:
        """
        Add an error to the history.
        
        Args:
            error_record: Error record to add to history
        """
        # Add to history
        self.error_history.append(error_record)
        
        # Trim history if it exceeds the maximum size
        if len(self.error_history) > self.max_history_size:
            self.error_history = self.error_history[-self.max_history_size:]
    
    def _send_telemetry(self, error_record: Dict[str, Any]) -> None:
        """
        Send telemetry data about an error.
        
        Args:
            error_record: Error record to send telemetry for
        """
        # This is a placeholder for actual telemetry implementation
        # In a real implementation, this would send data to a telemetry service
        pass
    
    def get_error_count(self) -> int:
        """
        Get the total number of errors reported.
        
        Returns:
            Total number of errors reported
        """
        return self.error_count
    
    def get_error_history(self, limit: Optional[int] = None) -> List[Dict[str, Any]]:
        """
        Get the error history.
        
        Args:
            limit: Maximum number of errors to return
            
        Returns:
            List of error records
        """
        if limit is None:
            return self.error_history
        
        return self.error_history[-limit:]
    
    def clear_error_history(self) -> None:
        """Clear the error history."""
        self.error_history = []
    
    def enable_telemetry(self) -> None:
        """Enable telemetry."""
        self.telemetry_enabled = True
    
    def disable_telemetry(self) -> None:
        """Disable telemetry."""
        self.telemetry_enabled = False
    
    def is_telemetry_enabled(self) -> bool:
        """
        Check if telemetry is enabled.
        
        Returns:
            Whether telemetry is enabled
        """
        return self.telemetry_enabled
    
    def format_error_for_user(self, error_record: Dict[str, Any]) -> str:
        """
        Format an error record for display to the user.
        
        Args:
            error_record: Error record to format
            
        Returns:
            Formatted error message
        """
        # Basic error message
        message = f"Error: {error_record['message']}"
        
        # Add context if available and not empty
        if error_record["context"] and any(error_record["context"].values()):
            context_items = []
            for key, value in error_record["context"].items():
                context_items.append(f"{key}: {value}")
            
            context_str = ", ".join(context_items)
            message += f"\nContext: {context_str}"
        
        return message


# Create a singleton instance of the error reporter
error_reporter = ErrorReporter()
