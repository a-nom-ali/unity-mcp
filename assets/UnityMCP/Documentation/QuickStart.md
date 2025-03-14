# UnityMCP Quick Start Guide

This guide will help you get started with UnityMCP, the Unity Model Context Protocol integration that enables AI-assisted game development.

## Installation

### Prerequisites

- Unity 2020.3 or newer
- Python 3.10 or newer
- MCP package: `pip install mcp[cli]>=1.3.0`

### Step 1: Import UnityMCP into your Unity Project

1. Copy the `UnityMCP` folder into your Unity project's `Assets` folder
2. Wait for Unity to import all the scripts and assets

### Step 2: Create the UnityMCP Server

1. In Unity, go to `Tools > Unity MCP > Create Server`
2. This will create a GameObject with the `UnityMCPBrain` component
3. The server is now ready to be started

### Step 3: Configure Claude

1. Open Claude for Desktop
2. Go to `Settings > Developer > Edit Config`
3. Open the `claude_desktop_config.json` file
4. Add the following to the configuration:

```json
{
    "mcpServers": {
        "unity": {
            "command": "python",
            "args": [
                "path/to/your/project/Assets/UnityMCP/Python/unity_mcp_server.py"
            ]
        }
    }
}
```

Replace `path/to/your/project` with the actual path to your Unity project.

## Using UnityMCP

### Starting the Connection

1. In Unity, select the UnityMCPServer GameObject
2. Click "Start Server" in the Inspector
3. You should see a message in the console: "Unity MCP Server started on localhost:9876"

### Using with Claude

1. Open Claude for Desktop
2. You should see a hammer icon in the toolbar, indicating that the Unity MCP tools are available
3. You can now ask Claude to interact with your Unity scene

### Example Commands

Try asking Claude to:

- "Create a simple scene with a red cube, a blue sphere, and a green cylinder"
- "Add a point light above the scene with a warm color"
- "Create a camera that looks at the cube and make it the main camera"

## Troubleshooting

### Connection Issues

If Claude cannot connect to Unity:

1. Make sure the Unity server is running (check the Unity console)
2. Verify that the port (default: 9876) is not blocked by a firewall
3. Check that the path in the Claude configuration is correct
4. Try restarting both Unity and Claude

### Command Errors

If commands are not working correctly:

1. Check the Unity console for error messages
2. Make sure you're using the correct command syntax
3. Try simpler commands first to verify the connection is working

## Next Steps

- Explore the [Command Reference](CommandReference.md) to see all available commands
- Learn about the [Subsystems](SubsystemReference.md) that extend UnityMCP's functionality
- Understand the [Architecture](Architecture.md) to see how UnityMCP works
- Try the [Assistant](assistant.md) features for AI-powered scene analysis and suggestions

## Getting Help

If you encounter issues or have questions:

1. Check the [Documentation](index.md) for detailed information
2. Look for error messages in the Unity console
3. Try simplifying your requests to Claude 