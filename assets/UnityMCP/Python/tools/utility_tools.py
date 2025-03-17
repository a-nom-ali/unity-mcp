"""
Unity MCP Utility Tools Module

This module provides utility tools for working with Unity MCP.
These tools include functionality for:
- Scene management (loading, saving, creating new scenes)
- Project management
- Editor utilities
- Debug and development tools
- Performance monitoring
- Screenshot and recording tools
"""

import json
import os
from typing import List, Dict, Any, Optional, Union
from mcp.server.fastmcp import Context

# Import error reporting
from ..core.error_reporter import error_reporter

# Import command handler registry
from ..core.command_handler import command_handler_registry


def create_new_scene(ctx: Context, name: Optional[str] = None) -> str:
    """Create a new empty scene.
    
    Args:
        ctx: The MCP context
        name: Optional name for the new scene
        
    Returns:
        JSON string with the result of the operation
    """
    params = {}
    
    if name is not None:
        params["name"] = name
    
    # Execute the command
    result = command_handler_registry.handle_command("create_new_scene", params)
    
    return json.dumps(result)


def save_scene(ctx: Context, path: Optional[str] = None, save_as: bool = False) -> str:
    """Save the current scene.
    
    Args:
        ctx: The MCP context
        path: Optional path to save the scene to
        save_as: Whether to save as a new scene
        
    Returns:
        JSON string with the result of the operation
    """
    params = {
        "save_as": save_as
    }
    
    if path is not None:
        params["path"] = path
    
    # Execute the command
    result = command_handler_registry.handle_command("save_scene", params)
    
    return json.dumps(result)


def load_scene(ctx: Context, path: str) -> str:
    """Load a scene from a file.
    
    Args:
        ctx: The MCP context
        path: Path to the scene file
        
    Returns:
        JSON string with the result of the operation
    """
    if not path:
        error_message = "Scene path is required"
        error_reporter.report_error("ValueError", error_message, {"path": path})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("load_scene", {
        "path": path
    })
    
    return json.dumps(result)


def get_scene_info(ctx: Context) -> str:
    """Get information about the current scene.
    
    Args:
        ctx: The MCP context
        
    Returns:
        JSON string with the result of the operation
    """
    # Execute the command
    result = command_handler_registry.handle_command("get_scene_info", {})
    
    return json.dumps(result)


def take_screenshot(ctx: Context, path: str, width: int = 1920, height: int = 1080) -> str:
    """Take a screenshot of the current view.
    
    Args:
        ctx: The MCP context
        path: Path to save the screenshot to
        width: Width of the screenshot in pixels
        height: Height of the screenshot in pixels
        
    Returns:
        JSON string with the result of the operation
    """
    if not path:
        error_message = "Screenshot path is required"
        error_reporter.report_error("ValueError", error_message, {"path": path})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate width and height
    if width <= 0:
        error_message = f"Width must be greater than 0, got {width}"
        error_reporter.report_error("ValueError", error_message, {"width": width})
        return json.dumps({"error": error_message, "success": False})
    
    if height <= 0:
        error_message = f"Height must be greater than 0, got {height}"
        error_reporter.report_error("ValueError", error_message, {"height": height})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("take_screenshot", {
        "path": path,
        "width": width,
        "height": height
    })
    
    return json.dumps(result)


def start_recording(ctx: Context, path: str, width: int = 1920, height: int = 1080, 
                   framerate: int = 30, quality: int = 75) -> str:
    """Start recording the game view.
    
    Args:
        ctx: The MCP context
        path: Path to save the recording to
        width: Width of the recording in pixels
        height: Height of the recording in pixels
        framerate: Frame rate of the recording
        quality: Quality of the recording (0-100)
        
    Returns:
        JSON string with the result of the operation
    """
    if not path:
        error_message = "Recording path is required"
        error_reporter.report_error("ValueError", error_message, {"path": path})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate width and height
    if width <= 0:
        error_message = f"Width must be greater than 0, got {width}"
        error_reporter.report_error("ValueError", error_message, {"width": width})
        return json.dumps({"error": error_message, "success": False})
    
    if height <= 0:
        error_message = f"Height must be greater than 0, got {height}"
        error_reporter.report_error("ValueError", error_message, {"height": height})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate framerate
    if framerate <= 0:
        error_message = f"Framerate must be greater than 0, got {framerate}"
        error_reporter.report_error("ValueError", error_message, {"framerate": framerate})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate quality
    if quality < 0 or quality > 100:
        error_message = f"Quality must be between 0 and 100, got {quality}"
        error_reporter.report_error("ValueError", error_message, {"quality": quality})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("start_recording", {
        "path": path,
        "width": width,
        "height": height,
        "framerate": framerate,
        "quality": quality
    })
    
    return json.dumps(result)


