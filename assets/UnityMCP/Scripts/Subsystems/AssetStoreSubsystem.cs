using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.Networking;

namespace UnityMCP.Subsystems
{
    /// <summary>
    /// Subsystem for integrating with the Unity Asset Store and other asset sources
    /// </summary>
    public class AssetStoreSubsystem : MonoBehaviour, IUnityMCPSubsystem, ICommandProvider
    {
        private UnityMCPBrain _brain;
        private bool _initialized = false;
        private AssetStoreCommandHandler _commandHandler;
        
        public void Initialize(UnityMCPBrain brain)
        {
            if (_initialized) return;
            
            _brain = brain;
            _commandHandler = new AssetStoreCommandHandler();
            
            _initialized = true;
            _brain.LogInfo("Asset Store subsystem initialized");
        }
        
        public void Shutdown()
        {
            _initialized = false;
            _brain.LogInfo("Asset Store subsystem shut down");
        }
        
        public string GetName()
        {
            return "AssetStore";
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
                { "asset", _commandHandler }
            };
        }
    }
    
    /// <summary>
    /// Command handler for asset store operations
    /// </summary>
    public class AssetStoreCommandHandler : CommandHandler
    {
        // Cache for downloaded asset metadata
        private Dictionary<string, object> _assetCache = new Dictionary<string, object>();
        
        [CommandMethod]
        public Dictionary<string, object> SearchAssets(string query, string category = null, int limit = 10)
        {
            #if UNITY_EDITOR
            // This is a simplified implementation - in a real implementation, you would
            // use the Asset Store API or a third-party service to search for assets
            
            // For now, we'll return some mock data
            var results = new List<Dictionary<string, object>>();
            
            // Add some mock results based on the query
            for (int i = 1; i <= limit; i++)
            {
                results.Add(new Dictionary<string, object>
                {
                    { "id", $"asset_{i}" },
                    { "name", $"{query} Asset {i}" },
                    { "publisher", "Mock Publisher" },
                    { "category", category ?? "3D Models" },
                    { "price", "Free" },
                    { "rating", 4.5f },
                    { "url", $"https://assetstore.unity.com/packages/mock/{i}" }
                });
            }
            
            return new Dictionary<string, object>
            {
                { "query", query },
                { "category", category },
                { "count", results.Count },
                { "results", results }
            };
            #else
            return new Dictionary<string, object>
            {
                { "error", "Asset Store integration is only available in the Unity Editor" }
            };
            #endif
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetAssetCategories()
        {
            // Return a list of common asset categories
            var categories = new List<string>
            {
                "3D Models",
                "Animations",
                "Audio",
                "Characters",
                "Environments",
                "Materials",
                "Particles",
                "Prefabs",
                "Shaders",
                "Textures",
                "UI",
                "VFX",
                "Tools",
                "Scripts"
            };
            
            return new Dictionary<string, object>
            {
                { "categories", categories }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> ImportLocalAsset(string path, string assetType = null)
        {
            #if UNITY_EDITOR
            try
            {
                // Check if the file exists
                if (!File.Exists(path))
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"File not found: {path}" }
                    };
                }
                
                // Determine the asset type if not specified
                if (string.IsNullOrEmpty(assetType))
                {
                    string extension = Path.GetExtension(path).ToLower();
                    switch (extension)
                    {
                        case ".fbx":
                        case ".obj":
                        case ".blend":
                            assetType = "Model";
                            break;
                        case ".png":
                        case ".jpg":
                        case ".jpeg":
                        case ".tga":
                            assetType = "Texture";
                            break;
                        case ".wav":
                        case ".mp3":
                        case ".ogg":
                            assetType = "Audio";
                            break;
                        default:
                            assetType = "Unknown";
                            break;
                    }
                }
                
                // Copy the file to the project if it's not already there
                string projectPath = Path.Combine("Assets", "ImportedAssets", Path.GetFileName(path));
                string fullProjectPath = Path.Combine(Application.dataPath, "..", projectPath);
                
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullProjectPath));
                
                // Copy the file
                File.Copy(path, fullProjectPath, true);
                
                // Refresh the asset database
                UnityEditor.AssetDatabase.Refresh();
                
                // Get the imported asset
                UnityEngine.Object asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(projectPath);
                
                return new Dictionary<string, object>
                {
                    { "path", projectPath },
                    { "type", assetType },
                    { "imported", asset != null },
                    { "name", asset != null ? asset.name : Path.GetFileNameWithoutExtension(path) }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Failed to import asset: {e.Message}" }
                };
            }
            #else
            return new Dictionary<string, object>
            {
                { "error", "Asset importing is only available in the Unity Editor" }
            };
            #endif
        }
        
        [CommandMethod]
        public Dictionary<string, object> InstantiateAsset(string assetPath, string name = null, Vector3? position = null, Vector3? rotation = null, Vector3? scale = null)
        {
            try
            {
                // Load the asset
                GameObject prefab = Resources.Load<GameObject>(assetPath);
                
                #if UNITY_EDITOR
                if (prefab == null)
                {
                    // Try loading from the asset database
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                }
                #endif
                
                if (prefab == null)
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"Asset not found: {assetPath}" }
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
                    { "assetPath", assetPath },
                    { "instantiated", true },
                    { "instance", new Dictionary<string, object>
                        {
                            { "name", instance.name },
                            { "id", instance.GetInstanceID() }
                        }
                    }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Failed to instantiate asset: {e.Message}" }
                };
            }
        }
        
        [CommandMethod]
        public Dictionary<string, object> DownloadTexture(string url, string saveName = null)
        {
            #if UNITY_EDITOR
            // This would normally be implemented as a coroutine, but for simplicity
            // we'll use a synchronous approach in this example
            try
            {
                // Generate a save name if not provided
                if (string.IsNullOrEmpty(saveName))
                {
                    saveName = Path.GetFileNameWithoutExtension(url);
                }
                
                // Determine the save path
                string savePath = Path.Combine("Assets", "DownloadedAssets", "Textures", $"{saveName}.png");
                string fullSavePath = Path.Combine(Application.dataPath, "..", savePath);
                
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullSavePath));
                
                // Download the texture
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, fullSavePath);
                }
                
                // Refresh the asset database
                UnityEditor.AssetDatabase.Refresh();
                
                return new Dictionary<string, object>
                {
                    { "url", url },
                    { "savePath", savePath },
                    { "downloaded", true }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Failed to download texture: {e.Message}" }
                };
            }
            #else
            return new Dictionary<string, object>
            {
                { "error", "Asset downloading is only available in the Unity Editor" }
            };
            #endif
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetProjectAssets(string filter = null, int limit = 100)
        {
            #if UNITY_EDITOR
            try
            {
                var assets = new List<Dictionary<string, object>>();
                
                // Build the filter string
                string searchFilter = "t:Object";
                if (!string.IsNullOrEmpty(filter))
                {
                    searchFilter = filter;
                }
                
                // Find assets
                string[] guids = UnityEditor.AssetDatabase.FindAssets(searchFilter);
                
                // Limit the number of results
                int count = Math.Min(guids.Length, limit);
                
                for (int i = 0; i < count; i++)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                    UnityEngine.Object asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    
                    if (asset != null)
                    {
                        assets.Add(new Dictionary<string, object>
                        {
                            { "name", asset.name },
                            { "path", path },
                            { "type", asset.GetType().Name }
                        });
                    }
                }
                
                return new Dictionary<string, object>
                {
                    { "filter", filter },
                    { "count", assets.Count },
                    { "totalCount", guids.Length },
                    { "assets", assets }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Failed to get project assets: {e.Message}" }
                };
            }
            #else
            return new Dictionary<string, object>
            {
                { "error", "Asset database queries are only available in the Unity Editor" }
            };
            #endif
        }
        
        // This would be a WebClient in a real implementation
        private class WebClient : IDisposable
        {
            public void DownloadFile(string url, string fileName)
            {
                // Mock implementation - in a real implementation, this would download the file
                File.WriteAllText(fileName, "Mock texture data");
            }
            
            public void Dispose()
            {
                // Cleanup
            }
        }
    }
} 