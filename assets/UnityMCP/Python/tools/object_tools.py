#!/usr/bin/env python3
"""
Object manipulation tools for Unity MCP.
"""

import json
import logging
from typing import Dict, List, Optional, Tuple, Union

from mcp.server.fastmcp import Context

from ..core.command_handler import command_handler_registry
from ..error_reporter import error_reporter

# Initialize logger
logger = logging.getLogger("unity_mcp.tools.object")


def create_primitive(ctx: Context, primitive_type: str, position: Optional[List[float]] = None,
                    rotation: Optional[List[float]] = None, scale: Optional[List[float]] = None,
                    name: Optional[str] = None, parent: Optional[str] = None) -> str:
    """Create a primitive object in the scene.
    
    Args:
        ctx: The MCP context
        primitive_type: The type of primitive to create (cube, sphere, cylinder, etc.)
        position: The position of the object [x, y, z]
        rotation: The rotation of the object [x, y, z]
        scale: The scale of the object [x, y, z]
        name: The name of the object
        parent: The parent object's ID or path
        
    Returns:
        JSON string with the result of the operation
    """
    # Validate parameters
    valid_primitives = ["cube", "sphere", "cylinder", "capsule", "plane", "quad", "cone"]
    if primitive_type.lower() not in valid_primitives:
        error_message = f"Invalid primitive type: {primitive_type}. Valid types are: {', '.join(valid_primitives)}"
        error_reporter.report_error("ValueError", error_message, {"primitive_type": primitive_type})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "primitive_type": primitive_type.lower(),
    }
    
    if position is not None:
        if len(position) != 3:
            error_message = f"Position must be a list of 3 values [x, y, z], got {position}"
            error_reporter.report_error("ValueError", error_message, {"position": position})
            return json.dumps({"error": error_message, "success": False})
        params["position"] = position
    
    if rotation is not None:
        if len(rotation) != 3:
            error_message = f"Rotation must be a list of 3 values [x, y, z], got {rotation}"
            error_reporter.report_error("ValueError", error_message, {"rotation": rotation})
            return json.dumps({"error": error_message, "success": False})
        params["rotation"] = rotation
    
    if scale is not None:
        if len(scale) != 3:
            error_message = f"Scale must be a list of 3 values [x, y, z], got {scale}"
            error_reporter.report_error("ValueError", error_message, {"scale": scale})
            return json.dumps({"error": error_message, "success": False})
        params["scale"] = scale
    
    if name is not None:
        params["name"] = name
    
    if parent is not None:
        params["parent"] = parent
    
    # Execute the command
    result = command_handler_registry.handle_command("create_primitive", params)
    
    return json.dumps(result)


def create_empty(ctx: Context, position: Optional[List[float]] = None,
                rotation: Optional[List[float]] = None, name: Optional[str] = None,
                parent: Optional[str] = None) -> str:
    """Create an empty game object in the scene.
    
    Args:
        ctx: The MCP context
        position: The position of the object [x, y, z]
        rotation: The rotation of the object [x, y, z]
        name: The name of the object
        parent: The parent object's ID or path
        
    Returns:
        JSON string with the result of the operation
    """
    # Prepare parameters
    params = {}
    
    if position is not None:
        if len(position) != 3:
            error_message = f"Position must be a list of 3 values [x, y, z], got {position}"
            error_reporter.report_error("ValueError", error_message, {"position": position})
            return json.dumps({"error": error_message, "success": False})
        params["position"] = position
    
    if rotation is not None:
        if len(rotation) != 3:
            error_message = f"Rotation must be a list of 3 values [x, y, z], got {rotation}"
            error_reporter.report_error("ValueError", error_message, {"rotation": rotation})
            return json.dumps({"error": error_message, "success": False})
        params["rotation"] = rotation
    
    if name is not None:
        params["name"] = name
    
    if parent is not None:
        params["parent"] = parent
    
    # Execute the command
    result = command_handler_registry.handle_command("create_empty", params)
    
    return json.dumps(result)


def delete_object(ctx: Context, object_id: str) -> str:
    """Delete an object from the scene.
    
    Args:
        ctx: The MCP context
        object_id: The ID or path of the object to delete
        
    Returns:
        JSON string with the result of the operation
    """
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("delete_object", {"object_id": object_id})
    
    return json.dumps(result)


