"""
Unity MCP Asset Tools Module

This module provides tools for working with assets in Unity MCP.
These tools include functionality for:
- Importing assets from various sources
- Managing asset bundles
- Working with the asset store
- Handling PolyHaven assets
"""

import json
import os
from typing import List, Dict, Any, Optional, Union
from mcp.server.fastmcp import Context

# Import error reporting
from ..core.error_reporter import error_reporter

# Import command handler registry
from ..core.command_handler import command_handler_registry


def import_asset(ctx: Context, path: str, destination: Optional[str] = None) -> str:
    """Import an asset from a file.
    
    Args:
        ctx: The MCP context
        path: Path to the asset file
        destination: Optional destination folder within the project
        
    Returns:
        JSON string with the result of the operation
    """
    if not path:
        error_message = "Asset path is required"
        error_reporter.report_error("ValueError", error_message, {"path": path})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "path": path
    }
    
    if destination is not None:
        params["destination"] = destination
    
    # Execute the command
    result = command_handler_registry.handle_command("import_asset", params)
    
    return json.dumps(result)


def search_asset_store(ctx: Context, query: str, category: Optional[str] = None, 
                      page: int = 1, page_size: int = 20) -> str:
    """Search the Unity Asset Store.
    
    Args:
        ctx: The MCP context
        query: Search query
        category: Optional category to filter by
        page: Page number for pagination
        page_size: Number of results per page
        
    Returns:
        JSON string with the result of the operation
    """
    if not query:
        error_message = "Search query is required"
        error_reporter.report_error("ValueError", error_message, {"query": query})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate page and page_size
    if page < 1:
        error_message = f"Page must be greater than 0, got {page}"
        error_reporter.report_error("ValueError", error_message, {"page": page})
        return json.dumps({"error": error_message, "success": False})
    
    if page_size < 1 or page_size > 100:
        error_message = f"Page size must be between 1 and 100, got {page_size}"
        error_reporter.report_error("ValueError", error_message, {"page_size": page_size})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "query": query,
        "page": page,
        "page_size": page_size
    }
    
    if category is not None:
        params["category"] = category
    
    # Execute the command
    result = command_handler_registry.handle_command("search_asset_store", params)
    
    return json.dumps(result)


def get_asset_store_categories(ctx: Context) -> str:
    """Get a list of categories from the Unity Asset Store.
    
    Args:
        ctx: The MCP context
        
    Returns:
        JSON string with the result of the operation
    """
    # Execute the command
    result = command_handler_registry.handle_command("get_asset_store_categories", {})
    
    return json.dumps(result)


def download_asset_store_package(ctx: Context, asset_id: str, destination: Optional[str] = None) -> str:
    """Download a package from the Unity Asset Store.
    
    Args:
        ctx: The MCP context
        asset_id: ID of the asset to download
        destination: Optional destination folder within the project
        
    Returns:
        JSON string with the result of the operation
    """
    if not asset_id:
        error_message = "Asset ID is required"
        error_reporter.report_error("ValueError", error_message, {"asset_id": asset_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "asset_id": asset_id
    }
    
    if destination is not None:
        params["destination"] = destination
    
    # Execute the command
    result = command_handler_registry.handle_command("download_asset_store_package", params)
    
    return json.dumps(result)


def search_polyhaven(ctx: Context, query: str, category: Optional[str] = None, 
                    page: int = 1, page_size: int = 20) -> str:
    """Search PolyHaven for assets.
    
    Args:
        ctx: The MCP context
        query: Search query
        category: Optional category to filter by (hdri, texture, model)
        page: Page number for pagination
        page_size: Number of results per page
        
    Returns:
        JSON string with the result of the operation
    """
    if not query:
        error_message = "Search query is required"
        error_reporter.report_error("ValueError", error_message, {"query": query})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate category
    valid_categories = [None, "hdri", "texture", "model"]
    if category not in valid_categories:
        error_message = f"Category must be one of {valid_categories}, got {category}"
        error_reporter.report_error("ValueError", error_message, {"category": category})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate page and page_size
    if page < 1:
        error_message = f"Page must be greater than 0, got {page}"
        error_reporter.report_error("ValueError", error_message, {"page": page})
        return json.dumps({"error": error_message, "success": False})
    
    if page_size < 1 or page_size > 100:
        error_message = f"Page size must be between 1 and 100, got {page_size}"
        error_reporter.report_error("ValueError", error_message, {"page_size": page_size})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "query": query,
        "page": page,
        "page_size": page_size
    }
    
    if category is not None:
        params["category"] = category
    
    # Execute the command
    result = command_handler_registry.handle_command("search_polyhaven", params)
    
    return json.dumps(result)


