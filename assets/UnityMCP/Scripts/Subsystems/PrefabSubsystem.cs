using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP.Subsystems
{
    /// <summary>
    /// Subsystem for managing prefabs
    /// </summary>
    public class PrefabSubsystem : MonoBehaviour, IUnityMCPSubsystem, ICommandProvider
    {
        private UnityMCPBrain _brain;
        private bool _initialized = false;
        private PrefabCommandHandler _commandHandler;
        
        public void Initialize(UnityMCPBrain brain)
        {
            if (_initialized) return;
            
            _brain = brain;
            _commandHandler = new PrefabCommandHandler();
            
            _initialized = true;
            _brain.LogInfo("Prefab subsystem initialized");
        }
        
        public void Shutdown()
        {
            _initialized = false;
            _brain.LogInfo("Prefab subsystem shut down");
        }
        
        public string GetName()
        {
            return "Prefab";
        }
        
        public string GetVersion()
        {
            return "1.0.0";
        }
        
        public bool IsInitialized()
        {
            return _initialized;
        }
        
        public Dictionary<string, CommandHandler> GetCommandHandlers()
        {
            return new Dictionary<string, CommandHandler>
            {
                { "prefab", _commandHandler }
            };
        }
    }
    
    /// <summary>
    /// Command handler for prefab operations
    /// </summary>
    public class PrefabCommandHandler : CommandHandler
    {
        [CommandMethod]
        public Dictionary<string, object> GetPrefabList()
        {
            var prefabs = new List<Dictionary<string, object>>();
            
            #if UNITY_EDITOR
            // In the editor, we can find all prefabs in the project
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                prefabs.Add(new Dictionary<string, object>
                {
                    { "name", prefab.name },
                    { "path", path }
                });
            }
            #else
            // At runtime, we can only find prefabs that are loaded in Resources
            var loadedPrefabs = Resources.LoadAll<GameObject>("");
            foreach (var prefab in loadedPrefabs)
            {
                prefabs.Add(new Dictionary<string, object>
                {
                    { "name", prefab.name }
                });
            }
            #endif
            
            return new Dictionary<string, object>
            {
                { "count", prefabs.Count },
                { "prefabs", prefabs }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> InstantiatePrefab(
            string prefabPath, 
            string name = null, 
            Vector3? position = null, 
            Vector3? rotation = null, 
            Vector3? scale = null)
        {
            GameObject prefab = null;
            
            #if UNITY_EDITOR
            // In the editor, we can load from any path
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                // Try as a resource
                prefab = Resources.Load<GameObject>(prefabPath);
            }
            #else
            // At runtime, we can only load from Resources
            prefab = Resources.Load<GameObject>(prefabPath);
            #endif
            
            if (prefab == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Prefab '{prefabPath}' not found" }
                };
            }
            
            // Instantiate the prefab
            GameObject instance = GameObject.Instantiate(prefab);
            
            // Set name if provided
            if (!string.IsNullOrEmpty(name))
            {
                instance.name = name;
            }
            else
            {
                // Remove the "(Clone)" suffix
                instance.name = prefab.name;
            }
            
            // Set transform properties if provided
            if (position.HasValue)
            {
                instance.transform.position = position.Value;
            }
            
            if (rotation.HasValue)
            {
                instance.transform.eulerAngles = rotation.Value;
            }
            
            if (scale.HasValue)
            {
                instance.transform.localScale = scale.Value;
            }
            
            // Set as focused object in context
            _context.SetFocusedObject(instance);
            
            return new Dictionary<string, object>
            {
                { "prefabPath", prefabPath },
                { "instantiated", true },
                { "instance", new Dictionary<string, object>
                    {
                        { "name", instance.name },
                        { "id", instance.GetInstanceID() }
                    }
                }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreatePrefab(string objectName, string prefabPath = null)
        {
            #if UNITY_EDITOR
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' not found" }
                };
            }
            
            // Determine the prefab path
            if (string.IsNullOrEmpty(prefabPath))
            {
                prefabPath = $"Assets/Prefabs/{obj.name}.prefab";
            }
            
            // Ensure the directory exists
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // Create the prefab
            GameObject prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            
            return new Dictionary<string, object>
            {
                { "objectName", objectName },
                { "prefabPath", prefabPath },
                { "created", prefab != null }
            };
            #else
            return new Dictionary<string, object>
            {
                { "error", "Creating prefabs at runtime is not supported" }
            };
            #endif
        }
        
        [CommandMethod]
        public Dictionary<string, object> ApplyPrefabChanges(string objectName)
        {
            #if UNITY_EDITOR
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' not found" }
                };
            }
            
            // Check if the object is a prefab instance
            if (!UnityEditor.PrefabUtility.IsPartOfPrefabInstance(obj))
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' is not a prefab instance" }
                };
            }
            
            // Apply changes to the prefab
            GameObject prefabRoot = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            UnityEditor.PrefabUtility.ApplyPrefabInstance(prefabRoot, UnityEditor.InteractionMode.AutomatedAction);
            
            return new Dictionary<string, object>
            {
                { "objectName", objectName },
                { "applied", true }
            };
            #else
            return new Dictionary<string, object>
            {
                { "error", "Applying prefab changes at runtime is not supported" }
            };
            #endif
        }
        
        [CommandMethod]
        public Dictionary<string, object> RevertPrefabChanges(string objectName)
        {
            #if UNITY_EDITOR
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' not found" }
                };
            }
            
            // Check if the object is a prefab instance
            if (!UnityEditor.PrefabUtility.IsPartOfPrefabInstance(obj))
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' is not a prefab instance" }
                };
            }
            
            // Revert changes to the prefab
            GameObject prefabRoot = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            UnityEditor.PrefabUtility.RevertPrefabInstance(prefabRoot, UnityEditor.InteractionMode.AutomatedAction);
            
            return new Dictionary<string, object>
            {
                { "objectName", objectName },
                { "reverted", true }
            };
            #else
            return new Dictionary<string, object>
            {
                { "error", "Reverting prefab changes at runtime is not supported" }
            };
            #endif
        }
    }
} 