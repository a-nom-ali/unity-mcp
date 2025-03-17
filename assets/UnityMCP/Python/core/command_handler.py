#!/usr/bin/env python3
"""
Command handler for Unity MCP.
"""

import json
import logging
import time
from abc import ABC, abstractmethod
from typing import Any, Dict, List, Optional, Type

from ..connection import get_unity_connection
from ..error_reporter import error_reporter
from .config import config

# Initialize logger
logger = logging.getLogger("unity_mcp.command_handler")


class CommandHandler(ABC):
    """Base class for command handlers."""
    
    @abstractmethod
    def handle(self, command_type: str, parameters: Dict) -> Dict:
        """Handle a command.
        
        Args:
            command_type: The type of command to handle
            parameters: The parameters for the command
            
        Returns:
            The result of the command
        """
        pass


class UnityCommandHandler(CommandHandler):
    """Command handler for Unity commands."""
    
    def __init__(self):
        """Initialize the Unity command handler."""
        self.connection = get_unity_connection(
            host=config.connection.host,
            port=config.connection.port
        )
        self.command_cache = {}
        
    def handle(self, command_type: str, parameters: Dict) -> Dict:
        """Handle a Unity command.
        
        Args:
            command_type: The type of command to handle
            parameters: The parameters for the command
            
        Returns:
            The result of the command
        """
        try:
            # Check if we should use cached result
            if config.performance.enable_caching and self._is_cacheable(command_type):
                cache_key = self._get_cache_key(command_type, parameters)
                if cache_key in self.command_cache:
                    cache_entry = self.command_cache[cache_key]
                    # Check if cache entry is still valid
                    if time.time() - cache_entry["timestamp"] < config.performance.cache_ttl:
                        logger.debug(f"Using cached result for {command_type}")
                        return cache_entry["result"]
            
            # Ensure we have a connection
            if not self.connection:
                self.connection = get_unity_connection(
                    host=config.connection.host,
                    port=config.connection.port
                )
                if not self.connection:
                    return {
                        "error": "Failed to connect to Unity",
                        "success": False
                    }
            
            # Send the command to Unity
            result = self.connection.send_command(command_type, parameters)
            
            # Cache the result if applicable
            if config.performance.enable_caching and self._is_cacheable(command_type) and result.get("success", False):
                cache_key = self._get_cache_key(command_type, parameters)
                self.command_cache[cache_key] = {
                    "result": result,
                    "timestamp": time.time()
                }
                
                # Limit cache size
                if len(self.command_cache) > config.performance.max_cache_size:
                    # Remove oldest entries
                    oldest_keys = sorted(
                        self.command_cache.keys(),
                        key=lambda k: self.command_cache[k]["timestamp"]
                    )[:len(self.command_cache) - config.performance.max_cache_size]
                    
                    for key in oldest_keys:
                        del self.command_cache[key]
            
            return result
        except Exception as e:
            error_info = error_reporter.format_exception(e)
            error_id = error_reporter.report_error(
                error_info["type"],
                error_info["message"],
                {"command_type": command_type, "parameters": parameters},
                error_info["stack_trace"]
            )
            
            logger.error(f"Error handling command {command_type}: {str(e)}")
            return {
                "error": f"Error handling command: {str(e)}",
                "error_id": error_id,
                "success": False
            }
    
    def _is_cacheable(self, command_type: str) -> bool:
        """Check if a command is cacheable.
        
        Args:
            command_type: The type of command
            
        Returns:
            True if the command is cacheable, False otherwise
        """
        # Only cache read-only commands
        read_only_commands = [
            "get_system_info",
            "get_scene_info",
            "get_object_info",
            "get_asset_categories",
            "search_asset_store",
            "search_polyhaven_assets",
            "get_assistant_insights",
            "get_creative_suggestions"
        ]
        
        return command_type in read_only_commands
    
    def _get_cache_key(self, command_type: str, parameters: Dict) -> str:
        """Get a cache key for a command.
        
        Args:
            command_type: The type of command
            parameters: The parameters for the command
            
        Returns:
            The cache key
        """
        return f"{command_type}:{json.dumps(parameters, sort_keys=True)}"


