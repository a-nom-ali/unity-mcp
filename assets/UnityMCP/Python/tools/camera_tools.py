#!/usr/bin/env python3
"""
Camera tools for Unity MCP.
"""

import json
import logging
from typing import Dict, List, Optional, Tuple, Union

from mcp.server.fastmcp import Context

from ..core.command_handler import command_handler_registry
from ..error_reporter import error_reporter

# Initialize logger
logger = logging.getLogger("unity_mcp.tools.camera")


def create_camera(ctx: Context, name: str, position: List[float] = None, 
                 rotation: List[float] = None, field_of_view: float = 60.0,
                 near_clip_plane: float = 0.3, far_clip_plane: float = 1000.0,
                 depth: int = 0, culling_mask: int = -1) -> str:
    """Create a new camera in the scene.
    
    Args:
        ctx: The MCP context
        name: The name of the camera
        position: The position of the camera [x, y, z]
        rotation: The rotation of the camera in euler angles [x, y, z]
        field_of_view: The field of view of the camera in degrees
        near_clip_plane: The near clip plane distance
        far_clip_plane: The far clip plane distance
        depth: The depth of the camera (determines rendering order)
        culling_mask: The culling mask for the camera
        
    Returns:
        JSON string with the result of the operation
    """
    if not name:
        error_message = "Camera name is required"
        error_reporter.report_error("ValueError", error_message, {"name": name})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate position
    if position is not None and (len(position) != 3 or not all(isinstance(x, (int, float)) for x in position)):
        error_message = "Position must be a list of 3 numbers"
        error_reporter.report_error("ValueError", error_message, {"position": position})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate rotation
    if rotation is not None and (len(rotation) != 3 or not all(isinstance(x, (int, float)) for x in rotation)):
        error_message = "Rotation must be a list of 3 numbers"
        error_reporter.report_error("ValueError", error_message, {"rotation": rotation})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate field of view
    if field_of_view <= 0 or field_of_view > 179:
        error_message = f"Field of view must be between 0 and 179, got {field_of_view}"
        error_reporter.report_error("ValueError", error_message, {"field_of_view": field_of_view})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate clip planes
    if near_clip_plane <= 0:
        error_message = f"Near clip plane must be greater than 0, got {near_clip_plane}"
        error_reporter.report_error("ValueError", error_message, {"near_clip_plane": near_clip_plane})
        return json.dumps({"error": error_message, "success": False})
    
    if far_clip_plane <= near_clip_plane:
        error_message = f"Far clip plane must be greater than near clip plane, got {far_clip_plane} <= {near_clip_plane}"
        error_reporter.report_error("ValueError", error_message, {"far_clip_plane": far_clip_plane, "near_clip_plane": near_clip_plane})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "name": name,
        "field_of_view": field_of_view,
        "near_clip_plane": near_clip_plane,
        "far_clip_plane": far_clip_plane,
        "depth": depth,
        "culling_mask": culling_mask
    }
    
    if position is not None:
        params["position"] = position
    
    if rotation is not None:
        params["rotation"] = rotation
    
    # Execute the command
    result = command_handler_registry.handle_command("create_camera", params)
    
    return json.dumps(result)


