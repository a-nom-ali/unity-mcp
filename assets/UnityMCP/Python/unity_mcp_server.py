#!/usr/bin/env python3
"""Unity integration through the Model Context Protocol."""

import argparse
import asyncio
import json
import logging
import os
import sys
from contextlib import asynccontextmanager
from typing import AsyncIterator
from mcp.server.fastmcp import Context, FastMCP

# Import core modules
from .core.config import config
from .core.error_reporter import error_reporter
from .core.command_handler import command_handler_registry
from .core.tool_registry import tool_registry

# Import connection module
from .connection import UnityConnection, get_unity_connection

# Import tools package
from .tools import register_all_tools

# Import batch tools module
from .batch_tools import register_batch_tools

__version__ = "1.0.0"

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger("unity_mcp")


@asynccontextmanager
async def server_lifespan(server: FastMCP) -> AsyncIterator[None]:
    """Lifecycle manager for the MCP server.
    
    This context manager handles the lifecycle of the MCP server,
    including connection setup and teardown.
    
    Args:
        server: The FastMCP server instance
    """
    # Server startup
    logger.info("Starting Unity MCP server...")
    
    # Initialize connection
    connection = get_unity_connection()
    if not connection.connect():
        logger.warning("Failed to connect to Unity. Will retry when needed.")
    
    # Register all tools
    register_all_tools()
    register_batch_tools(tool_registry)
    
    logger.info(f"Unity MCP server started (version {__version__})")
    
    try:
        yield
    finally:
        # Server shutdown
        logger.info("Shutting down Unity MCP server...")
        connection = get_unity_connection()
        connection.disconnect()
        logger.info("Unity MCP server shutdown complete")


def generate_api_documentation(
        ctx: Context,
        output_format: str = "markdown",
        include_examples: bool = True
) -> str:
    """Generate comprehensive API documentation for all Unity MCP tools.
    
    Args:
        ctx: The MCP context
        output_format: Format of the documentation (markdown or json)
        include_examples: Whether to include usage examples in the documentation
        
    Returns:
        A string containing the API documentation in the specified format
    """
    # Get all registered tools
    tools = tool_registry.get_all_tools()
    
    if output_format.lower() == "json":
        # Generate JSON documentation
        doc = {
            "version": __version__,
            "tools": []
        }
        
        for tool_name, tool_info in tools.items():
            tool_doc = {
                "name": tool_name,
                "description": tool_info.get("description", ""),
                "category": tool_info.get("category", ""),
                "parameters": tool_info.get("parameters", {}),
                "returns": tool_info.get("returns", {})
            }
            
            if include_examples and "example" in tool_info:
                tool_doc["example"] = tool_info["example"]
                
            doc["tools"].append(tool_doc)
            
        return json.dumps(doc, indent=2)
    else:
        # Generate Markdown documentation
        return tool_registry.generate_markdown_documentation(include_examples)


def unity_assistant_guide(ctx: Context) -> str:
    """Provides guidance on using the AI assistant features in Unity.
    
    Returns:
        A string containing guidance on using the AI assistant features
    """
    guide = """
# Unity AI Assistant Guide

The Unity AI Assistant provides several AI-powered features to help you with your Unity projects:

## General Features

1. **Scene Analysis**: Get insights about your current scene structure and suggestions for improvement.
2. **Creative Suggestions**: Get creative ideas for your game objects, visual design, gameplay mechanics, or story elements.
3. **Code Generation**: Generate C# scripts based on natural language descriptions of functionality.
4. **Asset Recommendations**: Get recommendations for assets from the Unity Asset Store or PolyHaven based on your project needs.

## Using the AI Assistant

To use the AI Assistant, you can:

1. **Ask Questions**: Type natural language questions about Unity, game development, or your project.
2. **Request Actions**: Ask the assistant to perform specific actions like creating objects, modifying properties, or generating code.
3. **Get Suggestions**: Ask for creative suggestions or recommendations for your project.

## Examples

- "Analyze my current scene and suggest improvements"
- "Give me creative ideas for a main character in a platformer game"
- "Generate a script for a simple third-person camera controller"
- "Recommend assets for a sci-fi environment"
- "Create a basic level with platforms and obstacles"

## Tips for Best Results

- Be specific in your requests
- Provide context about your project when relevant
- Break complex tasks into smaller steps
- Use the assistant's suggestions as a starting point and customize them to fit your needs
"""
    return guide


# Create the MCP server with lifespan support
mcp = FastMCP(
    title="Unity MCP",
    description="Model Context Protocol integration for Unity",
    version=__version__,
    lifespan=server_lifespan
)

# Register API documentation tool
tool_registry.register_tool(
    name="generate_api_documentation",
    func=generate_api_documentation,
    category="utility",
    description="Generate comprehensive API documentation for all Unity MCP tools",
    parameters={
        "output_format": {
            "description": "Format of the documentation (markdown or json)",
            "type": "string",
            "required": False
        },
        "include_examples": {
            "description": "Whether to include usage examples in the documentation",
            "type": "boolean",
            "required": False
        }
    },
    returns={
        "description": "A string containing the API documentation in the specified format",
        "type": "string"
    }
)

# Register assistant guide tool
tool_registry.register_tool(
    name="unity_assistant_guide",
    func=unity_assistant_guide,
    category="utility",
    description="Provides guidance on using the AI assistant features in Unity",
    parameters={},
    returns={
        "description": "A string containing guidance on using the AI assistant features",
        "type": "string"
    }
)


def main():
    """Main entry point for the Unity MCP server."""
    parser = argparse.ArgumentParser(description="Unity MCP Server")
    parser.add_argument("--host", default="localhost", help="Host to bind to")
    parser.add_argument("--port", type=int, default=8000, help="Port to bind to")
    parser.add_argument("--unity-host", default="localhost", help="Unity host to connect to")
    parser.add_argument("--unity-port", type=int, default=9000, help="Unity port to connect to")
    parser.add_argument("--log-level", default="info", help="Logging level")
    parser.add_argument("--version", action="store_true", help="Show version and exit")
    
    args = parser.parse_args()
    
    if args.version:
        print(f"Unity MCP Server version {__version__}")
        sys.exit(0)
    
    # Set up logging level
    log_level = getattr(logging, args.log_level.upper(), logging.INFO)
    logging.getLogger("unity_mcp").setLevel(log_level)
    
    # Set up Unity connection parameters
    config.set("connection.host", args.unity_host)
    config.set("connection.port", args.unity_port)
    
    # Start the server
    import uvicorn
    uvicorn.run(
        "unity_mcp_server:mcp",
        host=args.host,
        port=args.port,
        log_level=args.log_level.lower(),
        reload=False
    )


if __name__ == "__main__":
    main()