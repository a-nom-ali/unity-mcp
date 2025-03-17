#!/usr/bin/env python3
"""
Lighting tools for Unity MCP.
"""

import json
import logging
from typing import Dict, List, Optional, Tuple, Union

from mcp.server.fastmcp import Context

from ..core.command_handler import command_handler_registry
from ..error_reporter import error_reporter

# Initialize logger
logger = logging.getLogger("unity_mcp.tools.lighting")


def create_directional_light(ctx: Context, name: str, position: List[float] = None, 
                           rotation: List[float] = None, intensity: float = 1.0,
                           color: List[float] = None, shadows: bool = True,
                           shadow_strength: float = 1.0) -> str:
    """Create a new directional light in the scene.
    
    Args:
        ctx: The MCP context
        name: The name of the light
        position: The position of the light [x, y, z]
        rotation: The rotation of the light in euler angles [x, y, z]
        intensity: The intensity of the light
        color: The color of the light [r, g, b] or [r, g, b, a]
        shadows: Whether the light casts shadows
        shadow_strength: The strength of shadows (0-1)
        
    Returns:
        JSON string with the result of the operation
    """
    if not name:
        error_message = "Light name is required"
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
    
    # Validate intensity
    if intensity < 0:
        error_message = f"Intensity must be non-negative, got {intensity}"
        error_reporter.report_error("ValueError", error_message, {"intensity": intensity})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate color
    if color is not None:
        if not (len(color) == 3 or len(color) == 4):
            error_message = "Color must be a list of 3 or 4 numbers [r, g, b] or [r, g, b, a]"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(isinstance(x, (int, float)) for x in color):
            error_message = "Color values must be numbers"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(0 <= x <= 1 for x in color):
            error_message = "Color values must be between 0 and 1"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
    
    # Validate shadow strength
    if shadow_strength < 0 or shadow_strength > 1:
        error_message = f"Shadow strength must be between 0 and 1, got {shadow_strength}"
        error_reporter.report_error("ValueError", error_message, {"shadow_strength": shadow_strength})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "name": name,
        "type": "Directional",
        "intensity": intensity,
        "shadows": shadows,
        "shadow_strength": shadow_strength
    }
    
    if position is not None:
        params["position"] = position
    
    if rotation is not None:
        params["rotation"] = rotation
    
    if color is not None:
        params["color"] = color
    
    # Execute the command
    result = command_handler_registry.handle_command("create_light", params)
    
    return json.dumps(result)


def create_point_light(ctx: Context, name: str, position: List[float] = None, 
                     intensity: float = 1.0, color: List[float] = None,
                     range: float = 10.0, shadows: bool = True,
                     shadow_strength: float = 1.0) -> str:
    """Create a new point light in the scene.
    
    Args:
        ctx: The MCP context
        name: The name of the light
        position: The position of the light [x, y, z]
        intensity: The intensity of the light
        color: The color of the light [r, g, b] or [r, g, b, a]
        range: The range of the light
        shadows: Whether the light casts shadows
        shadow_strength: The strength of shadows (0-1)
        
    Returns:
        JSON string with the result of the operation
    """
    if not name:
        error_message = "Light name is required"
        error_reporter.report_error("ValueError", error_message, {"name": name})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate position
    if position is not None and (len(position) != 3 or not all(isinstance(x, (int, float)) for x in position)):
        error_message = "Position must be a list of 3 numbers"
        error_reporter.report_error("ValueError", error_message, {"position": position})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate intensity
    if intensity < 0:
        error_message = f"Intensity must be non-negative, got {intensity}"
        error_reporter.report_error("ValueError", error_message, {"intensity": intensity})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate color
    if color is not None:
        if not (len(color) == 3 or len(color) == 4):
            error_message = "Color must be a list of 3 or 4 numbers [r, g, b] or [r, g, b, a]"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(isinstance(x, (int, float)) for x in color):
            error_message = "Color values must be numbers"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(0 <= x <= 1 for x in color):
            error_message = "Color values must be between 0 and 1"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
    
    # Validate range
    if range <= 0:
        error_message = f"Range must be greater than 0, got {range}"
        error_reporter.report_error("ValueError", error_message, {"range": range})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate shadow strength
    if shadow_strength < 0 or shadow_strength > 1:
        error_message = f"Shadow strength must be between 0 and 1, got {shadow_strength}"
        error_reporter.report_error("ValueError", error_message, {"shadow_strength": shadow_strength})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "name": name,
        "type": "Point",
        "intensity": intensity,
        "range": range,
        "shadows": shadows,
        "shadow_strength": shadow_strength
    }
    
    if position is not None:
        params["position"] = position
    
    if color is not None:
        params["color"] = color
    
    # Execute the command
    result = command_handler_registry.handle_command("create_light", params)
    
    return json.dumps(result)