def stop_recording(ctx: Context) -> str:
    """Stop recording the game view.
    
    Args:
        ctx: The MCP context
        
    Returns:
        JSON string with the result of the operation
    """
    # Execute the command
    result = command_handler_registry.handle_command("stop_recording", {})
    
    return json.dumps(result)


def get_editor_prefs(ctx: Context, key: str, default_value: Optional[Any] = None) -> str:
    """Get an editor preference value.
    
    Args:
        ctx: The MCP context
        key: The preference key
        default_value: Default value to return if the key doesn't exist
        
    Returns:
        JSON string with the result of the operation
    """
    if not key:
        error_message = "Preference key is required"
        error_reporter.report_error("ValueError", error_message, {"key": key})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "key": key
    }
    
    if default_value is not None:
        params["default_value"] = default_value
    
    # Execute the command
    result = command_handler_registry.handle_command("get_editor_prefs", params)
    
    return json.dumps(result)


def set_editor_prefs(ctx: Context, key: str, value: Any) -> str:
    """Set an editor preference value.
    
    Args:
        ctx: The MCP context
        key: The preference key
        value: The preference value
        
    Returns:
        JSON string with the result of the operation
    """
    if not key:
        error_message = "Preference key is required"
        error_reporter.report_error("ValueError", error_message, {"key": key})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("set_editor_prefs", {
        "key": key,
        "value": value
    })
    
    return json.dumps(result)


def get_project_settings(ctx: Context, category: str) -> str:
    """Get project settings for a specific category.
    
    Args:
        ctx: The MCP context
        category: The settings category (e.g., "Physics", "Graphics", "Input")
        
    Returns:
        JSON string with the result of the operation
    """
    if not category:
        error_message = "Settings category is required"
        error_reporter.report_error("ValueError", error_message, {"category": category})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("get_project_settings", {
        "category": category
    })
    
    return json.dumps(result)


def set_project_settings(ctx: Context, category: str, settings: Dict[str, Any]) -> str:
    """Set project settings for a specific category.
    
    Args:
        ctx: The MCP context
        category: The settings category (e.g., "Physics", "Graphics", "Input")
        settings: Dictionary of settings to set
        
    Returns:
        JSON string with the result of the operation
    """
    if not category:
        error_message = "Settings category is required"
        error_reporter.report_error("ValueError", error_message, {"category": category})
        return json.dumps({"error": error_message, "success": False})
    
    if not settings:
        error_message = "Settings dictionary is required"
        error_reporter.report_error("ValueError", error_message, {"settings": settings})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("set_project_settings", {
        "category": category,
        "settings": settings
    })
    
    return json.dumps(result)


def get_performance_stats(ctx: Context) -> str:
    """Get performance statistics.
    
    Args:
        ctx: The MCP context
        
    Returns:
        JSON string with the result of the operation
    """
    # Execute the command
    result = command_handler_registry.handle_command("get_performance_stats", {})
    
    return json.dumps(result)


def clear_console(ctx: Context) -> str:
    """Clear the Unity console.
    
    Args:
        ctx: The MCP context
        
    Returns:
        JSON string with the result of the operation
    """
    # Execute the command
    result = command_handler_registry.handle_command("clear_console", {})
    
    return json.dumps(result)


def log_message(ctx: Context, message: str, log_type: str = "info") -> str:
    """Log a message to the Unity console.
    
    Args:
        ctx: The MCP context
        message: The message to log
        log_type: The type of log (info, warning, error)
        
    Returns:
        JSON string with the result of the operation
    """
    if not message:
        error_message = "Log message is required"
        error_reporter.report_error("ValueError", error_message, {"message": message})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate log type
    valid_log_types = ["info", "warning", "error"]
    if log_type not in valid_log_types:
        error_message = f"Log type must be one of {valid_log_types}, got {log_type}"
        error_reporter.report_error("ValueError", error_message, {"log_type": log_type})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("log_message", {
        "message": message,
        "log_type": log_type
    })
    
    return json.dumps(result)


