#!/usr/bin/env python3
"""
Material manipulation tools for Unity MCP.
"""

import json
import logging
import re
from typing import Dict, List, Optional, Tuple, Union

from mcp.server.fastmcp import Context

from ..core.command_handler import command_handler_registry
from ..error_reporter import error_reporter

# Initialize logger
logger = logging.getLogger("unity_mcp.tools.material")


def _validate_color(color: List[float]) -> Tuple[bool, Optional[str]]:
    """Validate a color value.
    
    Args:
        color: The color to validate [r, g, b, a] or [r, g, b]
        
    Returns:
        Tuple of (is_valid, error_message)
    """
    if not isinstance(color, list):
        return False, f"Color must be a list, got {type(color).__name__}"
    
    if len(color) not in [3, 4]:
        return False, f"Color must have 3 or 4 components, got {len(color)}"
    
    for i, component in enumerate(color):
        if not isinstance(component, (int, float)):
            return False, f"Color component {i} must be a number, got {type(component).__name__}"
        
        if component < 0 or component > 1:
            return False, f"Color component {i} must be between 0 and 1, got {component}"
    
    return True, None


def _normalize_color(color: List[float]) -> List[float]:
    """Normalize a color value to ensure it has 4 components.
    
    Args:
        color: The color to normalize [r, g, b, a] or [r, g, b]
        
    Returns:
        The normalized color [r, g, b, a]
    """
    if len(color) == 3:
        return color + [1.0]  # Add alpha = 1.0
    return color


def _parse_hex_color(hex_color: str) -> Optional[List[float]]:
    """Parse a hex color string to RGBA values.
    
    Args:
        hex_color: The hex color string (#RRGGBB or #RRGGBBAA)
        
    Returns:
        The color as [r, g, b, a] or None if invalid
    """
    # Remove # if present
    hex_color = hex_color.lstrip('#')
    
    # Check if valid hex color
    if not re.match(r'^[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$', hex_color):
        return None
    
    try:
        # Parse RGB
        r = int(hex_color[0:2], 16) / 255.0
        g = int(hex_color[2:4], 16) / 255.0
        b = int(hex_color[4:6], 16) / 255.0
        
        # Parse alpha if present
        if len(hex_color) == 8:
            a = int(hex_color[6:8], 16) / 255.0
        else:
            a = 1.0
        
        return [r, g, b, a]
    except ValueError:
        return None


def create_material(ctx: Context, name: str, shader_name: Optional[str] = "Standard",
                   color: Optional[Union[List[float], str]] = None) -> str:
    """Create a new material.
    
    Args:
        ctx: The MCP context
        name: The name of the material
        shader_name: The name of the shader to use
        color: The main color of the material as [r, g, b, a], [r, g, b], or hex string
        
    Returns:
        JSON string with the result of the operation
    """
    if not name:
        error_message = "Material name is required"
        error_reporter.report_error("ValueError", error_message, {"name": name})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "name": name,
        "shader_name": shader_name
    }
    
    # Handle color parameter
    if color is not None:
        if isinstance(color, str):
            # Parse hex color
            rgba = _parse_hex_color(color)
            if rgba is None:
                error_message = f"Invalid hex color format: {color}"
                error_reporter.report_error("ValueError", error_message, {"color": color})
                return json.dumps({"error": error_message, "success": False})
            params["color"] = rgba
        else:
            # Validate color list
            is_valid, error_message = _validate_color(color)
            if not is_valid:
                error_reporter.report_error("ValueError", error_message, {"color": color})
                return json.dumps({"error": error_message, "success": False})
            
            # Normalize color to ensure it has 4 components
            params["color"] = _normalize_color(color)
    
    # Execute the command
    result = command_handler_registry.handle_command("create_material", params)
    
    return json.dumps(result)