class LocalCommandHandler(CommandHandler):
    """Command handler for local commands that don't need to be sent to Unity."""
    
    def handle(self, command_type: str, parameters: Dict) -> Dict:
        """Handle a local command.
        
        Args:
            command_type: The type of command to handle
            parameters: The parameters for the command
            
        Returns:
            The result of the command
        """
        try:
            # Handle different local commands
            if command_type == "get_error_logs":
                return self._handle_get_error_logs(parameters)
            elif command_type == "get_error_details":
                return self._handle_get_error_details(parameters)
            elif command_type == "clear_error_logs":
                return self._handle_clear_error_logs(parameters)
            elif command_type == "generate_api_documentation":
                return self._handle_generate_api_documentation(parameters)
            else:
                return {
                    "error": f"Unknown local command: {command_type}",
                    "success": False
                }
        except Exception as e:
            error_info = error_reporter.format_exception(e)
            error_id = error_reporter.report_error(
                error_info["type"],
                error_info["message"],
                {"command_type": command_type, "parameters": parameters},
                error_info["stack_trace"]
            )
            
            logger.error(f"Error handling local command {command_type}: {str(e)}")
            return {
                "error": f"Error handling local command: {str(e)}",
                "error_id": error_id,
                "success": False
            }
    
    def _handle_get_error_logs(self, parameters: Dict) -> Dict:
        """Handle get_error_logs command.
        
        Args:
            parameters: The parameters for the command
            
        Returns:
            The result of the command
        """
        limit = parameters.get("limit", 10)
        error_type = parameters.get("error_type")
        
        errors = error_reporter.get_recent_errors(limit, error_type)
        
        return {
            "errors": errors,
            "count": len(errors),
            "success": True
        }
    
    def _handle_get_error_details(self, parameters: Dict) -> Dict:
        """Handle get_error_details command.
        
        Args:
            parameters: The parameters for the command
            
        Returns:
            The result of the command
        """
        error_id = parameters.get("error_id")
        
        if not error_id:
            return {
                "error": "Error ID is required",
                "success": False
            }
        
        error = error_reporter.get_error_by_id(error_id)
        
        if not error:
            return {
                "error": f"Error with ID {error_id} not found",
                "success": False
            }
        
        return {
            "error": error,
            "success": True
        }
    
    def _handle_clear_error_logs(self, parameters: Dict) -> Dict:
        """Handle clear_error_logs command.
        
        Args:
            parameters: The parameters for the command
            
        Returns:
            The result of the command
        """
        error_reporter.clear_errors()
        
        return {
            "message": "Error logs cleared",
            "success": True
        }
    
    def _handle_generate_api_documentation(self, parameters: Dict) -> Dict:
        """Handle generate_api_documentation command.
        
        Args:
            parameters: The parameters for the command
            
        Returns:
            The result of the command
        """
        # This would be implemented in the tool_registry module
        return {
            "error": "Not implemented in command handler",
            "success": False
        }


class CommandHandlerRegistry:
    """Registry for command handlers."""
    
    def __init__(self):
        """Initialize the command handler registry."""
        self.handlers = {}
        self.default_handler = UnityCommandHandler()
        self.local_handler = LocalCommandHandler()
    
    def register_handler(self, command_type: str, handler: CommandHandler):
        """Register a handler for a command type.
        
        Args:
            command_type: The type of command
            handler: The handler for the command
        """
        self.handlers[command_type] = handler
    
    def get_handler(self, command_type: str) -> CommandHandler:
        """Get the handler for a command type.
        
        Args:
            command_type: The type of command
            
        Returns:
            The handler for the command
        """
        # Check if we have a specific handler for this command type
        if command_type in self.handlers:
            return self.handlers[command_type]
        
        # Check if this is a local command
        local_commands = [
            "get_error_logs",
            "get_error_details",
            "clear_error_logs",
            "generate_api_documentation"
        ]
        
        if command_type in local_commands:
            return self.local_handler
        
        # Use the default handler
        return self.default_handler
    
    def handle_command(self, command_type: str, parameters: Dict) -> Dict:
        """Handle a command.
        
        Args:
            command_type: The type of command
            parameters: The parameters for the command
            
        Returns:
            The result of the command
        """
        handler = self.get_handler(command_type)
        return handler.handle(command_type, parameters)


# Global command handler registry
command_handler_registry = CommandHandlerRegistry()
