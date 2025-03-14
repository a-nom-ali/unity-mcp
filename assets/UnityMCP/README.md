# UnityMCP - Advanced Unity Model Context Protocol Integration

UnityMCP is a powerful integration between Unity and Claude AI through the Model Context Protocol (MCP), enabling AI-assisted game development and 3D scene creation. This advanced implementation provides a comprehensive set of tools for manipulating Unity scenes, objects, materials, lighting, cameras, animations, and more.

## Features

### Core Features
- **Two-way communication**: Robust socket-based communication between Claude AI and Unity
- **Intelligent command handling**: Advanced command parsing and execution with error handling
- **Context awareness**: Maintains state and context between commands
- **Extensible architecture**: Modular subsystem design for easy expansion

### Object Manipulation
- Create, modify, and delete 3D objects with precise control
- Manipulate transforms (position, rotation, scale)
- Parent/child relationships and hierarchy management
- Component addition and removal

### Visual Systems
- **Materials**: Create and apply materials with PBR properties
- **Lighting**: Comprehensive lighting control (point, directional, spot, area lights)
- **Cameras**: Camera creation, positioning, and configuration
- **Visual effects**: Post-processing and environment settings

### Animation
- Play and control animations on objects
- Animation parameter manipulation
- Animation state machine interaction
- Timeline and keyframe creation

### Asset Management
- Prefab instantiation and management
- Asset importing and exporting
- Project asset discovery and manipulation
- External asset integration

## Architecture

UnityMCP uses a modular architecture with these key components:

1. **UnityMCPBrain**: Central intelligence system that coordinates all subsystems
2. **Command Handlers**: Process specific types of commands (objects, materials, lighting, etc.)
3. **Subsystems**: Specialized modules for different aspects of Unity (animation, prefabs, etc.)
4. **MCP Server**: Python server that implements the Model Context Protocol
5. **Socket Communication**: Bidirectional communication between Claude and Unity

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

### Available Tools

#### Core Tools
- `get_system_info` - Get information about the Unity system
- `get_scene_info` - Get information about the current scene
- `get_object_info` - Get detailed information about a specific object

#### Object Tools
- `create_object` - Create a new primitive object
- `modify_object` - Modify an existing object's transform
- `delete_object` - Remove an object from the scene

#### Visual Tools
- `set_material` - Apply or create materials for objects
- `create_light` - Add a light to the scene
- `create_camera` - Add a camera to the scene
- `camera_look_at` - Make a camera look at a specific object

#### Asset Tools
- `instantiate_prefab` - Instantiate a prefab in the scene

#### Animation Tools
- `play_animation` - Play an animation on an object

#### Assistant Tools
- `get_insights` - Get insights and suggestions about your scene
- `get_analysis` - Get a detailed analysis of your project
- `get_creative_suggestion` - Get a creative suggestion for your scene

### Example Commands

Here are some examples of what you can ask Claude to do:

- "Create a simple scene with a red cube, a blue sphere, and a green cylinder"
- "Add a point light above the scene with a warm color"
- "Create a camera that looks at the cube and make it the main camera"
- "Make the cube twice as large and rotate it 45 degrees around the Y axis"
- "Apply a metallic material to the sphere with a gold color"
- "Create a prefab from the cube and instantiate three copies in different positions"
- "Play the 'Idle' animation on the character model"
- "Analyze my scene and give me insights on how to improve it"
- "Suggest a creative way to enhance my scene's visual appeal"

## Advanced Usage

### Subsystem Extensions

UnityMCP can be extended with additional subsystems:

1. Create a new class that implements `IUnityMCPSubsystem` and `ICommandProvider`
2. Implement the required methods
3. The subsystem will be automatically discovered and initialized

### Custom Command Handlers

You can create custom command handlers:

1. Create a new class that extends `CommandHandler`
2. Add methods with the `[CommandMethod]` attribute
3. Register the handler with the brain using `RegisterCommandHandler`

### Scene Analysis and Insights

The UnityMCP Assistant subsystem provides intelligent analysis of your scene:

1. Automatically analyzes composition, lighting, materials, and performance
2. Provides insights and suggestions for improvement
3. Offers creative ideas based on your current project
4. Helps maintain best practices and optimize workflow

## Troubleshooting

- **Connection issues**: Make sure the Unity server is running, and the MCP server is configured on Claude
- **Timeout errors**: Try simplifying your requests or breaking them down into smaller steps
- **Command errors**: Check the Unity console for detailed error messages
- **Performance issues**: For complex operations, break them down into smaller commands

## Technical Details

### Communication Protocol

The system uses a JSON-based protocol over TCP sockets:

- **Commands** are sent as JSON objects with a `type` and `parameters`
- **Responses** are JSON objects with a `status` and `result` or `message`

### Command Structure

Commands follow a domain-based structure:
