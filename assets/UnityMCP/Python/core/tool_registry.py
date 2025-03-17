#!/usr/bin/env python3
"""
Tool registry for Unity MCP.
"""

import inspect
import json
import logging
import time
from dataclasses import dataclass
from typing import Any, Callable, Dict, List, Optional, Set, Tuple, Type

from mcp.server.fastmcp import Context, FastMCP

from ..error_reporter import error_reporter
from .command_handler import command_handler_registry
from .config import config

# Initialize logger
logger = logging.getLogger("unity_mcp.tool_registry")


@dataclass
class ToolMetrics:
    """Metrics for a tool."""
    call_count: int = 0
    total_execution_time: float = 0.0
    average_execution_time: float = 0.0
    error_count: int = 0
    last_called: Optional[float] = None


class ToolRegistry:
    """Registry for MCP tools."""
    
    def __init__(self, mcp: FastMCP):
        """Initialize the tool registry.
        
        Args:
            mcp: The MCP instance
        """
        self.mcp = mcp
        self.tools = {}
        self.tool_metrics = {}
        self.tool_categories = {
            "object": set(),
            "material": set(),
            "animation": set(),
            "camera": set(),
            "light": set(),
            "asset": set(),
            "scene": set(),
            "utility": set()
        }
    
    def register_tool(self, name: str, func: Callable, category: str = "utility", **kwargs):
        """Register a tool with MCP.
        
        Args:
            name: The name of the tool
            func: The function to call when the tool is invoked
            category: The category of the tool
            **kwargs: Additional arguments to pass to mcp.tool
        """
        # Register the tool with MCP
        wrapped_func = self._create_tool_wrapper(name, func)
        self.mcp.tool(name=name, **kwargs)(wrapped_func)
        
        # Store the tool in our registry
        self.tools[name] = {
            "func": func,
            "category": category,
            "metadata": kwargs
        }
        
        # Add the tool to the appropriate category
        if category in self.tool_categories:
            self.tool_categories[category].add(name)
        else:
            self.tool_categories["utility"].add(name)
        
        # Initialize metrics for the tool
        self.tool_metrics[name] = ToolMetrics()
        
        logger.info(f"Registered tool {name} in category {category}")
    
    def _create_tool_wrapper(self, name: str, func: Callable) -> Callable:
        """Create a wrapper for a tool function that handles metrics and error reporting.
        
        Args:
            name: The name of the tool
            func: The function to wrap
            
        Returns:
            The wrapped function
        """
        def wrapper(ctx: Context, *args, **kwargs):
            start_time = time.time()
            
            try:
                # Update metrics
                metrics = self.tool_metrics[name]
                metrics.call_count += 1
                metrics.last_called = start_time
                
                # Execute the function
                result = func(ctx, *args, **kwargs)
                
                # Update execution time metrics
                execution_time = time.time() - start_time
                metrics.total_execution_time += execution_time
                metrics.average_execution_time = metrics.total_execution_time / metrics.call_count
                
                # Log telemetry if enabled
                if config.telemetry.enabled and config.telemetry.collect_performance_metrics:
                    logger.info(f"Tool {name} executed in {execution_time:.4f}s")
                
                return result
            except Exception as e:
                # Update error metrics
                metrics = self.tool_metrics[name]
                metrics.error_count += 1
                
                # Report the error
                error_info = error_reporter.format_exception(e)
                error_id = error_reporter.report_error(
                    error_info["type"],
                    error_info["message"],
                    {"tool": name, "args": args, "kwargs": kwargs},
                    error_info["stack_trace"]
                )
                
                logger.error(f"Error executing tool {name}: {str(e)}")
                
                # Return an error response
                return json.dumps({
                    "error": f"Error executing tool {name}: {str(e)}",
                    "error_id": error_id,
                    "success": False
                })
        
        # Copy the function signature and docstring
        wrapper.__name__ = func.__name__
        wrapper.__doc__ = func.__doc__
        wrapper.__signature__ = inspect.signature(func)
        
        return wrapper
    
    def get_tool_categories(self) -> Dict[str, Set[str]]:
        """Get all tool categories and their tools.
        
        Returns:
            A dictionary mapping category names to sets of tool names
        """
        return self.tool_categories
    
    def get_tool_metrics(self, name: Optional[str] = None) -> Dict[str, ToolMetrics]:
        """Get metrics for a tool or all tools.
        
        Args:
            name: The name of the tool, or None to get metrics for all tools
            
        Returns:
            A dictionary mapping tool names to metrics
        """
        if name:
            if name in self.tool_metrics:
                return {name: self.tool_metrics[name]}
            return {}
        
        return self.tool_metrics
    
    def get_tool_metadata(self, name: str) -> Optional[Dict]:
        """Get metadata for a tool.
        
        Args:
            name: The name of the tool
            
        Returns:
            The tool metadata, or None if the tool doesn't exist
        """
        if name in self.tools:
            return self.tools[name]["metadata"]
        return None
    
    def get_all_tools(self) -> List[Dict]:
        """Get information about all registered tools.
        
        Returns:
            A list of dictionaries containing tool information
        """
        tools = []
        
        for name, tool_info in self.tools.items():
            metadata = tool_info["metadata"]
            
            # Get parameter information
            parameters = []
            sig = inspect.signature(tool_info["func"])
            
            for param_name, param in list(sig.parameters.items())[1:]:  # Skip 'ctx' parameter
                param_info = {
                    "name": param_name,
                    "type": str(param.annotation).replace("<class '", "").replace("'>", ""),
                    "required": param.default == inspect.Parameter.empty,
                    "default": None if param.default == inspect.Parameter.empty else param.default,
                    "description": metadata.get("parameters", {}).get(param_name, {}).get("description", "")
                }
                parameters.append(param_info)
            
            # Get return information
            return_info = {
                "type": str(sig.return_annotation).replace("<class '", "").replace("'>", ""),
                "description": metadata.get("returns", {}).get("description", "")
            }
            
            # Create tool information
            tool_info = {
                "name": name,
                "description": tool_info["func"].__doc__ or "",
                "category": tool_info["category"],
                "parameters": parameters,
                "returns": return_info
            }
            
            tools.append(tool_info)
        
        return tools
    
    def generate_api_documentation(self, output_format: str = "markdown", include_examples: bool = True) -> str:
        """Generate API documentation for all tools.
        
        Args:
            output_format: The output format (markdown or json)
            include_examples: Whether to include examples in the documentation
            
        Returns:
            The generated documentation
        """
        tools = self.get_all_tools()
        
        if output_format.lower() == "json":
            return json.dumps(tools, indent=2)
        else:
            return self._generate_markdown_documentation(tools, include_examples)
    
    def _generate_markdown_documentation(self, tools: List[Dict], include_examples: bool) -> str:
        """Generate markdown documentation for the tools.
        
        Args:
            tools: The tools to document
            include_examples: Whether to include examples in the documentation
            
        Returns:
            The generated documentation
        """
        doc = "# Unity MCP API Documentation\n\n"
        doc += "This document provides a comprehensive reference for all available tools in the Unity MCP integration.\n\n"
        
        # Table of contents
        doc += "## Table of Contents\n\n"
        
        # Group tools by category
        categories = {}
        for tool in tools:
            category = tool["category"]
            if category not in categories:
                categories[category] = []
            categories[category].append(tool)
        
        # Add categories to table of contents
        for category in sorted(categories.keys()):
            doc += f"- [{category.capitalize()} Tools](#{category.lower()}-tools)\n"
        doc += "\n"
        
        # Document each category
        for category in sorted(categories.keys()):
            doc += f"## {category.capitalize()} Tools\n\n"
            
            # Document each tool in the category
            for tool in sorted(categories[category], key=lambda t: t["name"]):
                doc += f"### {tool['name']}\n\n"
                doc += f"{tool['description']}\n\n"
                
                # Parameters table
                if tool['parameters']:
                    doc += "#### Parameters\n\n"
                    doc += "| Name | Type | Required | Default | Description |\n"
                    doc += "|------|------|----------|---------|-------------|\n"
                    
                    for param in tool['parameters']:
                        default = str(param['default']) if param['default'] is not None else "N/A"
                        required = "Yes" if param['required'] else "No"
                        doc += f"| {param['name']} | {param['type']} | {required} | {default} | {param['description']} |\n"
                    
                    doc += "\n"
                
                # Returns
                doc += "#### Returns\n\n"
                doc += f"Type: {tool['returns']['type']}\n\n"
                doc += f"{tool['returns']['description']}\n\n"
                
                # Example
                if include_examples:
                    doc += "#### Example\n\n"
                    doc += f"```python\n"
                    doc += f"result = {tool['name']}("
                    
                    param_examples = []
                    for param in tool['parameters']:
                        if param['type'] == "str":
                            example_value = f'"{param["name"]}_example"'
                        elif param['type'] == "int":
                            example_value = "1"
                        elif param['type'] == "float":
                            example_value = "1.0"
                        elif param['type'] == "bool":
                            example_value = "True"
                        elif param['type'] == "List":
                            example_value = "[]"
                        elif param['type'] == "Dict":
                            example_value = "{}"
                        else:
                            example_value = "None"
                        
                        param_examples.append(f"{param['name']}={example_value}")
                    
                    doc += ", ".join(param_examples)
                    doc += ")\n"
                    
                    # Add example result parsing
                    doc += "result_json = json.loads(result)\n"
                    doc += "# Process the result\n"
                    doc += "```\n\n"
                
                doc += "---\n\n"
        
        # Add footer
        import datetime
        doc += f"\n\n*Documentation generated on {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}*\n"
        
        return doc


# Global tool registry instance
_tool_registry = None


def get_tool_registry(mcp: FastMCP) -> ToolRegistry:
    """Get or create the tool registry.
    
    Args:
        mcp: The MCP instance
        
    Returns:
        The tool registry
    """
    global _tool_registry
    
    if _tool_registry is None:
        _tool_registry = ToolRegistry(mcp)
    
    return _tool_registry