def set_object_transform(ctx: Context, object_id: str, position: Optional[List[float]] = None,
                        rotation: Optional[List[float]] = None, scale: Optional[List[float]] = None,
                        local: bool = False) -> str:
    """Set the transform of an object.
    
    Args:
        ctx: The MCP context
        object_id: The ID or path of the object
        position: The position of the object [x, y, z]
        rotation: The rotation of the object [x, y, z]
        scale: The scale of the object [x, y, z]
        local: Whether to use local or world space
        
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
        "local": local
    }
    
    if position is not None:
        if len(position) != 3:
            error_message = f"Position must be a list of 3 values [x, y, z], got {position}"
            error_reporter.report_error("ValueError", error_message, {"position": position})
            return json.dumps({"error": error_message, "success": False})
        params["position"] = position
    
    if rotation is not None:
        if len(rotation) != 3:
            error_message = f"Rotation must be a list of 3 values [x, y, z], got {rotation}"
            error_reporter.report_error("ValueError", error_message, {"rotation": rotation})
            return json.dumps({"error": error_message, "success": False})
        params["rotation"] = rotation
    
    if scale is not None:
        if len(scale) != 3:
            error_message = f"Scale must be a list of 3 values [x, y, z], got {scale}"
            error_reporter.report_error("ValueError", error_message, {"scale": scale})
            return json.dumps({"error": error_message, "success": False})
        params["scale"] = scale
    
    # Execute the command
    result = command_handler_registry.handle_command("set_object_transform", params)
    
    return json.dumps(result)


def get_object_transform(ctx: Context, object_id: str, local: bool = False) -> str:
    """Get the transform of an object.
    
    Args:
        ctx: The MCP context
        object_id: The ID or path of the object
        local: Whether to use local or world space
        
    Returns:
        JSON string with the result of the operation
    """
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("get_object_transform", {
        "object_id": object_id,
        "local": local
    })
    
    return json.dumps(result)


def set_parent(ctx: Context, object_id: str, parent_id: str, keep_world_transform: bool = True) -> str:
    """Set the parent of an object.
    
    Args:
        ctx: The MCP context
        object_id: The ID or path of the object
        parent_id: The ID or path of the parent object
        keep_world_transform: Whether to maintain the world transform when parenting
        
    Returns:
        JSON string with the result of the operation
    """
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    if not parent_id:
        error_message = "Parent ID is required"
        error_reporter.report_error("ValueError", error_message, {"parent_id": parent_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("set_parent", {
        "object_id": object_id,
        "parent_id": parent_id,
        "keep_world_transform": keep_world_transform
    })
    
    return json.dumps(result)


def get_object_info(ctx: Context, object_id: str, include_components: bool = False,
                   include_children: bool = False) -> str:
    """Get information about an object.
    
    Args:
        ctx: The MCP context
        object_id: The ID or path of the object
        include_components: Whether to include component information
        include_children: Whether to include child objects
        
    Returns:
        JSON string with the result of the operation
    """
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("get_object_info", {
        "object_id": object_id,
        "include_components": include_components,
        "include_children": include_children
    })
    
    return json.dumps(result)


def duplicate_object(ctx: Context, object_id: str, new_name: Optional[str] = None,
                    position: Optional[List[float]] = None) -> str:
    """Duplicate an object.
    
    Args:
        ctx: The MCP context
        object_id: The ID or path of the object to duplicate
        new_name: The name for the duplicated object
        position: The position for the duplicated object [x, y, z]
        
    Returns:
        JSON string with the result of the operation
    """
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "object_id": object_id
    }
    
    if new_name is not None:
        params["new_name"] = new_name
    
    if position is not None:
        if len(position) != 3:
            error_message = f"Position must be a list of 3 values [x, y, z], got {position}"
            error_reporter.report_error("ValueError", error_message, {"position": position})
            return json.dumps({"error": error_message, "success": False})
        params["position"] = position
    
    # Execute the command
    result = command_handler_registry.handle_command("duplicate_object", params)
    
    return json.dumps(result)


def find_objects_by_name(ctx: Context, name: str, exact_match: bool = False) -> str:
    """Find objects by name.
    
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
    result = command_handler_registry.handle_command("find_objects_by_name", {
        "name": name,
        "exact_match": exact_match
    })
    
    return json.dumps(result)


def find_objects_by_tag(ctx: Context, tag: str) -> str:
    """Find objects by tag.
    
    Args:
        ctx: The MCP context
        tag: The tag to search for
        
    Returns:
        JSON string with the result of the operation
    """
    if not tag:
        error_message = "Tag is required"
        error_reporter.report_error("ValueError", error_message, {"tag": tag})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("find_objects_by_tag", {
        "tag": tag
    })
    
    return json.dumps(result)


