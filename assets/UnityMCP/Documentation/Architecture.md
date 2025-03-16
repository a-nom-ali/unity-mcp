# UnityMCP Architecture

This document provides an overview of the UnityMCP architecture and how the different components interact.

## Overview

UnityMCP uses a modular architecture with these key components:

1. **UnityMCPBrain**: Central intelligence system that coordinates all subsystems
2. **Command Handlers**: Process specific types of commands (objects, materials, lighting, etc.)
3. **Subsystems**: Specialized modules for different aspects of Unity (animation, prefabs, etc.)
4. **MCP Server**: Python server that implements the Model Context Protocol
5. **Socket Communication**: Bidirectional communication between Claude and Unity

## Component Diagram

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│             │     │             │     │             │
│  Claude AI  │◄────┤  MCP Server │◄────┤ Unity Brain │
│             │     │             │     │             │
└─────────────┘     └─────────────┘     └──────┬──────┘
                                               │
                                               │
                                        ┌──────┴──────┐
                                        │             │
                                        │ Subsystems  │
                                        │             │
                                        └─────────────┘
```

## Communication Flow

1. Claude AI sends a command to the MCP Server
2. The MCP Server forwards the command to the Unity Brain
3. The Unity Brain routes the command to the appropriate Command Handler
4. The Command Handler executes the command, potentially using Subsystems
5. The result is returned back through the same path to Claude AI

## Key Classes

### UnityMCPBrain

The central coordinator for the entire system. It:

- Manages the socket server for communication
- Discovers and initializes subsystems
- Routes commands to the appropriate handlers
- Maintains context and state between commands

### CommandHandler

Base class for all command handlers. Each handler:

- Processes commands for a specific domain
- Uses reflection to discover command methods
- Parses parameters and executes commands
- Returns results in a standardized format

### IUnityMCPSubsystem

Interface for all subsystems. Each subsystem:

- Provides specialized functionality for a specific aspect of Unity
- Can provide command handlers for its domain
- Is automatically discovered and initialized by the brain

### UnityMCPContext

Maintains context and state between commands:

- Tracks focused and selected objects
- Stores variables for use across commands
- Provides session and project information

## Extending UnityMCP

### Adding a New Subsystem

1. Create a new class that implements `IUnityMCPSubsystem` and `ICommandProvider`
2. Implement the required methods
3. Create a command handler for your subsystem
4. The subsystem will be automatically discovered and initialized

### Adding New Commands

1. Create a method in a command handler class
2. Add the `[CommandMethod]` attribute to the method
3. The method will be automatically discovered and available as a command

## Security Considerations

- The socket server accepts connections from any client on the specified port
- Be cautious when exposing the server to networks
- Consider adding authentication for production use
- Always save your Unity project before using the MCP integration