def register_utility_tools(tool_registry):
    """Register utility tools with the tool registry.
    
    Args:
        tool_registry: The tool registry to register tools with
    """
    # Create new scene
    tool_registry.register_tool(
        name="create_new_scene",
        func=create_new_scene,
        category="utility",
        description="Create a new empty scene",
        parameters={
            "name": {
                "description": "Optional name for the new scene",
                "type": "string",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Save scene
    tool_registry.register_tool(
        name="save_scene",
        func=save_scene,
        category="utility",
        description="Save the current scene",
        parameters={
            "path": {
                "description": "Optional path to save the scene to",
                "type": "string",
                "required": False
            },
            "save_as": {
                "description": "Whether to save as a new scene",
                "type": "boolean",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Load scene
    tool_registry.register_tool(
        name="load_scene",
        func=load_scene,
        category="utility",
        description="Load a scene from a file",
        parameters={
            "path": {
                "description": "Path to the scene file",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get scene info
    tool_registry.register_tool(
        name="get_scene_info",
        func=get_scene_info,
        category="utility",
        description="Get information about the current scene",
        parameters={},
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Take screenshot
    tool_registry.register_tool(
        name="take_screenshot",
        func=take_screenshot,
        category="utility",
        description="Take a screenshot of the current view",
        parameters={
            "path": {
                "description": "Path to save the screenshot to",
                "type": "string",
                "required": True
            },
            "width": {
                "description": "Width of the screenshot in pixels",
                "type": "integer",
                "required": False
            },
            "height": {
                "description": "Height of the screenshot in pixels",
                "type": "integer",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Start recording
    tool_registry.register_tool(
        name="start_recording",
        func=start_recording,
        category="utility",
        description="Start recording the game view",
        parameters={
            "path": {
                "description": "Path to save the recording to",
                "type": "string",
                "required": True
            },
            "width": {
                "description": "Width of the recording in pixels",
                "type": "integer",
                "required": False
            },
            "height": {
                "description": "Height of the recording in pixels",
                "type": "integer",
                "required": False
            },
            "framerate": {
                "description": "Frame rate of the recording",
                "type": "integer",
                "required": False
            },
            "quality": {
                "description": "Quality of the recording (0-100)",
                "type": "integer",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Stop recording
    tool_registry.register_tool(
        name="stop_recording",
        func=stop_recording,
        category="utility",
        description="Stop recording the game view",
        parameters={},
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get editor prefs
    tool_registry.register_tool(
        name="get_editor_prefs",
        func=get_editor_prefs,
        category="utility",
        description="Get an editor preference value",
        parameters={
            "key": {
                "description": "The preference key",
                "type": "string",
                "required": True
            },
            "default_value": {
                "description": "Default value to return if the key doesn't exist",
                "type": "any",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set editor prefs
    tool_registry.register_tool(
        name="set_editor_prefs",
        func=set_editor_prefs,
        category="utility",
        description="Set an editor preference value",
        parameters={
            "key": {
                "description": "The preference key",
                "type": "string",
                "required": True
            },
            "value": {
                "description": "The preference value",
                "type": "any",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get project settings
    tool_registry.register_tool(
        name="get_project_settings",
        func=get_project_settings,
        category="utility",
        description="Get project settings for a specific category",
        parameters={
            "category": {
                "description": "The settings category (e.g., 'Physics', 'Graphics', 'Input')",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set project settings
    tool_registry.register_tool(
        name="set_project_settings",
        func=set_project_settings,
        category="utility",
        description="Set project settings for a specific category",
        parameters={
            "category": {
                "description": "The settings category (e.g., 'Physics', 'Graphics', 'Input')",
                "type": "string",
                "required": True
            },
            "settings": {
                "description": "Dictionary of settings to set",
                "type": "object",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get performance stats
    tool_registry.register_tool(
        name="get_performance_stats",
        func=get_performance_stats,
        category="utility",
        description="Get performance statistics",
        parameters={},
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Clear console
    tool_registry.register_tool(
        name="clear_console",
        func=clear_console,
        category="utility",
        description="Clear the Unity console",
        parameters={},
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Log message
    tool_registry.register_tool(
        name="log_message",
        func=log_message,
        category="utility",
        description="Log a message to the Unity console",
        parameters={
            "message": {
                "description": "The message to log",
                "type": "string",
                "required": True
            },
            "log_type": {
                "description": "The type of log (info, warning, error)",
                "type": "string",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