def set_material_color(ctx: Context, material_id: str, property_name: str, 
                      color: Union[List[float], str]) -> str:
    """Set a color property on a material.
    
    Args:
        ctx: The MCP context
        material_id: The ID or name of the material
        property_name: The name of the color property
        color: The color value as [r, g, b, a], [r, g, b], or hex string
        
    Returns:
        JSON string with the result of the operation
    """
    if not material_id:
        error_message = "Material ID is required"
        error_reporter.report_error("ValueError", error_message, {"material_id": material_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not property_name:
        error_message = "Property name is required"
        error_reporter.report_error("ValueError", error_message, {"property_name": property_name})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "material_id": material_id,
        "property_name": property_name
    }
    
    # Handle color parameter
    if isinstance(color, str):
        # Parse hex color
        rgba = _parse_hex_color(color)
        if rgba is None:
            error_message = f"Invalid hex color format: {color}"
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        params["color"] = rgba
    else:
        # Validate color list
        is_valid, error_message = _validate_color(color)
        if not is_valid:
            error_reporter.report_error("ValueError", error_message, {"color": color})
            return json.dumps({"error": error_message, "success": False})
        
        # Normalize color to ensure it has 4 components
        params["color"] = _normalize_color(color)
    
    # Execute the command
    result = command_handler_registry.handle_command("set_material_color", params)
    
    return json.dumps(result)


def set_material_float(ctx: Context, material_id: str, property_name: str, value: float) -> str:
    """Set a float property on a material.
    
    Args:
        ctx: The MCP context
        material_id: The ID or name of the material
        property_name: The name of the float property
        value: The float value
        
    Returns:
        JSON string with the result of the operation
    """
    if not material_id:
        error_message = "Material ID is required"
        error_reporter.report_error("ValueError", error_message, {"material_id": material_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not property_name:
        error_message = "Property name is required"
        error_reporter.report_error("ValueError", error_message, {"property_name": property_name})
        return json.dumps({"error": error_message, "success": False})
    
    if not isinstance(value, (int, float)):
        error_message = f"Value must be a number, got {type(value).__name__}"
        error_reporter.report_error("ValueError", error_message, {"value": value})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("set_material_float", {
        "material_id": material_id,
        "property_name": property_name,
        "value": float(value)
    })
    
    return json.dumps(result)


def set_material_vector(ctx: Context, material_id: str, property_name: str, value: List[float]) -> str:
    """Set a vector property on a material.
    
    Args:
        ctx: The MCP context
        material_id: The ID or name of the material
        property_name: The name of the vector property
        value: The vector value [x, y, z, w] or [x, y, z]
        
    Returns:
        JSON string with the result of the operation
    """
    if not material_id:
        error_message = "Material ID is required"
        error_reporter.report_error("ValueError", error_message, {"material_id": material_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not property_name:
        error_message = "Property name is required"
        error_reporter.report_error("ValueError", error_message, {"property_name": property_name})
        return json.dumps({"error": error_message, "success": False})
    
    if not isinstance(value, list):
        error_message = f"Value must be a list, got {type(value).__name__}"
        error_reporter.report_error("ValueError", error_message, {"value": value})
        return json.dumps({"error": error_message, "success": False})
    
    if len(value) not in [2, 3, 4]:
        error_message = f"Vector must have 2, 3, or 4 components, got {len(value)}"
        error_reporter.report_error("ValueError", error_message, {"value": value})
        return json.dumps({"error": error_message, "success": False})
    
    for i, component in enumerate(value):
        if not isinstance(component, (int, float)):
            error_message = f"Vector component {i} must be a number, got {type(component).__name__}"
            error_reporter.report_error("ValueError", error_message, {"value": value})
            return json.dumps({"error": error_message, "success": False})
    
    # Normalize vector to ensure it has 4 components
    if len(value) == 2:
        value = value + [0.0, 0.0]
    elif len(value) == 3:
        value = value + [0.0]
    
    # Execute the command
    result = command_handler_registry.handle_command("set_material_vector", {
        "material_id": material_id,
        "property_name": property_name,
        "value": value
    })
    
    return json.dumps(result)


def set_material_texture(ctx: Context, material_id: str, property_name: str, 
                        texture_path: str, tiling: Optional[List[float]] = None,
                        offset: Optional[List[float]] = None) -> str:
    """Set a texture property on a material.
    
    Args:
        ctx: The MCP context
        material_id: The ID or name of the material
        property_name: The name of the texture property
        texture_path: The path to the texture file
        tiling: The texture tiling [x, y]
        offset: The texture offset [x, y]
        
    Returns:
        JSON string with the result of the operation
    """
    if not material_id:
        error_message = "Material ID is required"
        error_reporter.report_error("ValueError", error_message, {"material_id": material_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not property_name:
        error_message = "Property name is required"
        error_reporter.report_error("ValueError", error_message, {"property_name": property_name})
        return json.dumps({"error": error_message, "success": False})
    
    if not texture_path:
        error_message = "Texture path is required"
        error_reporter.report_error("ValueError", error_message, {"texture_path": texture_path})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "material_id": material_id,
        "property_name": property_name,
        "texture_path": texture_path
    }
    
    if tiling is not None:
        if not isinstance(tiling, list) or len(tiling) != 2:
            error_message = f"Tiling must be a list of 2 values [x, y], got {tiling}"
            error_reporter.report_error("ValueError", error_message, {"tiling": tiling})
            return json.dumps({"error": error_message, "success": False})
        params["tiling"] = tiling
    
    if offset is not None:
        if not isinstance(offset, list) or len(offset) != 2:
            error_message = f"Offset must be a list of 2 values [x, y], got {offset}"
            error_reporter.report_error("ValueError", error_message, {"offset": offset})
            return json.dumps({"error": error_message, "success": False})
        params["offset"] = offset
    
    # Execute the command
    result = command_handler_registry.handle_command("set_material_texture", params)
    
    return json.dumps(result)


def apply_material_to_object(ctx: Context, material_id: str, object_id: str, 
                           material_index: Optional[int] = None) -> str:
    """Apply a material to an object.
    
    Args:
        ctx: The MCP context
        material_id: The ID or name of the material
        object_id: The ID or path of the object
        material_index: The material index for objects with multiple materials
        
    Returns:
        JSON string with the result of the operation
    """
    if not material_id:
        error_message = "Material ID is required"
        error_reporter.report_error("ValueError", error_message, {"material_id": material_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "material_id": material_id,
        "object_id": object_id
    }
    
    if material_index is not None:
        if not isinstance(material_index, int) or material_index < 0:
            error_message = f"Material index must be a non-negative integer, got {material_index}"
            error_reporter.report_error("ValueError", error_message, {"material_index": material_index})
            return json.dumps({"error": error_message, "success": False})
        params["material_index"] = material_index
    
    # Execute the command
    result = command_handler_registry.handle_command("apply_material_to_object", params)
    
    return json.dumps(result)


def get_material_info(ctx: Context, material_id: str) -> str:
    """Get information about a material.
    
    Args:
        ctx: The MCP context
        material_id: The ID or name of the material
        
    Returns:
        JSON string with the result of the operation
    """
    if not material_id:
        error_message = "Material ID is required"
        error_reporter.report_error("ValueError", error_message, {"material_id": material_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("get_material_info", {
        "material_id": material_id
    })
    
    return json.dumps(result)


def duplicate_material(ctx: Context, material_id: str, new_name: Optional[str] = None) -> str:
    """Duplicate a material.
    
    Args:
        ctx: The MCP context
        material_id: The ID or name of the material to duplicate
        new_name: The name for the duplicated material
        
    Returns:
        JSON string with the result of the operation
    """
    if not material_id:
        error_message = "Material ID is required"
        error_reporter.report_error("ValueError", error_message, {"material_id": material_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "material_id": material_id
    }
    
    if new_name is not None:
        params["new_name"] = new_name
    
    # Execute the command
    result = command_handler_registry.handle_command("duplicate_material", params)
    
    return json.dumps(result)


def find_materials_by_name(ctx: Context, name: str, exact_match: bool = False) -> str:
    """Find materials by name.
    
    Args:
        ctx: The MCP context
        name: The name to search for
        exact_match: Whether to require an exact match
        
    Returns:
        JSON string with the result of the operation
    """
    if not name:
        error_message = "Name is required"
        error_reporter.report_error("ValueError", error_message, {"name": name})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("find_materials_by_name", {
        "name": name,
        "exact_match": exact_match
    })
    
    return json.dumps(result)


def delete_material(ctx: Context, material_id: str) -> str:
    """Delete a material.
    
    Args:
        ctx: The MCP context
        material_id: The ID or name of the material to delete
        
    Returns:
        JSON string with the result of the operation
    """
    if not material_id:
        error_message = "Material ID is required"
        error_reporter.report_error("ValueError", error_message, {"material_id": material_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("delete_material", {
        "material_id": material_id
    })
    
    return json.dumps(result)


def register_material_tools(tool_registry):
    """Register material tools with the tool registry.
    
    Args:
        tool_registry: The tool registry to register tools with
    """
    # Create material
    tool_registry.register_tool(
        name="create_material",
        func=create_material,
        category="material",
        description="Create a new material",
        parameters={
            "name": {
                "description": "The name of the material",
                "type": "string",
                "required": True
            },
            "shader_name": {
                "description": "The name of the shader to use",
                "type": "string",
                "required": False
            },
            "color": {
                "description": "The main color of the material as [r, g, b, a], [r, g, b], or hex string",
                "type": "any",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set material color
    tool_registry.register_tool(
        name="set_material_color",
        func=set_material_color,
        category="material",
        description="Set a color property on a material",
        parameters={
            "material_id": {
                "description": "The ID or name of the material",
                "type": "string",
                "required": True
            },
            "property_name": {
                "description": "The name of the color property",
                "type": "string",
                "required": True
            },
            "color": {
                "description": "The color value as [r, g, b, a], [r, g, b], or hex string",
                "type": "any",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set material float
    tool_registry.register_tool(
        name="set_material_float",
        func=set_material_float,
        category="material",
        description="Set a float property on a material",
        parameters={
            "material_id": {
                "description": "The ID or name of the material",
                "type": "string",
                "required": True
            },
            "property_name": {
                "description": "The name of the float property",
                "type": "string",
                "required": True
            },
            "value": {
                "description": "The float value",
                "type": "number",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set material vector
    tool_registry.register_tool(
        name="set_material_vector",
        func=set_material_vector,
        category="material",
        description="Set a vector property on a material",
        parameters={
            "material_id": {
                "description": "The ID or name of the material",
                "type": "string",
                "required": True
            },
            "property_name": {
                "description": "The name of the vector property",
                "type": "string",
                "required": True
            },
            "value": {
                "description": "The vector value [x, y, z, w] or [x, y, z]",
                "type": "array",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set material texture
    tool_registry.register_tool(
        name="set_material_texture",
        func=set_material_texture,
        category="material",
        description="Set a texture property on a material",
        parameters={
            "material_id": {
                "description": "The ID or name of the material",
                "type": "string",
                "required": True
            },
            "property_name": {
                "description": "The name of the texture property",
                "type": "string",
                "required": True
            },
            "texture_path": {
                "description": "The path to the texture file",
                "type": "string",
                "required": True
            },
            "tiling": {
                "description": "The texture tiling [x, y]",
                "type": "array",
                "required": False
            },
            "offset": {
                "description": "The texture offset [x, y]",
                "type": "array",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Apply material to object
    tool_registry.register_tool(
        name="apply_material_to_object",
        func=apply_material_to_object,
        category="material",
        description="Apply a material to an object",
        parameters={
            "material_id": {
                "description": "The ID or name of the material",
                "type": "string",
                "required": True
            },
            "object_id": {
                "description": "The ID or path of the object",
                "type": "string",
                "required": True
            },
            "material_index": {
                "description": "The material index for objects with multiple materials",
                "type": "integer",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get material info
    tool_registry.register_tool(
        name="get_material_info",
        func=get_material_info,
        category="material",
        description="Get information about a material",
        parameters={
            "material_id": {
                "description": "The ID or name of the material",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Duplicate material
    tool_registry.register_tool(
        name="duplicate_material",
        func=duplicate_material,
        category="material",
        description="Duplicate a material",
        parameters={
            "material_id": {
                "description": "The ID or name of the material to duplicate",
                "type": "string",
                "required": True
            },
            "new_name": {
                "description": "The name for the duplicated material",
                "type": "string",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Find materials by name
    tool_registry.register_tool(
        name="find_materials_by_name",
        func=find_materials_by_name,
        category="material",
        description="Find materials by name",
        parameters={
            "name": {
                "description": "The name to search for",
                "type": "string",
                "required": True
            },
            "exact_match": {
                "description": "Whether to require an exact match",
                "type": "boolean",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Delete material
    tool_registry.register_tool(
        name="delete_material",
        func=delete_material,
        category="material",
        description="Delete a material",
        parameters={
            "material_id": {
                "description": "The ID or name of the material to delete",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