def set_camera_properties(ctx: Context, camera_id: str, field_of_view: Optional[float] = None,
                         near_clip_plane: Optional[float] = None, far_clip_plane: Optional[float] = None,
                         depth: Optional[int] = None, culling_mask: Optional[int] = None,
                         clear_flags: Optional[str] = None, background_color: Optional[List[float]] = None,
                         projection: Optional[str] = None, orthographic_size: Optional[float] = None) -> str:
    """Set properties of a camera.
    
    Args:
        ctx: The MCP context
        camera_id: The ID or path of the camera
        field_of_view: The field of view of the camera in degrees
        near_clip_plane: The near clip plane distance
        far_clip_plane: The far clip plane distance
        depth: The depth of the camera (determines rendering order)
        culling_mask: The culling mask for the camera
        clear_flags: The clear flags for the camera (Skybox, SolidColor, Depth, Nothing)
        background_color: The background color [r, g, b, a]
        projection: The projection mode (Perspective, Orthographic)
        orthographic_size: The orthographic size of the camera
        
    Returns:
        JSON string with the result of the operation
    """
    if not camera_id:
        error_message = "Camera ID is required"
        error_reporter.report_error("ValueError", error_message, {"camera_id": camera_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate field of view
    if field_of_view is not None and (field_of_view <= 0 or field_of_view > 179):
        error_message = f"Field of view must be between 0 and 179, got {field_of_view}"
        error_reporter.report_error("ValueError", error_message, {"field_of_view": field_of_view})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate clip planes
    if near_clip_plane is not None and near_clip_plane <= 0:
        error_message = f"Near clip plane must be greater than 0, got {near_clip_plane}"
        error_reporter.report_error("ValueError", error_message, {"near_clip_plane": near_clip_plane})
        return json.dumps({"error": error_message, "success": False})
    
    if far_clip_plane is not None and near_clip_plane is not None and far_clip_plane <= near_clip_plane:
        error_message = f"Far clip plane must be greater than near clip plane, got {far_clip_plane} <= {near_clip_plane}"
        error_reporter.report_error("ValueError", error_message, {"far_clip_plane": far_clip_plane, "near_clip_plane": near_clip_plane})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate clear flags
    if clear_flags is not None:
        valid_clear_flags = ["Skybox", "SolidColor", "Depth", "Nothing"]
        if clear_flags not in valid_clear_flags:
            error_message = f"Invalid clear flags: {clear_flags}. Valid flags are: {', '.join(valid_clear_flags)}"
            error_reporter.report_error("ValueError", error_message, {"clear_flags": clear_flags})
            return json.dumps({"error": error_message, "success": False})
    
    # Validate background color
    if background_color is not None:
        if len(background_color) != 4 or not all(isinstance(x, (int, float)) for x in background_color):
            error_message = "Background color must be a list of 4 numbers [r, g, b, a]"
            error_reporter.report_error("ValueError", error_message, {"background_color": background_color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(0 <= x <= 1 for x in background_color):
            error_message = "Background color values must be between 0 and 1"
            error_reporter.report_error("ValueError", error_message, {"background_color": background_color})
            return json.dumps({"error": error_message, "success": False})
    
    # Validate projection
    if projection is not None:
        valid_projections = ["Perspective", "Orthographic"]
        if projection not in valid_projections:
            error_message = f"Invalid projection: {projection}. Valid projections are: {', '.join(valid_projections)}"
            error_reporter.report_error("ValueError", error_message, {"projection": projection})
            return json.dumps({"error": error_message, "success": False})
    
    # Validate orthographic size
    if orthographic_size is not None and orthographic_size <= 0:
        error_message = f"Orthographic size must be greater than 0, got {orthographic_size}"
        error_reporter.report_error("ValueError", error_message, {"orthographic_size": orthographic_size})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "camera_id": camera_id
    }
    
    if field_of_view is not None:
        params["field_of_view"] = field_of_view
    
    if near_clip_plane is not None:
        params["near_clip_plane"] = near_clip_plane
    
    if far_clip_plane is not None:
        params["far_clip_plane"] = far_clip_plane
    
    if depth is not None:
        params["depth"] = depth
    
    if culling_mask is not None:
        params["culling_mask"] = culling_mask
    
    if clear_flags is not None:
        params["clear_flags"] = clear_flags
    
    if background_color is not None:
        params["background_color"] = background_color
    
    if projection is not None:
        params["projection"] = projection
    
    if orthographic_size is not None:
        params["orthographic_size"] = orthographic_size
    
    # Execute the command
    result = command_handler_registry.handle_command("set_camera_properties", params)
    
    return json.dumps(result)


def set_active_camera(ctx: Context, camera_id: str) -> str:
    """Set the active camera in the scene.
    
    Args:
        ctx: The MCP context
        camera_id: The ID or path of the camera
        
    Returns:
        JSON string with the result of the operation
    """
    if not camera_id:
        error_message = "Camera ID is required"
        error_reporter.report_error("ValueError", error_message, {"camera_id": camera_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("set_active_camera", {
        "camera_id": camera_id
    })
    
    return json.dumps(result)


def get_active_camera(ctx: Context) -> str:
    """Get the active camera in the scene.
    
    Args:
        ctx: The MCP context
        
    Returns:
        JSON string with the result of the operation
    """
    # Execute the command
    result = command_handler_registry.handle_command("get_active_camera", {})
    
    return json.dumps(result)


def get_camera_info(ctx: Context, camera_id: str) -> str:
    """Get information about a camera.
    
    Args:
        ctx: The MCP context
        camera_id: The ID or path of the camera
        
    Returns:
        JSON string with the result of the operation
    """
    if not camera_id:
        error_message = "Camera ID is required"
        error_reporter.report_error("ValueError", error_message, {"camera_id": camera_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("get_camera_info", {
        "camera_id": camera_id
    })
    
    return json.dumps(result)


def move_camera(ctx: Context, camera_id: str, position: List[float] = None, 
               rotation: List[float] = None, target: str = None,
               smooth: bool = False, duration: float = 1.0) -> str:
    """Move a camera to a new position and/or rotation.
    
    Args:
        ctx: The MCP context
        camera_id: The ID or path of the camera
        position: The new position [x, y, z]
        rotation: The new rotation in euler angles [x, y, z]
        target: The ID or path of an object to look at
        smooth: Whether to smooth the movement
        duration: The duration of the movement in seconds (if smooth is True)
        
    Returns:
        JSON string with the result of the operation
    """
    if not camera_id:
        error_message = "Camera ID is required"
        error_reporter.report_error("ValueError", error_message, {"camera_id": camera_id})
        return json.dumps({"error": error_message, "success": False})
    
    if position is None and rotation is None and target is None:
        error_message = "At least one of position, rotation, or target must be specified"
        error_reporter.report_error("ValueError", error_message, {})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate position
    if position is not None and (len(position) != 3 or not all(isinstance(x, (int, float)) for x in position)):
        error_message = "Position must be a list of 3 numbers"
        error_reporter.report_error("ValueError", error_message, {"position": position})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate rotation
    if rotation is not None and (len(rotation) != 3 or not all(isinstance(x, (int, float)) for x in rotation)):
        error_message = "Rotation must be a list of 3 numbers"
        error_reporter.report_error("ValueError", error_message, {"rotation": rotation})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate duration
    if smooth and duration <= 0:
        error_message = f"Duration must be greater than 0, got {duration}"
        error_reporter.report_error("ValueError", error_message, {"duration": duration})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "camera_id": camera_id,
        "smooth": smooth
    }
    
    if position is not None:
        params["position"] = position
    
    if rotation is not None:
        params["rotation"] = rotation
    
    if target is not None:
        params["target"] = target
    
    if smooth:
        params["duration"] = duration
    
    # Execute the command
    result = command_handler_registry.handle_command("move_camera", params)
    
    return json.dumps(result)


def add_camera_effect(ctx: Context, camera_id: str, effect_type: str, 
                     parameters: Dict[str, Union[float, int, bool, str, List]] = None) -> str:
    """Add a post-processing effect to a camera.
    
    Args:
        ctx: The MCP context
        camera_id: The ID or path of the camera
        effect_type: The type of effect (Bloom, ChromaticAberration, ColorGrading, DepthOfField, Grain, LensDistortion, Vignette)
        parameters: The parameters for the effect
        
    Returns:
        JSON string with the result of the operation
    """
    if not camera_id:
        error_message = "Camera ID is required"
        error_reporter.report_error("ValueError", error_message, {"camera_id": camera_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not effect_type:
        error_message = "Effect type is required"
        error_reporter.report_error("ValueError", error_message, {"effect_type": effect_type})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate effect type
    valid_effect_types = ["Bloom", "ChromaticAberration", "ColorGrading", "DepthOfField", "Grain", "LensDistortion", "Vignette"]
    if effect_type not in valid_effect_types:
        error_message = f"Invalid effect type: {effect_type}. Valid types are: {', '.join(valid_effect_types)}"
        error_reporter.report_error("ValueError", error_message, {"effect_type": effect_type})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "camera_id": camera_id,
        "effect_type": effect_type
    }
    
    if parameters is not None:
        params["parameters"] = parameters
    
    # Execute the command
    result = command_handler_registry.handle_command("add_camera_effect", params)
    
    return json.dumps(result)


def remove_camera_effect(ctx: Context, camera_id: str, effect_type: str) -> str:
    """Remove a post-processing effect from a camera.
    
    Args:
        ctx: The MCP context
        camera_id: The ID or path of the camera
        effect_type: The type of effect (Bloom, ChromaticAberration, ColorGrading, DepthOfField, Grain, LensDistortion, Vignette)
        
    Returns:
        JSON string with the result of the operation
    """
    if not camera_id:
        error_message = "Camera ID is required"
        error_reporter.report_error("ValueError", error_message, {"camera_id": camera_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not effect_type:
        error_message = "Effect type is required"
        error_reporter.report_error("ValueError", error_message, {"effect_type": effect_type})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate effect type
    valid_effect_types = ["Bloom", "ChromaticAberration", "ColorGrading", "DepthOfField", "Grain", "LensDistortion", "Vignette"]
    if effect_type not in valid_effect_types:
        error_message = f"Invalid effect type: {effect_type}. Valid types are: {', '.join(valid_effect_types)}"
        error_reporter.report_error("ValueError", error_message, {"effect_type": effect_type})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("remove_camera_effect", {
        "camera_id": camera_id,
        "effect_type": effect_type
    })
    
    return json.dumps(result)


def register_camera_tools(tool_registry):
    """Register camera tools with the tool registry.
    
    Args:
        tool_registry: The tool registry to register tools with
    """
    # Create camera
    tool_registry.register_tool(
        name="create_camera",
        func=create_camera,
        category="camera",
        description="Create a new camera in the scene",
        parameters={
            "name": {
                "description": "The name of the camera",
                "type": "string",
                "required": True
            },
            "position": {
                "description": "The position of the camera [x, y, z]",
                "type": "array",
                "required": False
            },
            "rotation": {
                "description": "The rotation of the camera in euler angles [x, y, z]",
                "type": "array",
                "required": False
            },
            "field_of_view": {
                "description": "The field of view of the camera in degrees",
                "type": "number",
                "required": False
            },
            "near_clip_plane": {
                "description": "The near clip plane distance",
                "type": "number",
                "required": False
            },
            "far_clip_plane": {
                "description": "The far clip plane distance",
                "type": "number",
                "required": False
            },
            "depth": {
                "description": "The depth of the camera (determines rendering order)",
                "type": "integer",
                "required": False
            },
            "culling_mask": {
                "description": "The culling mask for the camera",
                "type": "integer",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set camera properties
    tool_registry.register_tool(
        name="set_camera_properties",
        func=set_camera_properties,
        category="camera",
        description="Set properties of a camera",
        parameters={
            "camera_id": {
                "description": "The ID or path of the camera",
                "type": "string",
                "required": True
            },
            "field_of_view": {
                "description": "The field of view of the camera in degrees",
                "type": "number",
                "required": False
            },
            "near_clip_plane": {
                "description": "The near clip plane distance",
                "type": "number",
                "required": False
            },
            "far_clip_plane": {
                "description": "The far clip plane distance",
                "type": "number",
                "required": False
            },
            "depth": {
                "description": "The depth of the camera (determines rendering order)",
                "type": "integer",
                "required": False
            },
            "culling_mask": {
                "description": "The culling mask for the camera",
                "type": "integer",
                "required": False
            },
            "clear_flags": {
                "description": "The clear flags for the camera (Skybox, SolidColor, Depth, Nothing)",
                "type": "string",
                "required": False
            },
            "background_color": {
                "description": "The background color [r, g, b, a]",
                "type": "array",
                "required": False
            },
            "projection": {
                "description": "The projection mode (Perspective, Orthographic)",
                "type": "string",
                "required": False
            },
            "orthographic_size": {
                "description": "The orthographic size of the camera",
                "type": "number",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set active camera
    tool_registry.register_tool(
        name="set_active_camera",
        func=set_active_camera,
        category="camera",
        description="Set the active camera in the scene",
        parameters={
            "camera_id": {
                "description": "The ID or path of the camera",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get active camera
    tool_registry.register_tool(
        name="get_active_camera",
        func=get_active_camera,
        category="camera",
        description="Get the active camera in the scene",
        parameters={},
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get camera info
    tool_registry.register_tool(
        name="get_camera_info",
        func=get_camera_info,
        category="camera",
        description="Get information about a camera",
        parameters={
            "camera_id": {
                "description": "The ID or path of the camera",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Move camera
    tool_registry.register_tool(
        name="move_camera",
        func=move_camera,
        category="camera",
        description="Move a camera to a new position and/or rotation",
        parameters={
            "camera_id": {
                "description": "The ID or path of the camera",
                "type": "string",
                "required": True
            },
            "position": {
                "description": "The new position [x, y, z]",
                "type": "array",
                "required": False
            },
            "rotation": {
                "description": "The new rotation in euler angles [x, y, z]",
                "type": "array",
                "required": False
            },
            "target": {
                "description": "The ID or path of an object to look at",
                "type": "string",
                "required": False
            },
            "smooth": {
                "description": "Whether to smooth the movement",
                "type": "boolean",
                "required": False
            },
            "duration": {
                "description": "The duration of the movement in seconds (if smooth is True)",
                "type": "number",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Add camera effect
    tool_registry.register_tool(
        name="add_camera_effect",
        func=add_camera_effect,
        category="camera",
        description="Add a post-processing effect to a camera",
        parameters={
            "camera_id": {
                "description": "The ID or path of the camera",
                "type": "string",
                "required": True
            },
            "effect_type": {
                "description": "The type of effect (Bloom, ChromaticAberration, ColorGrading, DepthOfField, Grain, LensDistortion, Vignette)",
                "type": "string",
                "required": True
            },
            "parameters": {
                "description": "The parameters for the effect",
                "type": "object",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Remove camera effect
    tool_registry.register_tool(
        name="remove_camera_effect",
        func=remove_camera_effect,
        category="camera",
        description="Remove a post-processing effect from a camera",
        parameters={
            "camera_id": {
                "description": "The ID or path of the camera",
                "type": "string",
                "required": True
            },
            "effect_type": {
                "description": "The type of effect (Bloom, ChromaticAberration, ColorGrading, DepthOfField, Grain, LensDistortion, Vignette)",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
