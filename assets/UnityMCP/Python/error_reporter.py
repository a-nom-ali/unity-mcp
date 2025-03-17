#!/usr/bin/env python3
"""
Advanced error reporting and tracking system for Unity MCP.
"""

import datetime
import json
import logging
import os
import traceback
import uuid
from typing import Dict, List, Optional

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger("unity_mcp.error_reporter")

# Create a file handler for error logs
error_log_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "logs")
os.makedirs(error_log_dir, exist_ok=True)
error_log_file = os.path.join(error_log_dir, "unity_mcp_errors.log")
file_handler = logging.FileHandler(error_log_file)
file_handler.setLevel(logging.ERROR)
file_formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
file_handler.setFormatter(file_formatter)
logger.addHandler(file_handler)


class ErrorReporter:
    """Advanced error reporting and tracking system for Unity MCP."""
    
    def __init__(self):
        self.errors = []
        self.max_errors = 100  # Maximum number of errors to keep in memory
        self.error_log_file = error_log_file
        
    def report_error(self, error_type: str, message: str, details: Dict = None, stack_trace: str = None) -> str:
        """Report an error and log it to the error log file.
        
        Args:
            error_type: Type of error (e.g., "ConnectionError", "CommandError")
            message: Error message
            details: Additional details about the error
            stack_trace: Stack trace of the error
            
        Returns:
            The error ID
        """
        # Create error record
        error_id = str(uuid.uuid4())
        timestamp = datetime.datetime.now().isoformat()
        
        error_record = {
            "id": error_id,
            "timestamp": timestamp,
            "type": error_type,
            "message": message,
            "details": details or {},
        }
        
        if stack_trace:
            error_record["stack_trace"] = stack_trace
            
        # Add to in-memory list (with size limit)
        self.errors.append(error_record)
        if len(self.errors) > self.max_errors:
            self.errors.pop(0)  # Remove oldest error
            
        # Log to file
        logger.error(f"Error {error_id}: [{error_type}] {message}")
        if details:
            logger.error(f"Details: {json.dumps(details)}")
        if stack_trace:
            logger.error(f"Stack trace: {stack_trace}")
            
        return error_id
        
    def get_recent_errors(self, limit: int = 10, error_type: str = None) -> List[Dict]:
        """Get recent errors, optionally filtered by type.
        
        Args:
            limit: Maximum number of errors to return
            error_type: Optional filter for error type
            
        Returns:
            List of error records
        """
        filtered_errors = self.errors
        if error_type:
            filtered_errors = [e for e in filtered_errors if e["type"] == error_type]
            
        return filtered_errors[-limit:]
        
    def get_error_by_id(self, error_id: str) -> Optional[Dict]:
        """Get a specific error by ID.
        
        Args:
            error_id: ID of the error to retrieve
            
        Returns:
            Error record if found, None otherwise
        """
        for error in self.errors:
            if error["id"] == error_id:
                return error
        return None
        
    def clear_errors(self):
        """Clear all errors from memory."""
        self.errors = []
        
    @staticmethod
    def format_exception(e: Exception) -> Dict:
        """Format an exception into a standardized error record.
        
        Args:
            e: Exception to format
            
        Returns:
            Formatted error record
        """
        error_type = e.__class__.__name__
        message = str(e)
        stack_trace = traceback.format_exc()
        
        return {
            "type": error_type,
            "message": message,
            "stack_trace": stack_trace
        }


# Global error reporter instance
error_reporter = ErrorReporter()
