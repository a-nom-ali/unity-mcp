using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace UnityMCP
{
    /// <summary>
    /// Handles core system commands
    /// </summary>
    public class CoreCommandHandler : CommandHandler
    {
        [CommandMethod]
        public Dictionary<string, object> GetSystemInfo()
        {
            return new Dictionary<string, object>
            {
                { "unityVersion", Application.unityVersion },
                { "platform", Application.platform.ToString() },
                { "productName", Application.productName },
                { "companyName", Application.companyName },
                { "systemLanguage", Application.systemLanguage.ToString() },
                { "targetFrameRate", Application.targetFrameRate },
                { "systemMemorySize", SystemInfo.systemMemorySize },
                { "processorType", SystemInfo.processorType },
                { "processorCount", SystemInfo.processorCount },
                { "graphicsDeviceName", SystemInfo.graphicsDeviceName },
                { "graphicsMemorySize", SystemInfo.graphicsMemorySize },
                { "mcpVersion", typeof(UnityMCPBrain).Assembly.GetName().Version.ToString() }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetContext()
        {
            return _context.GetContextSnapshot();
        }
        
        // public static UnityEngine.Object[] FindObjectsOfTypeByName(string aClassName)
        // {
        //     var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        //     for(int i = 0; i < assemblies.Length; i++)
        //     {
        //         var types = assemblies*.GetTypes();*
        //         for(int n = 0; n < types.Length; n++)
        //         {
        //             if (typeof(UnityEngine.Object).IsAssignableFrom(types[n]) && aClassName == types[n].Name)
        //                 return UnityEngine.Object.FindObjectsOfType(types[n]);
        //         }
        //     }
        //     return new UnityEngine.Object[0];
        // }

        public static UnityEngine.Object[] FindObjectsOfTypeByName(string aClassName)
        {
            var type = System.Type.GetType(aClassName);
            return Object.FindObjectsByType(type, FindObjectsSortMode.InstanceID);
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetSubsystems()
        {
            var brain = UnityMCPBrain.Instance;
            var result = new Dictionary<string, object>();
            
            // Get all subsystems
            var subsystems = new List<Dictionary<string, string>>();
            
            foreach (var subsystemType in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IUnityMCPSubsystem).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
            {
                
                var subsystem = brain.GetSubsystem(subsystemType) as IUnityMCPSubsystem;
                if (subsystem != null)
                {
                    subsystems.Add(new Dictionary<string, string>
                    {
                        { "name", subsystem.GetName() },
                        { "version", subsystem.GetVersion() },
                        { "status", subsystem.IsInitialized() ? "Initialized" : "Not Initialized" }
                    });
                }
            }
            
            result["subsystems"] = subsystems;
            return result;
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetCommandHandlers()
        {
            var brain = UnityMCPBrain.Instance;
            var result = new Dictionary<string, object>();
            
            // This would need to be implemented in the brain to expose registered handlers
            // For now, we'll return a placeholder
            result["handlers"] = new List<string> { "core", "scene", "object", "material" };
            
            return result;
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetVariable(string key, object value)
        {
            _context.SetVariable(key, value);
            
            return new Dictionary<string, object>
            {
                { "key", key },
                { "value", value },
                { "set", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetVariable(string key)
        {
            if (_context.HasVariable(key))
            {
                return new Dictionary<string, object>
                {
                    { "key", key },
                    { "value", _context.GetVariable<object>(key) },
                    { "exists", true }
                };
            }
            else
            {
                return new Dictionary<string, object>
                {
                    { "key", key },
                    { "exists", false }
                };
            }
        }
        
        [CommandMethod]
        public Dictionary<string, object> RemoveVariable(string key)
        {
            bool existed = _context.HasVariable(key);
            _context.RemoveVariable(key);
            
            return new Dictionary<string, object>
            {
                { "key", key },
                { "removed", existed }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> ExecuteCoroutine(string coroutineName, Dictionary<string, object> parameters)
        {
            // This would need a coroutine registry and execution system
            // For now, we'll return a placeholder
            return new Dictionary<string, object>
            {
                { "coroutineName", coroutineName },
                { "status", "not_implemented" },
                { "message", "Coroutine execution is not implemented yet" }
            };
        }
    }
} 