def create_spot_light(ctx: Context, name: str, position: List[float] = None, 
                    rotation: List[float] = None, intensity: float = 1.0,
                    color: List[float] = None, range: float = 10.0,
                    spot_angle: float = 30.0, shadows: bool = True,
                    shadow_strength: float = 1.0) -> str:
    """Create a new spot light in the scene.
    
    Args:
        ctx: The MCP context
        name: The name of the light
        position: The position of the light [x, y, z]
        rotation: The rotation of the light in euler angles [x, y, z]
        intensity: The intensity of the light
        color: The color of the light [r, g, b] or [r, g, b, a]
        range: The range of the light
        spot_angle: The angle of the spot light cone in degrees
        shadows: Whether the light casts shadows
        shadow_strength: The strength of shadows (0-1)
        
    Returns:
        JSON string with the result of the operation
    """
    if not name:
        error_message = "Light name is required"
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
    
    # Validate intensity
    if intensity < 0:
        error_message = f"Intensity must be non-negative, got {intensity}"
        error_reporter.report_error("ValueError", error_message, {"intensity": intensity})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate color
    if color is not None:
        if not (len(color) == 3 or len(color) == 4):
            error_message = "Color must be a list of 3 or 4 numbers [r, g, b] or [r, g, b, a]"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(isinstance(x, (int, float)) for x in color):
            error_message = "Color values must be numbers"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(0 <= x <= 1 for x in color):
            error_message = "Color values must be between 0 and 1"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
    
    # Validate range
    if range <= 0:
        error_message = f"Range must be greater than 0, got {range}"
        error_reporter.report_error("ValueError", error_message, {"range": range})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate spot angle
    if spot_angle <= 0 or spot_angle > 179:
        error_message = f"Spot angle must be between 0 and 179, got {spot_angle}"
        error_reporter.report_error("ValueError", error_message, {"spot_angle": spot_angle})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate shadow strength
    if shadow_strength < 0 or shadow_strength > 1:
        error_message = f"Shadow strength must be between 0 and 1, got {shadow_strength}"
        error_reporter.report_error("ValueError", error_message, {"shadow_strength": shadow_strength})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "name": name,
        "type": "Spot",
        "intensity": intensity,
        "range": range,
        "spot_angle": spot_angle,
        "shadows": shadows,
        "shadow_strength": shadow_strength
    }
    
    if position is not None:
        params["position"] = position
    
    if rotation is not None:
        params["rotation"] = rotation
    
    if color is not None:
        params["color"] = color
    
    # Execute the command
    result = command_handler_registry.handle_command("create_light", params)
    
    return json.dumps(result)


def create_area_light(ctx: Context, name: str, position: List[float] = None, 
                    rotation: List[float] = None, intensity: float = 1.0,
                    color: List[float] = None, width: float = 1.0,
                    height: float = 1.0, shadows: bool = True,
                    shadow_strength: float = 1.0) -> str:
    """Create a new area light in the scene.
    
    Args:
        ctx: The MCP context
        name: The name of the light
        position: The position of the light [x, y, z]
        rotation: The rotation of the light in euler angles [x, y, z]
        intensity: The intensity of the light
        color: The color of the light [r, g, b] or [r, g, b, a]
        width: The width of the area light
        height: The height of the area light
        shadows: Whether the light casts shadows
        shadow_strength: The strength of shadows (0-1)
        
    Returns:
        JSON string with the result of the operation
    """
    if not name:
        error_message = "Light name is required"
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
    
    # Validate intensity
    if intensity < 0:
        error_message = f"Intensity must be non-negative, got {intensity}"
        error_reporter.report_error("ValueError", error_message, {"intensity": intensity})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate color
    if color is not None:
        if not (len(color) == 3 or len(color) == 4):
            error_message = "Color must be a list of 3 or 4 numbers [r, g, b] or [r, g, b, a]"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(isinstance(x, (int, float)) for x in color):
            error_message = "Color values must be numbers"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(0 <= x <= 1 for x in color):
            error_message = "Color values must be between 0 and 1"
            error_reporter.report_error("ValueError", error_message, {"color": color})
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
    
    # Validate shadow strength
    if shadow_strength < 0 or shadow_strength > 1:
        error_message = f"Shadow strength must be between 0 and 1, got {shadow_strength}"
        error_reporter.report_error("ValueError", error_message, {"shadow_strength": shadow_strength})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "name": name,
        "type": "Area",
        "intensity": intensity,
        "width": width,
        "height": height,
        "shadows": shadows,
        "shadow_strength": shadow_strength
    }
    
    if position is not None:
        params["position"] = position
    
    if rotation is not None:
        params["rotation"] = rotation
    
    if color is not None:
        params["color"] = color
    
    # Execute the command
    result = command_handler_registry.handle_command("create_light", params)
    
    return json.dumps(result)


