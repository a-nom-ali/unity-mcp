using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMCP.Editor
{
    public class DocGenerator : MonoBehaviour
    {
        private void Start()
        {
            // Example usage of the DocGenerator
            GenerateDocumentation();
        }

        private void GenerateDocumentation()
        {
            // Implement the logic to generate documentation based on the existing code
            // This is a placeholder and should be replaced with the actual implementation
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