def set_object_enabled(ctx: Context, object_id: str, enabled: bool) -> str:
    """Enable or disable an object.
    
    Args:
        ctx: The MCP context
        object_id: The ID or path of the object
        enabled: Whether the object should be enabled
        
    Returns:
        JSON string with the result of the operation
    """
    if not object_id:
        error_message = "Object ID is required"
        error_reporter.report_error("ValueError", error_message, {"object_id": object_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("set_object_enabled", {
        "object_id": object_id,
        "enabled": enabled
    })
    
    return json.dumps(result)


def register_object_tools(tool_registry):
    """Register object tools with the tool registry.
    
    Args:
        tool_registry: The tool registry to register tools with
    """
    # Create primitive
    tool_registry.register_tool(
        name="create_primitive",
        func=create_primitive,
        category="object",
        description="Create a primitive object in the scene",
        parameters={
            "primitive_type": {
                "description": "The type of primitive to create (cube, sphere, cylinder, etc.)",
                "type": "string",
                "required": True
            },
            "position": {
                "description": "The position of the object [x, y, z]",
                "type": "array",
                "required": False
            },
            "rotation": {
                "description": "The rotation of the object [x, y, z]",
                "type": "array",
                "required": False
            },
            "scale": {
                "description": "The scale of the object [x, y, z]",
                "type": "array",
                "required": False
            },
            "name": {
                "description": "The name of the object",
                "type": "string",
                "required": False
            },
            "parent": {
                "description": "The parent object's ID or path",
                "type": "string",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Create empty
    tool_registry.register_tool(
        name="create_empty",
        func=create_empty,
        category="object",
        description="Create an empty game object in the scene",
        parameters={
            "position": {
                "description": "The position of the object [x, y, z]",
                "type": "array",
                "required": False
            },
            "rotation": {
                "description": "The rotation of the object [x, y, z]",
                "type": "array",
                "required": False
            },
            "name": {
                "description": "The name of the object",
                "type": "string",
                "required": False
            },
            "parent": {
                "description": "The parent object's ID or path",
                "type": "string",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Delete object
    tool_registry.register_tool(
        name="delete_object",
        func=delete_object,
        category="object",
        description="Delete an object from the scene",
        parameters={
            "object_id": {
                "description": "The ID or path of the object to delete",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set object transform
    tool_registry.register_tool(
        name="set_object_transform",
        func=set_object_transform,
        category="object",
        description="Set the transform of an object",
        parameters={
            "object_id": {
                "description": "The ID or path of the object",
                "type": "string",
                "required": True
            },
            "position": {
                "description": "The position of the object [x, y, z]",
                "type": "array",
                "required": False
            },
            "rotation": {
                "description": "The rotation of the object [x, y, z]",
                "type": "array",
                "required": False
            },
            "scale": {
                "description": "The scale of the object [x, y, z]",
                "type": "array",
                "required": False
            },
            "local": {
                "description": "Whether to use local or world space",
                "type": "boolean",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get object transform
    tool_registry.register_tool(
        name="get_object_transform",
        func=get_object_transform,
        category="object",
        description="Get the transform of an object",
        parameters={
            "object_id": {
                "description": "The ID or path of the object",
                "type": "string",
                "required": True
            },
            "local": {
                "description": "Whether to use local or world space",
                "type": "boolean",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set parent
    tool_registry.register_tool(
        name="set_parent",
        func=set_parent,
        category="object",
        description="Set the parent of an object",
        parameters={
            "object_id": {
                "description": "The ID or path of the object",
                "type": "string",
                "required": True
            },
            "parent_id": {
                "description": "The ID or path of the parent object",
                "type": "string",
                "required": True
            },
            "keep_world_transform": {
                "description": "Whether to maintain the world transform when parenting",
                "type": "boolean",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get object info
    tool_registry.register_tool(
        name="get_object_info",
        func=get_object_info,
        category="object",
        description="Get information about an object",
        parameters={
            "object_id": {
                "description": "The ID or path of the object",
                "type": "string",
                "required": True
            },
            "include_components": {
                "description": "Whether to include component information",
                "type": "boolean",
                "required": False
            },
            "include_children": {
                "description": "Whether to include child objects",
                "type": "boolean",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Duplicate object
    tool_registry.register_tool(
        name="duplicate_object",
        func=duplicate_object,
        category="object",
        description="Duplicate an object",
        parameters={
            "object_id": {
                "description": "The ID or path of the object to duplicate",
                "type": "string",
                "required": True
            },
            "new_name": {
                "description": "The name for the duplicated object",
                "type": "string",
                "required": False
            },
            "position": {
                "description": "The position for the duplicated object [x, y, z]",
                "type": "array",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Find objects by name
    tool_registry.register_tool(
        name="find_objects_by_name",
        func=find_objects_by_name,
        category="object",
        description="Find objects by name",
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
    
    # Find objects by tag
    tool_registry.register_tool(
        name="find_objects_by_tag",
        func=find_objects_by_tag,
        category="object",
        description="Find objects by tag",
        parameters={
            "tag": {
                "description": "The tag to search for",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Set object enabled
    tool_registry.register_tool(
        name="set_object_enabled",
        func=set_object_enabled,
        category="object",
        description="Enable or disable an object",
        parameters={
            "object_id": {
                "description": "The ID or path of the object",
                "type": "string",
                "required": True
            },
            "enabled": {
                "description": "Whether the object should be enabled",
                "type": "boolean",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
