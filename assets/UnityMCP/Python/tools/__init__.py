"""
Unity MCP Tools Package

This package contains tool implementations for Unity MCP.
Each module in this package provides a set of tools for a specific domain.
"""

from ..core.tool_registry import tool_registry

# Import tool modules
from .object_tools import register_object_tools
from .material_tools import register_material_tools
from .animation_tools import register_animation_tools
from .camera_tools import register_camera_tools
from .lighting_tools import register_lighting_tools
from .utility_tools import register_utility_tools
from .asset_tools import register_asset_tools

__all__ = [
    'register_object_tools',
    'register_material_tools',
    'register_animation_tools',
    'register_camera_tools',
    'register_lighting_tools',
    'register_utility_tools',
    'register_asset_tools',
    'register_all_tools'
]

# Register all tools
def register_all_tools():
    """Register all tools with the tool registry."""
    register_object_tools(tool_registry)
    register_material_tools(tool_registry)
    register_animation_tools(tool_registry)
    register_camera_tools(tool_registry)
    register_lighting_tools(tool_registry)
    register_utility_tools(tool_registry)
    register_asset_tools(tool_registry)
    
    print(f"Registered {tool_registry.count_tools()} tools with the tool registry")
