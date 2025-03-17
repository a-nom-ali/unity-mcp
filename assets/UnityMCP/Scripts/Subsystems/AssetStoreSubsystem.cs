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
        
        [CommandMethod]
        public Dictionary<string, object> SearchPolyHavenAssets(string query, string category = null, int limit = 10)
        {
            #if UNITY_EDITOR
            try
            {
                // Validate parameters
                if (string.IsNullOrEmpty(query))
                {
                    return new Dictionary<string, object>
                    {
                        { "error", "Search query is required" },
                        { "results", new List<Dictionary<string, object>>() }
                    };
                }
                
                // Normalize category
                if (!string.IsNullOrEmpty(category))
                {
                    category = category.ToLower();
                    if (category != "models" && category != "textures" && category != "hdris")
                    {
                        return new Dictionary<string, object>
                        {
                            { "error", $"Invalid category: {category}. Valid categories are: models, textures, hdris" },
                            { "results", new List<Dictionary<string, object>>() }
                        };
                    }
                }
                
                // In a real implementation, this would call the PolyHaven API
                // For now, we'll return mock data
                var results = new List<Dictionary<string, object>>();
                
                // Generate mock results based on the query and category
                string assetType = category ?? "models";
                for (int i = 1; i <= limit; i++)
                {
                    string assetId = $"ph_{assetType}_{i}";
                    string assetName = $"{query}_{assetType}_{i}";
                    
                    results.Add(new Dictionary<string, object>
                    {
                        { "id", assetId },
                        { "name", assetName },
                        { "category", assetType },
                        { "url", $"https://polyhaven.com/a/{assetId}" },
                        { "thumbnailUrl", $"https://polyhaven.com/thumbnails/{assetId}.png" },
                        { "downloadUrl", $"https://polyhaven.com/download/{assetId}" },
                        { "author", "PolyHaven" },
                        { "license", "CC0" }
                    });
                }
                
                return new Dictionary<string, object>
                {
                    { "query", query },
                    { "category", category },
                    { "count", results.Count },
                    { "results", results }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Failed to search PolyHaven assets: {e.Message}" },
                    { "results", new List<Dictionary<string, object>>() }
                };
            }
            #else
            return new Dictionary<string, object>
            {
                { "error", "PolyHaven asset search is only available in the Unity Editor" },
                { "results", new List<Dictionary<string, object>>() }
            };
            #endif
        }
        
        [CommandMethod]
        public Dictionary<string, object> DownloadPolyHavenAsset(string assetId, string resolution = "2k", bool importAfterDownload = true)
        {
            #if UNITY_EDITOR
            try
            {
                // Validate parameters
                if (string.IsNullOrEmpty(assetId))
                {
                    return new Dictionary<string, object>
                    {
                        { "error", "Asset ID is required" }
                    };
                }
                
                // Validate resolution
                if (resolution != "1k" && resolution != "2k" && resolution != "4k" && resolution != "8k")
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"Invalid resolution: {resolution}. Valid resolutions are: 1k, 2k, 4k, 8k" }
                    };
                }
                
                // In a real implementation, this would download the asset from PolyHaven
                // For now, we'll simulate the download
                
                // Determine the asset type from the ID
                string assetType = "model";
                if (assetId.Contains("textures"))
                {
                    assetType = "texture";
                }
                else if (assetId.Contains("hdris"))
                {
                    assetType = "hdri";
                }
                
                // Create a mock path for the downloaded asset
                string assetName = Path.GetFileName(assetId);
                string downloadPath = Path.Combine(Application.dataPath, "PolyHaven", assetType, assetName);
                string projectPath = $"Assets/PolyHaven/{assetType}/{assetName}";
                
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(downloadPath));
                
                // Simulate the download by creating an empty file
                if (!File.Exists(downloadPath))
                {
                    File.WriteAllText(downloadPath, "Mock PolyHaven asset content");
                }
                
                // Import the asset if requested
                if (importAfterDownload)
                {
                    UnityEditor.AssetDatabase.Refresh();
                }
                
                return new Dictionary<string, object>
                {
                    { "assetId", assetId },
                    { "assetType", assetType },
                    { "resolution", resolution },
                    { "downloadPath", downloadPath },
                    { "projectPath", projectPath },
                    { "imported", importAfterDownload },
                    { "success", true }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Failed to download PolyHaven asset: {e.Message}" }
                };
            }
            #else
            return new Dictionary<string, object>
            {
                { "error", "PolyHaven asset download is only available in the Unity Editor" }
            };
            #endif
        }
        
        [CommandMethod]
        public Dictionary<string, object> AssetStoreApi(string endpoint, string method = "GET", Dictionary<string, object> parameters = null, bool cacheResults = true)
        {
            #if UNITY_EDITOR
            try
            {
                // Validate parameters
                if (string.IsNullOrEmpty(endpoint))
                {
                    return new Dictionary<string, object>
                    {
                        { "error", "API endpoint is required" },
                        { "success", false }
                    };
                }
                
                // Validate method
                if (method != "GET" && method != "POST" && method != "PUT" && method != "DELETE")
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"Invalid method: {method}. Valid methods are: GET, POST, PUT, DELETE" },
                        { "success", false }
                    };
                }
                
                // Check cache first if enabled and it's a GET request
                string cacheKey = null;
                if (cacheResults && method == "GET")
                {
                    cacheKey = $"{endpoint}_{JsonUtility.ToJson(parameters ?? new Dictionary<string, object>())}";
                    if (_assetCache.ContainsKey(cacheKey))
                    {
                        var cachedResult = _assetCache[cacheKey] as Dictionary<string, object>;
                        if (cachedResult != null)
                        {
                            cachedResult["fromCache"] = true;
                            return cachedResult;
                        }
                    }
                }
                
                // In a real implementation, this would call the Unity Asset Store API
                // For now, we'll create a mock implementation that returns different responses
                // based on the endpoint and method
                
                Dictionary<string, object> result = new Dictionary<string, object>();
                
                // Process different endpoints
                switch (endpoint.ToLower())
                {
                    case "assets":
                        result = HandleAssetsEndpoint(method, parameters);
                        break;
                        
                    case "categories":
                        result = HandleCategoriesEndpoint(method, parameters);
                        break;
                        
                    case "publishers":
                        result = HandlePublishersEndpoint(method, parameters);
                        break;
                        
                    case "user":
                        result = HandleUserEndpoint(method, parameters);
                        break;
                        
                    default:
                        result = new Dictionary<string, object>
                        {
                            { "error", $"Unknown endpoint: {endpoint}" },
                            { "success", false }
                        };
                        break;
                }
                
                // Cache the result if needed
                if (cacheResults && method == "GET" && result.ContainsKey("success") && (bool)result["success"])
                {
                    _assetCache[cacheKey] = result;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Error accessing Asset Store API: {ex.Message}" },
                    { "stack", ex.StackTrace },
                    { "success", false }
                };
            }
            #else
            return new Dictionary<string, object>
            {
                { "error", "Asset Store API is only available in the Unity Editor" },
                { "success", false }
            };
            #endif
        }
        
        private Dictionary<string, object> HandleAssetsEndpoint(string method, Dictionary<string, object> parameters)
        {
            // Mock implementation for the assets endpoint
            if (method == "GET")
            {
                // Handle asset search or get asset details
                string assetId = parameters != null && parameters.ContainsKey("id") ? parameters["id"] as string : null;
                
                if (!string.IsNullOrEmpty(assetId))
                {
                    // Return details for a specific asset
                    return new Dictionary<string, object>
                    {
                        { "id", assetId },
                        { "name", $"Asset {assetId}" },
                        { "description", "This is a mock asset description." },
                        { "publisher", "Mock Publisher" },
                        { "category", "3D Models" },
                        { "price", "Free" },
                        { "rating", 4.5f },
                        { "downloads", 1000 },
                        { "lastUpdated", DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd") },
                        { "version", "1.0.0" },
                        { "size", "25MB" },
                        { "requirements", "Unity 2020.3 or higher" },
                        { "success", true }
                    };
                }
                else
                {
                    // Return a list of assets (search results)
                    string query = parameters != null && parameters.ContainsKey("query") ? parameters["query"] as string : "";
                    string category = parameters != null && parameters.ContainsKey("category") ? parameters["category"] as string : null;
                    int limit = parameters != null && parameters.ContainsKey("limit") ? Convert.ToInt32(parameters["limit"]) : 10;
                    
                    var results = new List<Dictionary<string, object>>();
                    for (int i = 1; i <= limit; i++)
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            { "id", $"asset_{i}" },
                            { "name", $"{query} Asset {i}" },
                            { "publisher", "Mock Publisher" },
                            { "category", category ?? "3D Models" },
                            { "price", i % 3 == 0 ? "$9.99" : "Free" },
                            { "rating", 3.5f + (i % 3) }
                        });
                    }
                    
                    return new Dictionary<string, object>
                    {
                        { "query", query },
                        { "category", category },
                        { "count", results.Count },
                        { "results", results },
                        { "success", true }
                    };
                }
            }
            else if (method == "POST")
            {
                // Mock implementation for purchasing an asset
                return new Dictionary<string, object>
                {
                    { "message", "Asset purchase initiated" },
                    { "transactionId", Guid.NewGuid().ToString() },
                    { "success", true }
                };
            }
            
            return new Dictionary<string, object>
            {
                { "error", $"Method {method} not supported for assets endpoint" },
                { "success", false }
            };
        }
        
        private Dictionary<string, object> HandleCategoriesEndpoint(string method, Dictionary<string, object> parameters)
        {
            // Mock implementation for the categories endpoint
            if (method == "GET")
            {
                var categories = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { { "id", "3d" }, { "name", "3D Models" }, { "count", 1250 } },
                    new Dictionary<string, object> { { "id", "textures" }, { "name", "Textures & Materials" }, { "count", 980 } },
                    new Dictionary<string, object> { { "id", "tools" }, { "name", "Tools" }, { "count", 750 } },
                    new Dictionary<string, object> { { "id", "audio" }, { "name", "Audio" }, { "count", 420 } },
                    new Dictionary<string, object> { { "id", "vfx" }, { "name", "Visual Effects" }, { "count", 380 } },
                    new Dictionary<string, object> { { "id", "templates" }, { "name", "Templates" }, { "count", 210 } }
                };
                
                return new Dictionary<string, object>
                {
                    { "categories", categories },
                    { "count", categories.Count },
                    { "success", true }
                };
            }
            
            return new Dictionary<string, object>
            {
                { "error", $"Method {method} not supported for categories endpoint" },
                { "success", false }
            };
        }
        
        private Dictionary<string, object> HandlePublishersEndpoint(string method, Dictionary<string, object> parameters)
        {
            // Mock implementation for the publishers endpoint
            if (method == "GET")
            {
                string publisherId = parameters != null && parameters.ContainsKey("id") ? parameters["id"] as string : null;
                
                if (!string.IsNullOrEmpty(publisherId))
                {
                    // Return details for a specific publisher
                    return new Dictionary<string, object>
                    {
                        { "id", publisherId },
                        { "name", $"Publisher {publisherId}" },
                        { "description", "This is a mock publisher description." },
                        { "website", "https://example.com" },
                        { "assetCount", 25 },
                        { "rating", 4.2f },
                        { "success", true }
                    };
                }
                else
                {
                    // Return a list of publishers
                    int limit = parameters != null && parameters.ContainsKey("limit") ? Convert.ToInt32(parameters["limit"]) : 10;
                    
                    var results = new List<Dictionary<string, object>>();
                    for (int i = 1; i <= limit; i++)
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            { "id", $"pub_{i}" },
                            { "name", $"Publisher {i}" },
                            { "assetCount", i * 5 },
                            { "rating", 3.0f + (i % 5) * 0.5f }
                        });
                    }
                    
                    return new Dictionary<string, object>
                    {
                        { "count", results.Count },
                        { "results", results },
                        { "success", true }
                    };
                }
            }
            
            return new Dictionary<string, object>
            {
                { "error", $"Method {method} not supported for publishers endpoint" },
                { "success", false }
            };
        }
        
        private Dictionary<string, object> HandleUserEndpoint(string method, Dictionary<string, object> parameters)
        {
            // Mock implementation for the user endpoint
            if (method == "GET")
            {
                // Return user profile and purchased assets
                var purchasedAssets = new List<Dictionary<string, object>>();
                for (int i = 1; i <= 5; i++)
                {
                    purchasedAssets.Add(new Dictionary<string, object>
                    {
                        { "id", $"asset_{i}" },
                        { "name", $"Purchased Asset {i}" },
                        { "purchaseDate", DateTime.Now.AddDays(-i * 30).ToString("yyyy-MM-dd") }
                    });
                }
                
                return new Dictionary<string, object>
                {
                    { "username", "MockUser" },
                    { "email", "mock@example.com" },
                    { "purchasedAssets", purchasedAssets },
                    { "wishlist", new List<string> { "asset_10", "asset_15", "asset_20" } },
                    { "success", true }
                };
            }
            
            return new Dictionary<string, object>
            {
                { "error", $"Method {method} not supported for user endpoint" },
                { "success", false }
            };
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