def import_polyhaven_asset(ctx: Context, asset_id: str, resolution: str = "2k", 
                          destination: Optional[str] = None) -> str:
    """Import an asset from PolyHaven.
    
    Args:
        ctx: The MCP context
        asset_id: ID of the asset to import
        resolution: Resolution of the asset (1k, 2k, 4k, 8k)
        destination: Optional destination folder within the project
        
    Returns:
        JSON string with the result of the operation
    """
    if not asset_id:
        error_message = "Asset ID is required"
        error_reporter.report_error("ValueError", error_message, {"asset_id": asset_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate resolution
    valid_resolutions = ["1k", "2k", "4k", "8k"]
    if resolution not in valid_resolutions:
        error_message = f"Resolution must be one of {valid_resolutions}, got {resolution}"
        error_reporter.report_error("ValueError", error_message, {"resolution": resolution})
        return json.dumps({"error": error_message, "success": False})
    
    # Prepare parameters
    params = {
        "asset_id": asset_id,
        "resolution": resolution
    }
    
    if destination is not None:
        params["destination"] = destination
    
    # Execute the command
    result = command_handler_registry.handle_command("import_polyhaven_asset", params)
    
    return json.dumps(result)


def create_asset_bundle(ctx: Context, assets: List[str], output_path: str, 
                       target_platform: str = "standalone") -> str:
    """Create an asset bundle from a list of assets.
    
    Args:
        ctx: The MCP context
        assets: List of asset paths or IDs to include in the bundle
        output_path: Path to save the asset bundle to
        target_platform: Target platform for the asset bundle
        
    Returns:
        JSON string with the result of the operation
    """
    if not assets:
        error_message = "Asset list is required"
        error_reporter.report_error("ValueError", error_message, {"assets": assets})
        return json.dumps({"error": error_message, "success": False})
    
    if not output_path:
        error_message = "Output path is required"
        error_reporter.report_error("ValueError", error_message, {"output_path": output_path})
        return json.dumps({"error": error_message, "success": False})
    
    # Validate target platform
    valid_platforms = ["standalone", "android", "ios", "webgl", "windows", "macos", "linux"]
    if target_platform not in valid_platforms:
        error_message = f"Target platform must be one of {valid_platforms}, got {target_platform}"
        error_reporter.report_error("ValueError", error_message, {"target_platform": target_platform})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("create_asset_bundle", {
        "assets": assets,
        "output_path": output_path,
        "target_platform": target_platform
    })
    
    return json.dumps(result)


def load_asset_bundle(ctx: Context, path: str) -> str:
    """Load an asset bundle.
    
    Args:
        ctx: The MCP context
        path: Path to the asset bundle
        
    Returns:
        JSON string with the result of the operation
    """
    if not path:
        error_message = "Asset bundle path is required"
        error_reporter.report_error("ValueError", error_message, {"path": path})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("load_asset_bundle", {
        "path": path
    })
    
    return json.dumps(result)


def get_assets_in_bundle(ctx: Context, bundle_id: str) -> str:
    """Get a list of assets in an asset bundle.
    
    Args:
        ctx: The MCP context
        bundle_id: ID of the asset bundle
        
    Returns:
        JSON string with the result of the operation
    """
    if not bundle_id:
        error_message = "Asset bundle ID is required"
        error_reporter.report_error("ValueError", error_message, {"bundle_id": bundle_id})
        return json.dumps({"error": error_message, "success": False})
    
    # Execute the command
    result = command_handler_registry.handle_command("get_assets_in_bundle", {
        "bundle_id": bundle_id
    })
    
    return json.dumps(result)


def register_asset_tools(tool_registry):
    """Register asset tools with the tool registry.
    
    Args:
        tool_registry: The tool registry to register tools with
    """
    # Import asset
    tool_registry.register_tool(
        name="import_asset",
        func=import_asset,
        category="asset",
        description="Import an asset from a file",
        parameters={
            "path": {
                "description": "Path to the asset file",
                "type": "string",
                "required": True
            },
            "destination": {
                "description": "Optional destination folder within the project",
                "type": "string",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Search asset store
    tool_registry.register_tool(
        name="search_asset_store",
        func=search_asset_store,
        category="asset",
        description="Search the Unity Asset Store",
        parameters={
            "query": {
                "description": "Search query",
                "type": "string",
                "required": True
            },
            "category": {
                "description": "Optional category to filter by",
                "type": "string",
                "required": False
            },
            "page": {
                "description": "Page number for pagination",
                "type": "integer",
                "required": False
            },
            "page_size": {
                "description": "Number of results per page",
                "type": "integer",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get asset store categories
    tool_registry.register_tool(
        name="get_asset_store_categories",
        func=get_asset_store_categories,
        category="asset",
        description="Get a list of categories from the Unity Asset Store",
        parameters={},
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Download asset store package
    tool_registry.register_tool(
        name="download_asset_store_package",
        func=download_asset_store_package,
        category="asset",
        description="Download a package from the Unity Asset Store",
        parameters={
            "asset_id": {
                "description": "ID of the asset to download",
                "type": "string",
                "required": True
            },
            "destination": {
                "description": "Optional destination folder within the project",
                "type": "string",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Search PolyHaven
    tool_registry.register_tool(
        name="search_polyhaven",
        func=search_polyhaven,
        category="asset",
        description="Search PolyHaven for assets",
        parameters={
            "query": {
                "description": "Search query",
                "type": "string",
                "required": True
            },
            "category": {
                "description": "Optional category to filter by (hdri, texture, model)",
                "type": "string",
                "required": False
            },
            "page": {
                "description": "Page number for pagination",
                "type": "integer",
                "required": False
            },
            "page_size": {
                "description": "Number of results per page",
                "type": "integer",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Import PolyHaven asset
    tool_registry.register_tool(
        name="import_polyhaven_asset",
        func=import_polyhaven_asset,
        category="asset",
        description="Import an asset from PolyHaven",
        parameters={
            "asset_id": {
                "description": "ID of the asset to import",
                "type": "string",
                "required": True
            },
            "resolution": {
                "description": "Resolution of the asset (1k, 2k, 4k, 8k)",
                "type": "string",
                "required": False
            },
            "destination": {
                "description": "Optional destination folder within the project",
                "type": "string",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Create asset bundle
    tool_registry.register_tool(
        name="create_asset_bundle",
        func=create_asset_bundle,
        category="asset",
        description="Create an asset bundle from a list of assets",
        parameters={
            "assets": {
                "description": "List of asset paths or IDs to include in the bundle",
                "type": "array",
                "required": True
            },
            "output_path": {
                "description": "Path to save the asset bundle to",
                "type": "string",
                "required": True
            },
            "target_platform": {
                "description": "Target platform for the asset bundle",
                "type": "string",
                "required": False
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Load asset bundle
    tool_registry.register_tool(
        name="load_asset_bundle",
        func=load_asset_bundle,
        category="asset",
        description="Load an asset bundle",
        parameters={
            "path": {
                "description": "Path to the asset bundle",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
    
    # Get assets in bundle
    tool_registry.register_tool(
        name="get_assets_in_bundle",
        func=get_assets_in_bundle,
        category="asset",
        description="Get a list of assets in an asset bundle",
        parameters={
            "bundle_id": {
                "description": "ID of the asset bundle",
                "type": "string",
                "required": True
            }
        },
        returns={
            "description": "JSON string with the result of the operation",
            "type": "string"
        }
    )
