#!/usr/bin/env python3
"""
Animation tools for Unity MCP.
"""

import json
import logging
from typing import Dict, List, Optional, Tuple, Union

from mcp.server.fastmcp import Context

from ..core.command_handler import command_handler_registry
from ..error_reporter import error_reporter

# Initialize logger
logger = logging.getLogger("unity_mcp.tools.animation")


def create_animation_clip(ctx: Context, name: str, length: float, 
                         loop: bool = True, wrap_mode: str = "Loop") -> str:
    """Create a new animation clip.
    
    Args:
        ctx: The MCP context
        name: The name of the animation clip
        length: The length of the animation clip in seconds
        loop: Whether the animation should loop
        wrap_mode: The wrap mode for the animation (Loop, PingPong, ClampForever, Once)
        
    Returns:
        JSON string with the result of the operation
    """
    if not name:
        error_message = "Animation clip name is required"
        error_reporter.report_error("ValueError", error_message, {"name": name})
        return json.dumps({"error": error_message, "success": False})
    
    if length <= 0:
        error_message = f"Animation length must be greater than 0, got {length}"
        error_reporter.report_error("ValueError", error_message, {"length": length})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate wrap mode
    valid_wrap_modes = ["Loop", "PingPong", "ClampForever", "Once"]
    if wrap_mode not in valid_wrap_modes:
        error_message = f"Invalid wrap mode: {wrap_mode}. Valid modes are: {', '.join(valid_wrap_modes)}"
        error_reporter.report_error("ValueError", error_message, {"wrap_mode": wrap_mode})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("create_animation_clip", {
        "name": name,
        "length": length,
        "loop": loop,
        "wrap_mode": wrap_mode
    })
    
    return json.dumps(result)


