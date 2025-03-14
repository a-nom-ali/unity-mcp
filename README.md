# UnityMCP - Unity Model Context Protocol Integration

UnityMCP connects Unity to Claude AI through the Model Context Protocol (MCP), allowing Claude to directly interact with and control Unity. This integration enables prompt-assisted 3D modeling, scene creation, and manipulation.

## Release notes (1.0.0)

- Initial release of UnityMCP, transformed from the BlenderMCP project
- Comprehensive Unity integration with support for objects, materials, lighting, cameras, and more
- Intelligent scene analysis and suggestions through the Assistant subsystem
- Modular architecture for easy extension

## Features

- **Two-way communication**: Connect Claude AI to Unity through a socket-based server
- **Intelligent command handling**: Advanced command parsing and execution with error handling
- **Context awareness**: Maintains state and context between commands
- **Extensible architecture**: Modular subsystem design for easy expansion
- **Scene analysis**: AI-powered analysis and suggestions for your Unity scenes

## Components

The system consists of two main components:

1. **Unity Plugin (`Assets/UnityMCP`)**: A C# plugin that creates a socket server within Unity to receive and execute commands
2. **MCP Server (`Assets/UnityMCP/Python/unity_mcp_server.py`)**: A Python server that implements the Model Context Protocol and connects to the Unity plugin

## Installation

### Prerequisites

- Unity 2020.3 or newer
- Python 3.10 or newer
- MCP package: `pip install mcp[cli]>=1.3.0`

### Installing the Unity Plugin

1. Import the UnityMCP folder into your Unity project's Assets folder
2. In Unity, go to Tools > Unity MCP > Create Server
3. This will create a GameObject with the UnityMCPBrain component

### Setting Up the MCP Server

#### Claude for Desktop Integration

Go to Claude > Settings > Developer > Edit Config > claude_desktop_config.json to include the following:

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

## Usage

### Starting the Connection

1. In Unity, select the UnityMCPServer GameObject
2. Click "Start Server" in the Inspector
3. Make sure the MCP server is running in your terminal or through Claude

### Using with Claude

Once the config file has been set on Claude, and the server is running in Unity, you will see a hammer icon with tools for the Unity MCP.

### Example Commands

Here are some examples of what you can ask Claude to do:

- "Create a simple scene with a red cube, a blue sphere, and a green cylinder"
- "Add a point light above the scene with a warm color"
- "Create a camera that looks at the cube and make it the main camera"
- "Make the cube twice as large and rotate it 45 degrees around the Y axis"
- "Apply a metallic material to the sphere with a gold color"
- "Analyze my scene and give me insights on how to improve it"
- "Suggest a creative way to enhance my scene's visual appeal"

## Documentation

Comprehensive documentation is available in the `Assets/UnityMCP/Documentation` folder:

- **Quick Start Guide**: Get up and running quickly
- **Command Reference**: Complete list of available commands
- **Subsystem Reference**: Information about available subsystems
- **Architecture**: Overview of the UnityMCP architecture
- **Assistant Guide**: How to use the AI-powered scene analysis features

## Troubleshooting

- **Connection issues**: Make sure the Unity server is running, and the MCP server is configured on Claude
- **Timeout errors**: Try simplifying your requests or breaking them into smaller steps
- **Command errors**: Check the Unity console for detailed error messages
- **Performance issues**: For complex operations, break them down into smaller commands

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Acknowledgments

- This project is inspired by the BlenderMCP project
- Thanks to the Model Context Protocol team for creating the MCP standard

## License

This project is licensed under the MIT License - see the LICENSE file for details.
