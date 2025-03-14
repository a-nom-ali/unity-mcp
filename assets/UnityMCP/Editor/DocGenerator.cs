using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Documentation generator for UnityMCP
    /// </summary>
    public class DocGenerator : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool generateCommandDocs = true;
        private bool generateSubsystemDocs = true;
        private bool generateArchitectureDocs = true;
        private string outputPath = "Assets/UnityMCP/Documentation";
        private string lastGeneratedPath = "";
        
        [MenuItem("Tools/Unity MCP/Generate Documentation")]
        public static void ShowWindow()
        {
            GetWindow<DocGenerator>("UnityMCP Documentation Generator");
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("UnityMCP Documentation Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("This tool generates documentation for the UnityMCP system based on code comments and structure.", MessageType.Info);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Documentation Options", EditorStyles.boldLabel);
            generateCommandDocs = EditorGUILayout.Toggle("Generate Command Docs", generateCommandDocs);
            generateSubsystemDocs = EditorGUILayout.Toggle("Generate Subsystem Docs", generateSubsystemDocs);
            generateArchitectureDocs = EditorGUILayout.Toggle("Generate Architecture Docs", generateArchitectureDocs);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
            outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Generate Documentation", GUILayout.Height(30)))
            {
                GenerateDocumentation();
            }
            
            if (!string.IsNullOrEmpty(lastGeneratedPath))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox($"Documentation generated at: {lastGeneratedPath}", MessageType.Info);
                
                if (GUILayout.Button("Open Documentation Folder"))
                {
                    EditorUtility.RevealInFinder(lastGeneratedPath);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void GenerateDocumentation()
        {
            try
            {
                // Create output directory if it doesn't exist
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }
                
                // Generate documentation
                if (generateCommandDocs)
                {
                    GenerateCommandDocumentation();
                }
                
                if (generateSubsystemDocs)
                {
                    GenerateSubsystemDocumentation();
                }
                
                if (generateArchitectureDocs)
                {
                    GenerateArchitectureDocumentation();
                }
                
                // Generate index file
                GenerateIndexFile();
                
                lastGeneratedPath = outputPath;
                
                Debug.Log($"Documentation generated successfully at: {outputPath}");
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating documentation: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Documentation Error", $"Error generating documentation: {e.Message}", "OK");
            }
        }
        
        private void GenerateCommandDocumentation()
        {
            var commandHandlers = FindAllCommandHandlers();
            var sb = new StringBuilder();
            
            sb.AppendLine("# UnityMCP Command Reference");
            sb.AppendLine();
            sb.AppendLine("This document provides a reference for all available commands in the UnityMCP system.");
            sb.AppendLine();
            sb.AppendLine("## Command Structure");
            sb.AppendLine();
            sb.AppendLine("Commands follow a domain-based structure:");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine("domain.action");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("For example:");
            sb.AppendLine("- `object.CreatePrimitive` - Create a primitive object");
            sb.AppendLine("- `material.SetMaterial` - Set a material on an object");
            sb.AppendLine();
            sb.AppendLine("## Available Commands");
            sb.AppendLine();
            
            // Group commands by domain
            var commandsByDomain = new Dictionary<string, List<CommandInfo>>();
            
            foreach (var handler in commandHandlers)
            {
                string domain = GetDomainFromHandlerName(handler.Key);
                var commands = GetCommandsFromHandler(handler.Value);
                
                if (!commandsByDomain.ContainsKey(domain))
                {
                    commandsByDomain[domain] = new List<CommandInfo>();
                }
                
                commandsByDomain[domain].AddRange(commands);
            }
            
            // Generate documentation for each domain
            foreach (var domain in commandsByDomain.Keys.OrderBy(d => d))
            {
                sb.AppendLine($"### {domain} Commands");
                sb.AppendLine();
                
                foreach (var command in commandsByDomain[domain].OrderBy(c => c.Name))
                {
                    sb.AppendLine($"#### `{domain}.{command.Name}`");
                    sb.AppendLine();
                    
                    if (!string.IsNullOrEmpty(command.Description))
                    {
                        sb.AppendLine(command.Description);
                        sb.AppendLine();
                    }
                    
                    if (command.Parameters.Count > 0)
                    {
                        sb.AppendLine("**Parameters:**");
                        sb.AppendLine();
                        
                        foreach (var param in command.Parameters)
                        {
                            string defaultValue = param.HasDefaultValue ? $" (default: {FormatDefaultValue(param.DefaultValue)})" : "";
                            sb.AppendLine($"- `{param.Name}` ({param.Type}){defaultValue}: {param.Description}");
                        }
                        
                        sb.AppendLine();
                    }
                    
                    sb.AppendLine("**Returns:**");
                    sb.AppendLine();
                    sb.AppendLine(command.ReturnDescription);
                    sb.AppendLine();
                    
                    if (!string.IsNullOrEmpty(command.Example))
                    {
                        sb.AppendLine("**Example:**");
                        sb.AppendLine();
                        sb.AppendLine("```json");
                        sb.AppendLine(command.Example);
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }
                }
            }
            
            // Write to file
            string filePath = Path.Combine(outputPath, "CommandReference.md");
            File.WriteAllText(filePath, sb.ToString());
        }
        
        private void GenerateSubsystemDocumentation()
        {
            var subsystems = FindAllSubsystems();
            var sb = new StringBuilder();
            
            sb.AppendLine("# UnityMCP Subsystem Reference");
            sb.AppendLine();
            sb.AppendLine("This document provides information about the subsystems available in UnityMCP.");
            sb.AppendLine();
            sb.AppendLine("## What are Subsystems?");
            sb.AppendLine();
            sb.AppendLine("Subsystems are specialized modules that extend the functionality of UnityMCP. Each subsystem focuses on a specific aspect of Unity, such as animation, lighting, or prefabs.");
            sb.AppendLine();
            sb.AppendLine("## Available Subsystems");
            sb.AppendLine();
            
            foreach (var subsystem in subsystems.OrderBy(s => s.Name))
            {
                sb.AppendLine($"### {subsystem.Name} Subsystem");
                sb.AppendLine();
                
                if (!string.IsNullOrEmpty(subsystem.Description))
                {
                    sb.AppendLine(subsystem.Description);
                    sb.AppendLine();
                }
                
                sb.AppendLine($"**Version:** {subsystem.Version}");
                sb.AppendLine();
                
                if (subsystem.Commands.Count > 0)
                {
                    sb.AppendLine("**Provided Commands:**");
                    sb.AppendLine();
                    
                    foreach (var command in subsystem.Commands.OrderBy(c => c))
                    {
                        sb.AppendLine($"- `{subsystem.Domain}.{command}`");
                    }
                    
                    sb.AppendLine();
                }
                
                if (!string.IsNullOrEmpty(subsystem.Usage))
                {
                    sb.AppendLine("**Usage:**");
                    sb.AppendLine();
                    sb.AppendLine(subsystem.Usage);
                    sb.AppendLine();
                }
            }
            
            // Write to file
            string filePath = Path.Combine(outputPath, "SubsystemReference.md");
            File.WriteAllText(filePath, sb.ToString());
        }
        
        private void GenerateArchitectureDocumentation()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("# UnityMCP Architecture");
            sb.AppendLine();
            sb.AppendLine("This document provides an overview of the UnityMCP architecture and how the different components interact.");
            sb.AppendLine();
            sb.AppendLine("## Overview");
            sb.AppendLine();
            sb.AppendLine("UnityMCP uses a modular architecture with these key components:");
            sb.AppendLine();
            sb.AppendLine("1. **UnityMCPBrain**: Central intelligence system that coordinates all subsystems");
            sb.AppendLine("2. **Command Handlers**: Process specific types of commands (objects, materials, lighting, etc.)");
            sb.AppendLine("3. **Subsystems**: Specialized modules for different aspects of Unity (animation, prefabs, etc.)");
            sb.AppendLine("4. **MCP Server**: Python server that implements the Model Context Protocol");
            sb.AppendLine("5. **Socket Communication**: Bidirectional communication between Claude and Unity");
            sb.AppendLine();
            sb.AppendLine("## Component Diagram");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine("┌─────────────┐     ┌─────────────┐     ┌─────────────┐");
            sb.AppendLine("│             │     │             │     │             │");
            sb.AppendLine("│  Claude AI  │◄────┤  MCP Server │◄────┤ Unity Brain │");
            sb.AppendLine("│             │     │             │     │             │");
            sb.AppendLine("└─────────────┘     └─────────────┘     └──────┬──────┘");
            sb.AppendLine("                                               │");
            sb.AppendLine("                                               │");
            sb.AppendLine("                                        ┌──────┴──────┐");
            sb.AppendLine("                                        │             │");
            sb.AppendLine("                                        │ Subsystems  │");
            sb.AppendLine("                                        │             │");
            sb.AppendLine("                                        └─────────────┘");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("## Communication Flow");
            sb.AppendLine();
            sb.AppendLine("1. Claude AI sends a command to the MCP Server");
            sb.AppendLine("2. The MCP Server forwards the command to the Unity Brain");
            sb.AppendLine("3. The Unity Brain routes the command to the appropriate Command Handler");
            sb.AppendLine("4. The Command Handler executes the command, potentially using Subsystems");
            sb.AppendLine("5. The result is returned back through the same path to Claude AI");
            sb.AppendLine();
            sb.AppendLine("## Key Classes");
            sb.AppendLine();
            sb.AppendLine("### UnityMCPBrain");
            sb.AppendLine();
            sb.AppendLine("The central coordinator for the entire system. It:");
            sb.AppendLine();
            sb.AppendLine("- Manages the socket server for communication");
            sb.AppendLine("- Discovers and initializes subsystems");
            sb.AppendLine("- Routes commands to the appropriate handlers");
            sb.AppendLine("- Maintains context and state between commands");
            sb.AppendLine();
            sb.AppendLine("### CommandHandler");
            sb.AppendLine();
            sb.AppendLine("Base class for all command handlers. Each handler:");
            sb.AppendLine();
            sb.AppendLine("- Processes commands for a specific domain");
            sb.AppendLine("- Uses reflection to discover command methods");
            sb.AppendLine("- Parses parameters and executes commands");
            sb.AppendLine("- Returns results in a standardized format");
            sb.AppendLine();
            sb.AppendLine("### IUnityMCPSubsystem");
            sb.AppendLine();
            sb.AppendLine("Interface for all subsystems. Each subsystem:");
            sb.AppendLine();
            sb.AppendLine("- Provides specialized functionality for a specific aspect of Unity");
            sb.AppendLine("- Can provide command handlers for its domain");
            sb.AppendLine("- Is automatically discovered and initialized by the brain");
            sb.AppendLine();
            sb.AppendLine("### UnityMCPContext");
            sb.AppendLine();
            sb.AppendLine("Maintains context and state between commands:");
            sb.AppendLine();
            sb.AppendLine("- Tracks focused and selected objects");
            sb.AppendLine("- Stores variables for use across commands");
            sb.AppendLine("- Provides session and project information");
            sb.AppendLine();
            sb.AppendLine("## Extending UnityMCP");
            sb.AppendLine();
            sb.AppendLine("### Adding a New Subsystem");
            sb.AppendLine();
            sb.AppendLine("1. Create a new class that implements `IUnityMCPSubsystem` and `ICommandProvider`");
            sb.AppendLine("2. Implement the required methods");
            sb.AppendLine("3. Create a command handler for your subsystem");
            sb.AppendLine("4. The subsystem will be automatically discovered and initialized");
            sb.AppendLine();
            sb.AppendLine("### Adding New Commands");
            sb.AppendLine();
            sb.AppendLine("1. Create a method in a command handler class");
            sb.AppendLine("2. Add the `[CommandMethod]` attribute to the method");
            sb.AppendLine("3. The method will be automatically discovered and available as a command");
            sb.AppendLine();
            sb.AppendLine("## Security Considerations");
            sb.AppendLine();
            sb.AppendLine("- The socket server accepts connections from any client on the specified port");
            sb.AppendLine("- Be cautious when exposing the server to networks");
            sb.AppendLine("- Consider adding authentication for production use");
            sb.AppendLine("- Always save your Unity project before using the MCP integration");
            
            // Write to file
            string filePath = Path.Combine(outputPath, "Architecture.md");
            File.WriteAllText(filePath, sb.ToString());
        }
        
        private void GenerateIndexFile()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("# UnityMCP Documentation");
            sb.AppendLine();
            sb.AppendLine("Welcome to the UnityMCP documentation. This documentation provides information about the UnityMCP system, which enables AI-assisted game development through the Model Context Protocol.");
            sb.AppendLine();
            sb.AppendLine("## Contents");
            sb.AppendLine();
            
            if (generateCommandDocs)
            {
                sb.AppendLine("- [Command Reference](CommandReference.md) - Reference for all available commands");
            }
            
            if (generateSubsystemDocs)
            {
                sb.AppendLine("- [Subsystem Reference](SubsystemReference.md) - Information about available subsystems");
            }
            
            if (generateArchitectureDocs)
            {
                sb.AppendLine("- [Architecture](Architecture.md) - Overview of the UnityMCP architecture");
            }
            
            sb.AppendLine();
            sb.AppendLine("## Getting Started");
            sb.AppendLine();
            sb.AppendLine("To get started with UnityMCP, see the [README.md](../README.md) file for installation and usage instructions.");
            
            // Write to file
            string filePath = Path.Combine(outputPath, "index.md");
            File.WriteAllText(filePath, sb.ToString());
        }
        
        private Dictionary<string, Type> FindAllCommandHandlers()
        {
            var result = new Dictionary<string, Type>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(CommandHandler).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        string name = type.Name;
                        if (name.EndsWith("CommandHandler"))
                        {
                            name = name.Substring(0, name.Length - "CommandHandler".Length);
                        }
                        
                        result[name] = type;
                    }
                }
            }
            
            return result;
        }
        
        private List<SubsystemInfo> FindAllSubsystems()
        {
            var result = new List<SubsystemInfo>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IUnityMCPSubsystem).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        var subsystemInfo = new SubsystemInfo
                        {
                            Type = type,
                            Name = GetSubsystemName(type),
                            Description = GetTypeDescription(type),
                            Version = "1.0.0", // Default version
                            Domain = GetDomainFromSubsystemName(GetSubsystemName(type)),
                            Commands = GetCommandsForSubsystem(type),
                            Usage = GetSubsystemUsage(type)
                        };
                        
                        // Try to get version from GetVersion method
                        try
                        {
                            var instance = Activator.CreateInstance(type) as IUnityMCPSubsystem;
                            if (instance != null)
                            {
                                subsystemInfo.Version = instance.GetVersion();
                            }
                        }
                        catch
                        {
                            // Ignore errors when creating instance
                        }
                        
                        result.Add(subsystemInfo);
                    }
                }
            }
            
            return result;
        }
        
        private List<CommandInfo> GetCommandsFromHandler(Type handlerType)
        {
            var result = new List<CommandInfo>();
            
            foreach (var method in handlerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attribute = method.GetCustomAttribute<CommandMethodAttribute>();
                if (attribute != null)
                {
                    string commandName = attribute.CommandName ?? method.Name;
                    
                    var commandInfo = new CommandInfo
                    {
                        Name = commandName,
                        Description = GetMethodDescription(method),
                        ReturnDescription = GetReturnDescription(method),
                        Parameters = GetParameterInfo(method),
                        Example = GetCommandExample(method)
                    };
                    
                    result.Add(commandInfo);
                }
            }
            
            return result;
        }
        
        private List<ParameterInfo> GetParameterInfo(MethodInfo method)
        {
            var result = new List<ParameterInfo>();
            
            foreach (var param in method.GetParameters())
            {
                result.Add(new ParameterInfo
                {
                    Name = param.Name,
                    Type = GetFriendlyTypeName(param.ParameterType),
                    Description = GetParameterDescription(method, param.Name),
                    HasDefaultValue = param.HasDefaultValue,
                    DefaultValue = param.HasDefaultValue ? param.DefaultValue : null
                });
            }
            
            return result;
        }
        
        private string GetParameterDescription(MethodInfo method, string paramName)
        {
            // Try to extract parameter description from XML comments
            // This is a simplified version - in a real implementation, you would parse XML comments
            
            // For now, return a generic description
            return $"The {paramName} parameter";
        }
        
        private string GetMethodDescription(MethodInfo method)
        {
            // Try to extract method description from XML comments
            // This is a simplified version - in a real implementation, you would parse XML comments
            
            // For now, return a generic description based on the method name
            string name = method.Name;
            return $"Executes the {name} command";
        }
        
        private string GetReturnDescription(MethodInfo method)
        {
            // Try to extract return description from XML comments
            // This is a simplified version - in a real implementation, you would parse XML comments
            
            // For now, return a generic description
            return "A JSON object containing the result of the command";
        }
        
        private string GetTypeDescription(Type type)
        {
            // Try to extract type description from XML comments
            // This is a simplified version - in a real implementation, you would parse XML comments
            
            // For now, return a generic description based on the type name
            string name = type.Name;
            if (name.EndsWith("Subsystem"))
            {
                name = name.Substring(0, name.Length - "Subsystem".Length);
            }
            
            return $"Provides functionality for working with {name} in Unity";
        }
        
        private string GetCommandExample(MethodInfo method)
        {
            // In a real implementation, you would generate an example based on the method parameters
            // For now, return a generic example
            
            string commandName = method.Name;
            string domain = GetDomainFromHandlerName(method.DeclaringType.Name);
            
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                return $"{{\n  \"type\": \"{domain}.{commandName}\",\n  \"parameters\": {{}}\n}}";
            }
            else
            {
                var paramExample = new StringBuilder();
                paramExample.AppendLine("{");
                
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    string value = GetExampleValue(param.ParameterType);
                    
                    paramExample.Append($"    \"{param.Name}\": {value}");
                    
                    if (i < parameters.Length - 1)
                    {
                        paramExample.AppendLine(",");
                    }
                    else
                    {
                        paramExample.AppendLine();
                    }
                }
                
                paramExample.Append("  }");
                
                return $"{{\n  \"type\": \"{domain}.{commandName}\",\n  \"parameters\": {paramExample}\n}}";
            }
        }
        
        private string GetExampleValue(Type type)
        {
            if (type == typeof(string))
            {
                return "\"example\"";
            }
            else if (type == typeof(int) || type == typeof(long))
            {
                return "42";
            }
            else if (type == typeof(float) || type == typeof(double))
            {
                return "3.14";
            }
            else if (type == typeof(bool))
            {
                return "true";
            }
            else if (type == typeof(Vector3) || type.Name == "Vector3?")
            {
                return "{ \"x\": 1.0, \"y\": 2.0, \"z\": 3.0 }";
            }
            else if (type == typeof(Color) || type.Name == "Color?")
            {
                return "{ \"r\": 1.0, \"g\": 0.0, \"b\": 0.0, \"a\": 1.0 }";
            }
            else if (type.IsEnum)
            {
                var values = Enum.GetValues(type);
                if (values.Length > 0)
                {
                    return $"\"{values.GetValue(0)}\"";
                }
                return "\"EnumValue\"";
            }
            else if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
            {
                Type elementType;
                if (type.IsArray)
                {
                    elementType = type.GetElementType();
                }
                else
                {
                    elementType = type.GetGenericArguments()[0];
                }
                
                return $"[{GetExampleValue(elementType)}]";
            }
            else
            {
                return "{}";
            }
        }
        
        private string GetDomainFromHandlerName(string handlerName)
        {
            if (handlerName.EndsWith("CommandHandler"))
            {
                handlerName = handlerName.Substring(0, handlerName.Length - "CommandHandler".Length);
            }
            
            return handlerName.ToLower();
        }
        
        private string GetSubsystemName(Type subsystemType)
        {
            string name = subsystemType.Name;
            if (name.EndsWith("Subsystem"))
            {
                name = name.Substring(0, name.Length - "Subsystem".Length);
            }
            
            return name;
        }
        
        private string GetDomainFromSubsystemName(string subsystemName)
        {
            return subsystemName.ToLower();
        }
        
        private List<string> GetCommandsForSubsystem(Type subsystemType)
        {
            var result = new List<string>();
            
            // Try to find command handler types provided by this subsystem
            foreach (var method in subsystemType.GetMethods())
            {
                if (method.Name == "GetCommandHandlers" && method.ReturnType == typeof(Dictionary<string, CommandHandler>))
                {
                    try
                    {
                        var instance = Activator.CreateInstance(subsystemType) as IUnityMCPSubsystem;
                        if (instance != null)
                        {
                            // Initialize with a mock brain
                            var mockBrain = new MockBrain();
                            instance.Initialize(mockBrain);
                            
                            // Get command handlers
                            var commandProvider = instance as ICommandProvider;
                            if (commandProvider != null)
                            {
                                var handlers = commandProvider.GetCommandHandlers();
                                foreach (var handler in handlers)
                                {
                                    var commands = GetCommandsFromHandler(handler.Value.GetType());
                                    foreach (var command in commands)
                                    {
                                        result.Add(command.Name);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors when creating instance
                    }
                    
                    break;
                }
            }
            
            return result;
        }
        
        private string GetSubsystemUsage(Type subsystemType)
        {
            // In a real implementation, you would extract usage information from XML comments
            // For now, return a generic usage description
            
            string name = GetSubsystemName(subsystemType);
            return $"Use the {name.ToLower()} commands to work with {name} in your Unity scene.";
        }
        
        private string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(string))
            {
                return "string";
            }
            else if (type == typeof(int))
            {
                return "int";
            }
            else if (type == typeof(float))
            {
                return "float";
            }
            else if (type == typeof(bool))
            {
                return "bool";
            }
            else if (type == typeof(Vector3))
            {
                return "Vector3";
            }
            else if (type == typeof(Color))
            {
                return "Color";
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return $"{GetFriendlyTypeName(type.GetGenericArguments()[0])}?";
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return $"List<{GetFriendlyTypeName(type.GetGenericArguments()[0])}>";
            }
            else if (type.IsArray)
            {
                return $"{GetFriendlyTypeName(type.GetElementType())}[]";
            }
            else
            {
                return type.Name;
            }
        }
        
        private string FormatDefaultValue(object value)
        {
            if (value == null)
            {
                return "null";
            }
            else if (value is string)
            {
                return $"\"{value}\"";
            }
            else if (value is bool)
            {
                return value.ToString().ToLower();
            }
            else
            {
                return value.ToString();
            }
        }
        
        // Mock brain for initializing subsystems
        private class MockBrain : UnityMCPBrain
        {
            public override void LogInfo(string message) { }
            public override void LogWarning(string message) { }
            public override void LogError(string message) { }
            public override void LogDebug(string message) { }
        }
    }
    
    // Data classes for documentation generation
    
    public class CommandInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();
        public string ReturnDescription { get; set; }
        public string Example { get; set; }
    }
    
    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool HasDefaultValue { get; set; }
        public object DefaultValue { get; set; }
    }
    
    public class SubsystemInfo
    {
        public Type Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Domain { get; set; }
        public List<string> Commands { get; set; } = new List<string>();
        public string Usage { get; set; }
    }
}