def add_animation_key(ctx: Context, clip_id: str, property_path: str, time: float, 
                     value: Union[float, List[float]], tangent_mode: str = "Auto") -> str:
    """Add a key to an animation clip.
    
    Args:
        ctx: The MCP context
        clip_id: The ID or name of the animation clip
        property_path: The property path to animate
        time: The time of the key in seconds
        value: The value of the key (float or vector)
        tangent_mode: The tangent mode for the key (Auto, Linear, Constant)
        
    Returns:
        JSON string with the result of the operation
    """
    if not clip_id:
        error_message = "Animation clip ID is required"
        error_reporter.report_error("ValueError", error_message, {"clip_id": clip_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not property_path:
        error_message = "Property path is required"
        error_reporter.report_error("ValueError", error_message, {"property_path": property_path})
        return json.dumps({"error": error_message, "success": False})
    
    if time < 0:
        error_message = f"Time must be non-negative, got {time}"
        error_reporter.report_error("ValueError", error_message, {"time": time})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate tangent mode
    valid_tangent_modes = ["Auto", "Linear", "Constant"]
    if tangent_mode not in valid_tangent_modes:
        error_message = f"Invalid tangent mode: {tangent_mode}. Valid modes are: {', '.join(valid_tangent_modes)}"
        error_reporter.report_error("ValueError", error_message, {"tangent_mode": tangent_mode})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("add_animation_key", {
        "clip_id": clip_id,
        "property_path": property_path,
        "time": time,
        "value": value,
        "tangent_mode": tangent_mode
    })
    
    return json.dumps(result)


def create_animator_controller(ctx: Context, name: str) -> str:
    """Create a new animator controller.
    
    Args:
        ctx: The MCP context
        name: The name of the animator controller
        
    Returns:
        JSON string with the result of the operation
    """
    if not name:
        error_message = "Animator controller name is required"
        error_reporter.report_error("ValueError", error_message, {"name": name})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("create_animator_controller", {
        "name": name
    })
    
    return json.dumps(result)


def add_animation_state(ctx: Context, controller_id: str, state_name: str, 
                       clip_id: Optional[str] = None, is_default: bool = False) -> str:
    """Add a state to an animator controller.
    
    Args:
        ctx: The MCP context
        controller_id: The ID or name of the animator controller
        state_name: The name of the state
        clip_id: The ID or name of the animation clip to use
        is_default: Whether this is the default state
        
    Returns:
        JSON string with the result of the operation
    """
    if not controller_id:
        error_message = "Animator controller ID is required"
        error_reporter.report_error("ValueError", error_message, {"controller_id": controller_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not state_name:
        error_message = "State name is required"
        error_reporter.report_error("ValueError", error_message, {"state_name": state_name})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "controller_id": controller_id,
        "state_name": state_name,
        "is_default": is_default
    }
    
    if clip_id:
        params["clip_id"] = clip_id
    
    # Execute the command
    result = command_handler_registry.handle_command("add_animation_state", params)
    
    return json.dumps(result)


def add_animation_transition(ctx: Context, controller_id: str, from_state: str, to_state: str,
                           has_exit_time: bool = False, exit_time: float = 0.0,
                           duration: float = 0.25, offset: float = 0.0) -> str:
    """Add a transition between animation states.
    
    Args:
        ctx: The MCP context
        controller_id: The ID or name of the animator controller
        from_state: The name of the source state
        to_state: The name of the destination state
        has_exit_time: Whether the transition has an exit time
        exit_time: The exit time for the transition (0-1)
        duration: The duration of the transition in seconds
        offset: The offset for the transition (0-1)
        
    Returns:
        JSON string with the result of the operation
    """
    if not controller_id:
        error_message = "Animator controller ID is required"
        error_reporter.report_error("ValueError", error_message, {"controller_id": controller_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not from_state:
        error_message = "Source state is required"
        error_reporter.report_error("ValueError", error_message, {"from_state": from_state})
        return json.dumps({"error": error_message, "success": False})
    
    if not to_state:
        error_message = "Destination state is required"
        error_reporter.report_error("ValueError", error_message, {"to_state": to_state})
        return json.dumps({"error": error_message, "success": False})
    
    if exit_time < 0 or exit_time > 1:
        error_message = f"Exit time must be between 0 and 1, got {exit_time}"
        error_reporter.report_error("ValueError", error_message, {"exit_time": exit_time})
        return json.dumps({"error": error_message, "success": False})
    
    if duration < 0:
        error_message = f"Duration must be non-negative, got {duration}"
        error_reporter.report_error("ValueError", error_message, {"duration": duration})
        return json.dumps({"error": error_message, "success": False})
    
    if offset < 0 or offset > 1:
        error_message = f"Offset must be between 0 and 1, got {offset}"
        error_reporter.report_error("ValueError", error_message, {"offset": offset})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("add_animation_transition", {
        "controller_id": controller_id,
        "from_state": from_state,
        "to_state": to_state,
        "has_exit_time": has_exit_time,
        "exit_time": exit_time,
        "duration": duration,
        "offset": offset
    })
    
    return json.dumps(result)


def add_animation_parameter(ctx: Context, controller_id: str, param_name: str, 
                           param_type: str, default_value: Optional[Union[float, bool, int]] = None) -> str:
    """Add a parameter to an animator controller.
    
    Args:
        ctx: The MCP context
        controller_id: The ID or name of the animator controller
        param_name: The name of the parameter
        param_type: The type of the parameter (Float, Int, Bool, Trigger)
        default_value: The default value for the parameter
        
    Returns:
        JSON string with the result of the operation
    """
    if not controller_id:
        error_message = "Animator controller ID is required"
        error_reporter.report_error("ValueError", error_message, {"controller_id": controller_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not param_name:
        error_message = "Parameter name is required"
        error_reporter.report_error("ValueError", error_message, {"param_name": param_name})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate parameter type
    valid_param_types = ["Float", "Int", "Bool", "Trigger"]
    if param_type not in valid_param_types:
        error_message = f"Invalid parameter type: {param_type}. Valid types are: {', '.join(valid_param_types)}"
        error_reporter.report_error("ValueError", error_message, {"param_type": param_type})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate default value based on parameter type
    if default_value is not None:
        if param_type == "Float" and not isinstance(default_value, (int, float)):
            error_message = f"Default value for Float parameter must be a number, got {type(default_value).__name__}"
            error_reporter.report_error("ValueError", error_message, {"default_value": default_value})
            return json.dumps({"error": error_message, "success": False})
        elif param_type == "Int" and not isinstance(default_value, int):
            error_message = f"Default value for Int parameter must be an integer, got {type(default_value).__name__}"
            error_reporter.report_error("ValueError", error_message, {"default_value": default_value})
            return json.dumps({"error": error_message, "success": False})
        elif param_type == "Bool" and not isinstance(default_value, bool):
            error_message = f"Default value for Bool parameter must be a boolean, got {type(default_value).__name__}"
            error_reporter.report_error("ValueError", error_message, {"default_value": default_value})
            return json.dumps({"error": error_message, "success": False})
        elif param_type == "Trigger" and default_value is not None:
            error_message = "Trigger parameters cannot have a default value"
            error_reporter.report_error("ValueError", error_message, {"default_value": default_value})
            return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "controller_id": controller_id,
        "param_name": param_name,
        "param_type": param_type
    }
    
    if default_value is not None:
        params["default_value"] = default_value
    
    # Execute the command
    result = command_handler_registry.handle_command("add_animation_parameter", params)
    
    return json.dumps(result)


def add_transition_condition(ctx: Context, controller_id: str, from_state: str, to_state: str,
                            param_name: str, condition_type: str, threshold: Optional[Union[float, bool, int]] = None) -> str:
    """Add a condition to a transition.
    
    Args:
        ctx: The MCP context
        controller_id: The ID or name of the animator controller
        from_state: The name of the source state
        to_state: The name of the destination state
        param_name: The name of the parameter to check
        condition_type: The type of condition (Equals, Greater, Less, NotEqual, Trigger)
        threshold: The threshold value for the condition
        
    Returns:
        JSON string with the result of the operation
    """
    if not controller_id:
        error_message = "Animator controller ID is required"
        error_reporter.report_error("ValueError", error_message, {"controller_id": controller_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not from_state:
        error_message = "Source state is required"
        error_reporter.report_error("ValueError", error_message, {"from_state": from_state})
        return json.dumps({"error": error_message, "success": False})
    
    if not to_state:
        error_message = "Destination state is required"
        error_reporter.report_error("ValueError", error_message, {"to_state": to_state})
        return json.dumps({"error": error_message, "success": False})
    
    if not param_name:
        error_message = "Parameter name is required"
        error_reporter.report_error("ValueError", error_message, {"param_name": param_name})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate condition type
    valid_condition_types = ["Equals", "Greater", "Less", "NotEqual", "Trigger"]
    if condition_type not in valid_condition_types:
        error_message = f"Invalid condition type: {condition_type}. Valid types are: {', '.join(valid_condition_types)}"
        error_reporter.report_error("ValueError", error_message, {"condition_type": condition_type})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate threshold based on condition type
    if condition_type != "Trigger" and threshold is None:
        error_message = f"Threshold is required for condition type {condition_type}"
        error_reporter.report_error("ValueError", error_message, {"threshold": threshold})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "controller_id": controller_id,
        "from_state": from_state,
        "to_state": to_state,
        "param_name": param_name,
        "condition_type": condition_type
    }
    
    if threshold is not None:
        params["threshold"] = threshold
    
    # Execute the command
    result = command_handler_registry.handle_command("add_transition_condition", params)
    
    return json.dumps(result)


def apply_animator_to_object(ctx: Context, controller_id: str, object_id: str) -> str:
    """Apply an animator controller to an object.
    
    Args:
        ctx: The MCP context
        controller_id: The ID or name of the animator controller
        object_id: The ID or path of the object
        
    Returns:
        JSON string with the result of the operation
    """
    if not controller_id:
        error_message = "Animator controller ID is required"
        error_reporter.report_error("ValueError", error_message, {"controller_id": controller_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("apply_animator_to_object", {
        "controller_id": controller_id,
        "object_id": object_id
    })
    
    return json.dumps(result)


def play_animation(ctx: Context, object_id: str, state_name: Optional[str] = None,
                 layer: int = 0, normalized_time: Optional[float] = None) -> str:
    """Play an animation on an object.
    
    Args:
        ctx: The MCP context
        object_id: The ID or path of the object
        state_name: The name of the state to play
        layer: The layer to play the animation on
        normalized_time: The normalized time to start playing at (0-1)
        
    Returns:
        JSON string with the result of the operation
    """
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "object_id": object_id,
        "layer": layer
    }
    
    if state_name:
        params["state_name"] = state_name
    
    if normalized_time is not None:
        if normalized_time < 0 or normalized_time > 1:
            error_message = f"Normalized time must be between 0 and 1, got {normalized_time}"
            error_reporter.report_error("ValueError", error_message, {"normalized_time": normalized_time})
            return json.dumps({"error": error_message, "success": False})
        params["normalized_time"] = normalized_time
    
    # Execute the command
    result = command_handler_registry.handle_command("play_animation", params)
    
    return json.dumps(result)


def set_animator_parameter(ctx: Context, object_id: str, param_name: str, 
                         value: Union[float, bool, int]) -> str:
    """Set a parameter on an animator.
    
    Args:
        ctx: The MCP context
        object_id: The ID or path of the object
        param_name: The name of the parameter
        value: The value to set
        
    Returns:
        JSON string with the result of the operation
    """
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not param_name:
        error_message = "Parameter name is required"
        error_reporter.report_error("ValueError", error_message, {"param_name": param_name})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("set_animator_parameter", {
        "object_id": object_id,
        "param_name": param_name,
        "value": value
    })
    
    return json.dumps(result)


def trigger_animator_parameter(ctx: Context, object_id: str, param_name: str) -> str:
    """Trigger a parameter on an animator.
    
    Args:
        ctx: The MCP context
        object_id: The ID or path of the object
        param_name: The name of the parameter
        
    Returns:
        JSON string with the result of the operation
    """
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not param_name:
        error_message = "Parameter name is required"
        error_reporter.report_error("ValueError", error_message, {"param_name": param_name})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("trigger_animator_parameter", {
        "object_id": object_id,
        "param_name": param_name
    })
    
    return json.dumps(result)


def get_animator_info(ctx: Context, object_id: str) -> str:
    """Get information about an animator.
    
    Args:
        ctx: The MCP context
        object_id: The ID or path of the object
        
    Returns:
        JSON string with the result of the operation
    """
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("get_animator_info", {
        "object_id": object_id
    })
    
    return json.dumps(result)


def register_animation_tools(tool_registry):
    """Register animation tools with the tool registry.
    
    Args:
        tool_registry: The tool registry to register tools with
    """
    # Create animation clip
    tool_registry.register_tool(
        name="create_animation_clip",
        func=create_animation_clip,
        category="animation",
        description="Create a new animation clip",
        parameters={
            "name": {
                "description": "The name of the animation clip",
                "type": "string",
                "required": True
            },
            "length": {
                "description": "The length of the animation clip in seconds",
                "type": "number",
                "required": True
            },
            "loop": {
                "description": "Whether the animation should loop",
                "type": "boolean",
                "required": False
            },
            "wrap_mode": {
                "description": "The wrap mode for the animation (Loop, PingPong, ClampForever, Once)",
                "type": "string",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Add animation key
    tool_registry.register_tool(
        name="add_animation_key",
        func=add_animation_key,
        category="animation",
        description="Add a key to an animation clip",
        parameters={
            "clip_id": {
                "description": "The ID or name of the animation clip",
                "type": "string",
                "required": True
            },
            "property_path": {
                "description": "The property path to animate",
                "type": "string",
                "required": True
            },
            "time": {
                "description": "The time of the key in seconds",
                "type": "number",
                "required": True
            },
            "value": {
                "description": "The value of the key (float or vector)",
                "type": "any",
                "required": True
            },
            "tangent_mode": {
                "description": "The tangent mode for the key (Auto, Linear, Constant)",
                "type": "string",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Create animator controller
    tool_registry.register_tool(
        name="create_animator_controller",
        func=create_animator_controller,
        category="animation",
        description="Create a new animator controller",
        parameters={
            "name": {
                "description": "The name of the animator controller",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Add animation state
    tool_registry.register_tool(
        name="add_animation_state",
        func=add_animation_state,
        category="animation",
        description="Add a state to an animator controller",
        parameters={
            "controller_id": {
                "description": "The ID or name of the animator controller",
                "type": "string",
                "required": True
            },
            "state_name": {
                "description": "The name of the state",
                "type": "string",
                "required": True
            },
            "clip_id": {
                "description": "The ID or name of the animation clip to use",
                "type": "string",
                "required": False
            },
            "is_default": {
                "description": "Whether this is the default state",
                "type": "boolean",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Add animation transition
    tool_registry.register_tool(
        name="add_animation_transition",
        func=add_animation_transition,
        category="animation",
        description="Add a transition between animation states",
        parameters={
            "controller_id": {
                "description": "The ID or name of the animator controller",
                "type": "string",
                "required": True
            },
            "from_state": {
                "description": "The name of the source state",
                "type": "string",
                "required": True
            },
            "to_state": {
                "description": "The name of the destination state",
                "type": "string",
                "required": True
            },
            "has_exit_time": {
                "description": "Whether the transition has an exit time",
                "type": "boolean",
                "required": False
            },
            "exit_time": {
                "description": "The exit time for the transition (0-1)",
                "type": "number",
                "required": False
            },
            "duration": {
                "description": "The duration of the transition in seconds",
                "type": "number",
                "required": False
            },
            "offset": {
                "description": "The offset for the transition (0-1)",
                "type": "number",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Add animation parameter
    tool_registry.register_tool(
        name="add_animation_parameter",
        func=add_animation_parameter,
        category="animation",
        description="Add a parameter to an animator controller",
        parameters={
            "controller_id": {
                "description": "The ID or name of the animator controller",
                "type": "string",
                "required": True
            },
            "param_name": {
                "description": "The name of the parameter",
                "type": "string",
                "required": True
            },
            "param_type": {
                "description": "The type of the parameter (Float, Int, Bool, Trigger)",
                "type": "string",
                "required": True
            },
            "default_value": {
                "description": "The default value for the parameter",
                "type": "any",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Add transition condition
    tool_registry.register_tool(
        name="add_transition_condition",
        func=add_transition_condition,
        category="animation",
        description="Add a condition to a transition",
        parameters={
            "controller_id": {
                "description": "The ID or name of the animator controller",
                "type": "string",
                "required": True
            },
            "from_state": {
                "description": "The name of the source state",
                "type": "string",
                "required": True
            },
            "to_state": {
                "description": "The name of the destination state",
                "type": "string",
                "required": True
            },
            "param_name": {
                "description": "The name of the parameter to check",
                "type": "string",
                "required": True
            },
            "condition_type": {
                "description": "The type of condition (Equals, Greater, Less, NotEqual, Trigger)",
                "type": "string",
                "required": True
            },
            "threshold": {
                "description": "The threshold value for the condition",
                "type": "any",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Apply animator to object
    tool_registry.register_tool(
        name="apply_animator_to_object",
        func=apply_animator_to_object,
        category="animation",
        description="Apply an animator controller to an object",
        parameters={
            "controller_id": {
                "description": "The ID or name of the animator controller",
                "type": "string",
                "required": True
            },
            "object_id": {
                "description": "The ID or path of the object",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Play animation
    tool_registry.register_tool(
        name="play_animation",
        func=play_animation,
        category="animation",
        description="Play an animation on an object",
        parameters={
            "object_id": {
                "description": "The ID or path of the object",
                "type": "string",
                "required": True
            },
            "state_name": {
                "description": "The name of the state to play",
                "type": "string",
                "required": False
            },
            "layer": {
                "description": "The layer to play the animation on",
                "type": "integer",
                "required": False
            },
            "normalized_time": {
                "description": "The normalized time to start playing at (0-1)",
                "type": "number",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set animator parameter
    tool_registry.register_tool(
        name="set_animator_parameter",
        func=set_animator_parameter,
        category="animation",
        description="Set a parameter on an animator",
        parameters={
            "object_id": {
                "description": "The ID or path of the object",
                "type": "string",
                "required": True
            },
            "param_name": {
                "description": "The name of the parameter",
                "type": "string",
                "required": True
            },
            "value": {
                "description": "The value to set",
                "type": "any",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Trigger animator parameter
    tool_registry.register_tool(
        name="trigger_animator_parameter",
        func=trigger_animator_parameter,
        category="animation",
        description="Trigger a parameter on an animator",
        parameters={
            "object_id": {
                "description": "The ID or path of the object",
                "type": "string",
                "required": True
            },
            "param_name": {
                "description": "The name of the parameter",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get animator info
    tool_registry.register_tool(
        name="get_animator_info",
        func=get_animator_info,
        category="animation",
        description="Get information about an animator",
        parameters={
            "object_id": {
                "description": "The ID or path of the object",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