def set_light_properties(ctx: Context, light_id: str, intensity: Optional[float] = None,
                        color: Optional[List[float]] = None, range: Optional[float] = None,
                        spot_angle: Optional[float] = None, shadows: Optional[bool] = None,
                        shadow_strength: Optional[float] = None, width: Optional[float] = None,
                        height: Optional[float] = None) -> str:
    """Set properties of a light.
    
    Args:
        ctx: The MCP context
        light_id: The ID or path of the light
        intensity: The intensity of the light
        color: The color of the light [r, g, b] or [r, g, b, a]
        range: The range of the light (Point and Spot lights only)
        spot_angle: The angle of the spot light cone in degrees (Spot lights only)
        shadows: Whether the light casts shadows
        shadow_strength: The strength of shadows (0-1)
        width: The width of the area light (Area lights only)
        height: The height of the area light (Area lights only)
        
    Returns:
        JSON string with the result of the operation
    """
    if not light_id:
        error_message = "Light ID is required"
        error_reporter.report_error("ValueError", error_message, {"light_id": light_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate intensity
    if intensity is not None and intensity < 0:
        error_message = f"Intensity must be non-negative, got {intensity}"
        error_reporter.report_error("ValueError", error_message, {"intensity": intensity})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate color
    if color is not None:
        if not (len(color) == 3 or len(color) == 4):
            error_message = "Color must be a list of 3 or 4 numbers [r, g, b] or [r, g, b, a]"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(isinstance(x, (int, float)) for x in color):
            error_message = "Color values must be numbers"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(0 <= x <= 1 for x in color):
            error_message = "Color values must be between 0 and 1"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
    
    # Validate range
    if range is not None and range <= 0:
        error_message = f"Range must be greater than 0, got {range}"
        error_reporter.report_error("ValueError", error_message, {"range": range})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate spot angle
    if spot_angle is not None and (spot_angle <= 0 or spot_angle > 179):
        error_message = f"Spot angle must be between 0 and 179, got {spot_angle}"
        error_reporter.report_error("ValueError", error_message, {"spot_angle": spot_angle})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate shadow strength
    if shadow_strength is not None and (shadow_strength < 0 or shadow_strength > 1):
        error_message = f"Shadow strength must be between 0 and 1, got {shadow_strength}"
        error_reporter.report_error("ValueError", error_message, {"shadow_strength": shadow_strength})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate width and height
    if width is not None and width <= 0:
        error_message = f"Width must be greater than 0, got {width}"
        error_reporter.report_error("ValueError", error_message, {"width": width})
        return json.dumps({"error": error_message, "success": False})
    
    if height is not None and height <= 0:
        error_message = f"Height must be greater than 0, got {height}"
        error_reporter.report_error("ValueError", error_message, {"height": height})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "light_id": light_id
    }
    
    if intensity is not None:
        params["intensity"] = intensity
    
    if color is not None:
        params["color"] = color
    
    if range is not None:
        params["range"] = range
    
    if spot_angle is not None:
        params["spot_angle"] = spot_angle
    
    if shadows is not None:
        params["shadows"] = shadows
    
    if shadow_strength is not None:
        params["shadow_strength"] = shadow_strength
    
    if width is not None:
        params["width"] = width
    
    if height is not None:
        params["height"] = height
    
    # Execute the command
    result = command_handler_registry.handle_command("set_light_properties", params)
    
    return json.dumps(result)


def get_light_info(ctx: Context, light_id: str) -> str:
    """Get information about a light.
    
    Args:
        ctx: The MCP context
        light_id: The ID or path of the light
        
    Returns:
        JSON string with the result of the operation
    """
    if not light_id:
        error_message = "Light ID is required"
        error_reporter.report_error("ValueError", error_message, {"light_id": light_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("get_light_info", {
        "light_id": light_id
    })
    
    return json.dumps(result)


def set_ambient_light(ctx: Context, intensity: float = 1.0, color: List[float] = None) -> str:
    """Set the ambient light in the scene.
    
    Args:
        ctx: The MCP context
        intensity: The intensity of the ambient light
        color: The color of the ambient light [r, g, b] or [r, g, b, a]
        
    Returns:
        JSON string with the result of the operation
    """
    # Validate intensity
    if intensity < 0:
        error_message = f"Intensity must be non-negative, got {intensity}"
        error_reporter.report_error("ValueError", error_message, {"intensity": intensity})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate color
    if color is not None:
        if not (len(color) == 3 or len(color) == 4):
            error_message = "Color must be a list of 3 or 4 numbers [r, g, b] or [r, g, b, a]"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(isinstance(x, (int, float)) for x in color):
            error_message = "Color values must be numbers"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        if not all(0 <= x <= 1 for x in color):
            error_message = "Color values must be between 0 and 1"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "intensity": intensity
    }
    
    if color is not None:
        params["color"] = color
    
    # Execute the command
    result = command_handler_registry.handle_command("set_ambient_light", params)
    
    return json.dumps(result)


def set_skybox(ctx: Context, material_id: Optional[str] = None, exposure: Optional[float] = None,
              rotation: Optional[float] = None) -> str:
    """Set the skybox in the scene.
    
    Args:
        ctx: The MCP context
        material_id: The ID or name of the skybox material
        exposure: The exposure of the skybox
        rotation: The rotation of the skybox in degrees
        
    Returns:
        JSON string with the result of the operation
    """
    # Validate exposure
    if exposure is not None and exposure < 0:
        error_message = f"Exposure must be non-negative, got {exposure}"
        error_reporter.report_error("ValueError", error_message, {"exposure": exposure})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {}
    
    if material_id is not None:
        params["material_id"] = material_id
    
    if exposure is not None:
        params["exposure"] = exposure
    
    if rotation is not None:
        params["rotation"] = rotation
    
    # Execute the command
    result = command_handler_registry.handle_command("set_skybox", params)
    
    return json.dumps(result)


def register_lighting_tools(tool_registry):
    """Register lighting tools with the tool registry.
    
    Args:
        tool_registry: The tool registry to register tools with
    """
    # Create directional light
    tool_registry.register_tool(
        name="create_directional_light",
        func=create_directional_light,
        category="lighting",
        description="Create a new directional light in the scene",
        parameters={
            "name": {
                "description": "The name of the light",
                "type": "string",
                "required": True
            },
            "position": {
                "description": "The position of the light [x, y, z]",
                "type": "array",
                "required": False
            },
            "rotation": {
                "description": "The rotation of the light in euler angles [x, y, z]",
                "type": "array",
                "required": False
            },
            "intensity": {
                "description": "The intensity of the light",
                "type": "number",
                "required": False
            },
            "color": {
                "description": "The color of the light [r, g, b] or [r, g, b, a]",
                "type": "array",
                "required": False
            },
            "shadows": {
                "description": "Whether the light casts shadows",
                "type": "boolean",
                "required": False
            },
            "shadow_strength": {
                "description": "The strength of shadows (0-1)",
                "type": "number",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Create point light
    tool_registry.register_tool(
        name="create_point_light",
        func=create_point_light,
        category="lighting",
        description="Create a new point light in the scene",
        parameters={
            "name": {
                "description": "The name of the light",
                "type": "string",
                "required": True
            },
            "position": {
                "description": "The position of the light [x, y, z]",
                "type": "array",
                "required": False
            },
            "intensity": {
                "description": "The intensity of the light",
                "type": "number",
                "required": False
            },
            "color": {
                "description": "The color of the light [r, g, b] or [r, g, b, a]",
                "type": "array",
                "required": False
            },
            "range": {
                "description": "The range of the light",
                "type": "number",
                "required": False
            },
            "shadows": {
                "description": "Whether the light casts shadows",
                "type": "boolean",
                "required": False
            },
            "shadow_strength": {
                "description": "The strength of shadows (0-1)",
                "type": "number",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Create spot light
    tool_registry.register_tool(
        name="create_spot_light",
        func=create_spot_light,
        category="lighting",
        description="Create a new spot light in the scene",
        parameters={
            "name": {
                "description": "The name of the light",
                "type": "string",
                "required": True
            },
            "position": {
                "description": "The position of the light [x, y, z]",
                "type": "array",
                "required": False
            },
            "rotation": {
                "description": "The rotation of the light in euler angles [x, y, z]",
                "type": "array",
                "required": False
            },
            "intensity": {
                "description": "The intensity of the light",
                "type": "number",
                "required": False
            },
            "color": {
                "description": "The color of the light [r, g, b] or [r, g, b, a]",
                "type": "array",
                "required": False
            },
            "range": {
                "description": "The range of the light",
                "type": "number",
                "required": False
            },
            "spot_angle": {
                "description": "The angle of the spot light cone in degrees",
                "type": "number",
                "required": False
            },
            "shadows": {
                "description": "Whether the light casts shadows",
                "type": "boolean",
                "required": False
            },
            "shadow_strength": {
                "description": "The strength of shadows (0-1)",
                "type": "number",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Create area light
    tool_registry.register_tool(
        name="create_area_light",
        func=create_area_light,
        category="lighting",
        description="Create a new area light in the scene",
        parameters={
            "name": {
                "description": "The name of the light",
                "type": "string",
                "required": True
            },
            "position": {
                "description": "The position of the light [x, y, z]",
                "type": "array",
                "required": False
            },
            "rotation": {
                "description": "The rotation of the light in euler angles [x, y, z]",
                "type": "array",
                "required": False
            },
            "intensity": {
                "description": "The intensity of the light",
                "type": "number",
                "required": False
            },
            "color": {
                "description": "The color of the light [r, g, b] or [r, g, b, a]",
                "type": "array",
                "required": False
            },
            "width": {
                "description": "The width of the area light",
                "type": "number",
                "required": False
            },
            "height": {
                "description": "The height of the area light",
                "type": "number",
                "required": False
            },
            "shadows": {
                "description": "Whether the light casts shadows",
                "type": "boolean",
                "required": False
            },
            "shadow_strength": {
                "description": "The strength of shadows (0-1)",
                "type": "number",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set light properties
    tool_registry.register_tool(
        name="set_light_properties",
        func=set_light_properties,
        category="lighting",
        description="Set properties of a light",
        parameters={
            "light_id": {
                "description": "The ID or path of the light",
                "type": "string",
                "required": True
            },
            "intensity": {
                "description": "The intensity of the light",
                "type": "number",
                "required": False
            },
            "color": {
                "description": "The color of the light [r, g, b] or [r, g, b, a]",
                "type": "array",
                "required": False
            },
            "range": {
                "description": "The range of the light (Point and Spot lights only)",
                "type": "number",
                "required": False
            },
            "spot_angle": {
                "description": "The angle of the spot light cone in degrees (Spot lights only)",
                "type": "number",
                "required": False
            },
            "shadows": {
                "description": "Whether the light casts shadows",
                "type": "boolean",
                "required": False
            },
            "shadow_strength": {
                "description": "The strength of shadows (0-1)",
                "type": "number",
                "required": False
            },
            "width": {
                "description": "The width of the area light (Area lights only)",
                "type": "number",
                "required": False
            },
            "height": {
                "description": "The height of the area light (Area lights only)",
                "type": "number",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get light info
    tool_registry.register_tool(
        name="get_light_info",
        func=get_light_info,
        category="lighting",
        description="Get information about a light",
        parameters={
            "light_id": {
                "description": "The ID or path of the light",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set ambient light
    tool_registry.register_tool(
        name="set_ambient_light",
        func=set_ambient_light,
        category="lighting",
        description="Set the ambient light in the scene",
        parameters={
            "intensity": {
                "description": "The intensity of the ambient light",
                "type": "number",
                "required": False
            },
            "color": {
                "description": "The color of the ambient light [r, g, b] or [r, g, b, a]",
                "type": "array",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set skybox
    tool_registry.register_tool(
        name="set_skybox",
        func=set_skybox,
        category="lighting",
        description="Set the skybox in the scene",
        parameters={
            "material_id": {
                "description": "The ID or name of the skybox material",
                "type": "string",
                "required": False
            },
            "exposure": {
                "description": "The exposure of the skybox",
                "type": "number",
                "required": False
            },
            "rotation": {
                "description": "The rotation of the skybox in degrees",
                "type": "number",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
