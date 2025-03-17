using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace UnityMCP
{
    /// <summary>
    /// The intelligent assistant that provides high-level reasoning and creative suggestions
    /// based on the current project context.
    /// </summary>
    public class UnityMCPAssistant : MonoBehaviour, IUnityMCPSubsystem, ICommandProvider
    {
        private UnityMCPBrain _brain;
        private bool _initialized = false;
        private AssistantCommandHandler _commandHandler;
        
        // History of suggestions and insights
        private List<AssistantInsight> _insights = new List<AssistantInsight>();
        
        // Current project analysis
        private ProjectAnalysis _currentAnalysis;
        
        // Last analysis timestamp
        private DateTime _lastAnalysisTime;
        
        public void Initialize(UnityMCPBrain brain)
        {
            if (_initialized) return;
            
            _brain = brain;
            _commandHandler = new AssistantCommandHandler(this);
            
            // Subscribe to brain events
            _brain.OnSystemEvent += OnBrainEvent;
            
            // Perform initial analysis
            AnalyzeProject();
            
            _initialized = true;
            _brain.LogInfo("UnityMCP Assistant initialized");
        }
        
        public void Shutdown()
        {
            if (!_initialized) return;
            
            // Unsubscribe from brain events
            _brain.OnSystemEvent -= OnBrainEvent;
            
            _initialized = false;
            _brain.LogInfo("UnityMCP Assistant shut down");
        }
        
        public string GetName()
        {
            return "Assistant";
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
                { "assistant", _commandHandler }
            };
        }
        
        private void OnBrainEvent(string eventName, object data)
        {
            // React to significant events
            switch (eventName)
            {
                case "command.executed":
                    // After a certain number of commands, re-analyze the project
                    if (_insights.Count % 10 == 0 || 
                        (DateTime.Now - _lastAnalysisTime).TotalMinutes > 15)
                    {
                        AnalyzeProject();
                    }
                    break;
                    
                case "scene.changed":
                    // Scene has changed significantly, re-analyze
                    AnalyzeProject();
                    break;
            }
        }
        
        public void AnalyzeProject()
        {
            _lastAnalysisTime = DateTime.Now;
            
            // Create a new analysis
            _currentAnalysis = new ProjectAnalysis();
            
            // Analyze scene composition
            AnalyzeSceneComposition();
            
            // Analyze visual style
            AnalyzeVisualStyle();
            
            // Analyze performance
            AnalyzePerformance();
            
            // Generate insights based on analysis
            GenerateInsights();
            
            _brain.LogInfo("Project analysis completed");
        }
        
        private void AnalyzeSceneComposition()
        {
            // Get all objects in the scene
            var objects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.InstanceID)
                .Where(obj => obj.transform.parent == null) // Root objects only
                .ToList();
            
            _currentAnalysis.TotalObjectCount = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.InstanceID).Length;
            _currentAnalysis.RootObjectCount = objects.Count;
            
            // Analyze object types
            int meshCount = 0;
            int lightCount = 0;
            int cameraCount = 0;
            int uiCount = 0;
            
            foreach (var obj in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.InstanceID))
            {
                if (obj.GetComponent<MeshRenderer>() != null) meshCount++;
                if (obj.GetComponent<Light>() != null) lightCount++;
                if (obj.GetComponent<Camera>() != null) cameraCount++;
                if (obj.GetComponent<RectTransform>() != null) uiCount++;
            }
            
            _currentAnalysis.MeshCount = meshCount;
            _currentAnalysis.LightCount = lightCount;
            _currentAnalysis.CameraCount = cameraCount;
            _currentAnalysis.UIElementCount = uiCount;
            
            // Analyze scene balance
            AnalyzeSceneBalance(objects);
        }
        
        private void AnalyzeSceneBalance(List<GameObject> rootObjects)
        {
            if (rootObjects.Count == 0) return;
            
            // Calculate scene bounds
            Bounds sceneBounds = new Bounds();
            bool boundsInitialized = false;
            
            foreach (var obj in rootObjects)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (!boundsInitialized)
                    {
                        sceneBounds = renderer.bounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        sceneBounds.Encapsulate(renderer.bounds);
                    }
                }
            }
            
            if (boundsInitialized)
            {
                _currentAnalysis.SceneBounds = sceneBounds;
                
                // Analyze object distribution
                Vector3 center = sceneBounds.center;
                float radius = sceneBounds.extents.magnitude;
                
                // Check if objects are clustered or spread out
                float avgDistanceFromCenter = 0;
                int count = 0;
                
                foreach (var obj in rootObjects)
                {
                    if (obj.GetComponent<Renderer>() != null)
                    {
                        avgDistanceFromCenter += Vector3.Distance(obj.transform.position, center);
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    avgDistanceFromCenter /= count;
                    _currentAnalysis.AverageDistanceFromCenter = avgDistanceFromCenter;
                    _currentAnalysis.SceneRadius = radius;
                    
                    // Calculate distribution ratio (0 = all at center, 1 = evenly distributed)
                    _currentAnalysis.DistributionRatio = Mathf.Clamp01(avgDistanceFromCenter / radius);
                }
            }
        }
        
        private void AnalyzeVisualStyle()
        {
            // Analyze materials
            var materials = new HashSet<Material>();
            var colorPalette = new List<Color>();
            
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.InstanceID))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null && !materials.Contains(material))
                    {
                        materials.Add(material);
                        
                        if (material.HasProperty("_Color"))
                        {
                            colorPalette.Add(material.color);
                        }
                    }
                }
            }
            
            _currentAnalysis.MaterialCount = materials.Count;
            _currentAnalysis.ColorPalette = colorPalette;
            
            // Analyze lighting
            AnalyzeLighting();
        }
        
        private void AnalyzeLighting()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID);
            
            if (lights.Length > 0)
            {
                // Calculate average light color
                Color avgColor = Color.black;
                float totalIntensity = 0;
                
                foreach (var light in lights)
                {
                    avgColor += light.color * light.intensity;
                    totalIntensity += light.intensity;
                }
                
                if (totalIntensity > 0)
                {
                    avgColor /= totalIntensity;
                    _currentAnalysis.AverageLightColor = avgColor;
                    _currentAnalysis.TotalLightIntensity = totalIntensity;
                }
                
                // Determine lighting style
                int directionalCount = lights.Count(l => l.type == LightType.Directional);
                int pointCount = lights.Count(l => l.type == LightType.Point);
                int spotCount = lights.Count(l => l.type == LightType.Spot);
                int areaCount = lights.Count(l => l.type == LightType.Rectangle);
                
                _currentAnalysis.DirectionalLightCount = directionalCount;
                _currentAnalysis.PointLightCount = pointCount;
                _currentAnalysis.SpotLightCount = spotCount;
                _currentAnalysis.AreaLightCount = areaCount;
                
                // Determine lighting style
                if (directionalCount == 1 && pointCount == 0 && spotCount == 0)
                {
                    _currentAnalysis.LightingStyle = "Simple Directional";
                }
                else if (directionalCount >= 1 && (pointCount > 0 || spotCount > 0))
                {
                    _currentAnalysis.LightingStyle = "Mixed";
                }
                else if (directionalCount == 0 && pointCount > 0)
                {
                    _currentAnalysis.LightingStyle = "Point Light Based";
                }
                else if (spotCount > 0 && spotCount > pointCount)
                {
                    _currentAnalysis.LightingStyle = "Spot Light Focused";
                }
                else if (areaCount > 0 && areaCount >= (directionalCount + pointCount + spotCount))
                {
                    _currentAnalysis.LightingStyle = "Area Light Based";
                }
                else
                {
                    _currentAnalysis.LightingStyle = "Custom";
                }
            }
            else
            {
                _currentAnalysis.LightingStyle = "No Lights";
            }
        }
        
        private void AnalyzePerformance()
        {
            // Count total vertices and triangles
            int totalVertices = 0;
            int totalTriangles = 0;
            
            foreach (var mesh in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.InstanceID))
            {
                if (mesh.sharedMesh != null)
                {
                    totalVertices += mesh.sharedMesh.vertexCount;
                    totalTriangles += mesh.sharedMesh.triangles.Length / 3;
                }
            }
            
            _currentAnalysis.TotalVertices = totalVertices;
            _currentAnalysis.TotalTriangles = totalTriangles;
            
            // Analyze draw calls (simplified estimate)
            int estimatedDrawCalls = 0;
            var uniqueMaterialCombinations = new HashSet<string>();
            
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.InstanceID))
            {
                if (renderer.isVisible)
                {
                    string key = renderer.gameObject.layer.ToString();
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            key += "_" + material.GetInstanceID();
                        }
                    }
                    
                    uniqueMaterialCombinations.Add(key);
                }
            }
            
            estimatedDrawCalls = uniqueMaterialCombinations.Count;
            _currentAnalysis.EstimatedDrawCalls = estimatedDrawCalls;
            
            // Performance rating
            if (totalVertices < 100000 && estimatedDrawCalls < 100)
            {
                _currentAnalysis.PerformanceRating = "Excellent";
            }
            else if (totalVertices < 500000 && estimatedDrawCalls < 500)
            {
                _currentAnalysis.PerformanceRating = "Good";
            }
            else if (totalVertices < 1000000 && estimatedDrawCalls < 1000)
            {
                _currentAnalysis.PerformanceRating = "Average";
            }
            else if (totalVertices < 2000000 && estimatedDrawCalls < 2000)
            {
                _currentAnalysis.PerformanceRating = "Below Average";
            }
            else
            {
                _currentAnalysis.PerformanceRating = "Poor";
            }
        }
        
        private void GenerateInsights()
        {
            var insights = new List<AssistantInsight>();
            
            // Scene composition insights
            if (_currentAnalysis.RootObjectCount > 0)
            {
                // Check for scene balance
                if (_currentAnalysis.DistributionRatio < 0.3f)
                {
                    insights.Add(new AssistantInsight
                    {
                        Type = InsightType.Composition,
                        Title = "Objects are clustered near the center",
                        Description = "Consider spreading objects more evenly throughout the scene for better visual balance.",
                        Suggestion = "Try moving some objects further from the center to create a more balanced composition."
                    });
                }
                
                // Check for object hierarchy
                if (_currentAnalysis.TotalObjectCount > 20 && _currentAnalysis.RootObjectCount > 10)
                {
                    insights.Add(new AssistantInsight
                    {
                        Type = InsightType.Organization,
                        Title = "Many root objects detected",
                        Description = "Your scene has many objects at the root level, which can make organization difficult.",
                        Suggestion = "Consider grouping related objects under empty parent objects for better organization."
                    });
                }
            }
            
            // Lighting insights
            if (_currentAnalysis.LightCount > 0)
            {
                // Check for lighting balance
                if (_currentAnalysis.PointLightCount > 10)
                {
                    insights.Add(new AssistantInsight
                    {
                        Type = InsightType.Lighting,
                        Title = "Many point lights detected",
                        Description = "Using many point lights can impact performance and create uneven lighting.",
                        Suggestion = "Consider using fewer, strategically placed lights or baking lighting for better performance."
                    });
                }
                
                if (_currentAnalysis.DirectionalLightCount == 0)
                {
                    insights.Add(new AssistantInsight
                    {
                        Type = InsightType.Lighting,
                        Title = "No directional light found",
                        Description = "Most scenes benefit from a main directional light to establish primary shadows and overall illumination.",
                        Suggestion = "Add a directional light to serve as your main light source (like the sun)."
                    });
                }
            }
            else
            {
                insights.Add(new AssistantInsight
                {
                    Type = InsightType.Lighting,
                    Title = "No lights in scene",
                    Description = "Your scene doesn't have any lights, which will result in flat, uninteresting visuals.",
                    Suggestion = "Add at least one directional light as a main light source, and consider accent lights for visual interest."
                });
            }
            
            // Camera insights
            if (_currentAnalysis.CameraCount == 0)
            {
                insights.Add(new AssistantInsight
                {
                    Type = InsightType.Camera,
                    Title = "No camera in scene",
                    Description = "Your scene doesn't have a camera, which is needed to view the scene at runtime.",
                    Suggestion = "Add a main camera to your scene and position it to frame your content effectively."
                });
            }
            else if (_currentAnalysis.CameraCount > 3)
            {
                insights.Add(new AssistantInsight
                {
                    Type = InsightType.Camera,
                    Title = "Multiple cameras active",
                    Description = "Having multiple active cameras can be confusing and may impact performance.",
                    Suggestion = "Ensure only the cameras you need are active, and consider using camera switching rather than multiple active cameras."
                });
            }
            
            // Performance insights
            if (_currentAnalysis.PerformanceRating == "Below Average" || _currentAnalysis.PerformanceRating == "Poor")
            {
                insights.Add(new AssistantInsight
                {
                    Type = InsightType.Performance,
                    Title = "Performance concerns detected",
                    Description = $"Your scene has {_currentAnalysis.TotalVertices:N0} vertices and approximately {_currentAnalysis.EstimatedDrawCalls} draw calls, which may cause performance issues.",
                    Suggestion = "Consider optimizing by reducing polygon count, combining meshes, or using LOD (Level of Detail) for distant objects."
                });
            }
            
            // Material insights
            if (_currentAnalysis.MaterialCount > _currentAnalysis.MeshCount * 0.8f)
            {
                insights.Add(new AssistantInsight
                {
                    Type = InsightType.Materials,
                    Title = "High material count",
                    Description = "Your scene uses many unique materials, which can increase draw calls and impact performance.",
                    Suggestion = "Try to reuse materials where possible and consider creating a material atlas for similar objects."
                });
            }
            
            // Add insights to history
            foreach (var insight in insights)
            {
                insight.Timestamp = DateTime.Now;
                _insights.Add(insight);
            }
        }
        
        public List<AssistantInsight> GetRecentInsights(int count = 5)
        {
            return _insights.OrderByDescending(i => i.Timestamp).Take(count).ToList();
        }
        
        public ProjectAnalysis GetCurrentAnalysis()
        {
            return _currentAnalysis;
        }
        
        public string GenerateCreativeSuggestion()
        {
            // Based on the current analysis, generate a creative suggestion
            StringBuilder suggestion = new StringBuilder();
            
            // Determine what the scene might need based on analysis
            if (_currentAnalysis.TotalObjectCount < 5)
            {
                suggestion.AppendLine("Your scene looks quite empty. Consider adding:");
                suggestion.AppendLine("- A central focal point object");
                suggestion.AppendLine("- Supporting elements to create context");
                suggestion.AppendLine("- Background elements to add depth");
            }
            else if (_currentAnalysis.LightingStyle == "Simple Directional" || _currentAnalysis.LightingStyle == "No Lights")
            {
                suggestion.AppendLine("Your lighting could use more depth. Try adding:");
                suggestion.AppendLine("- Rim lighting to highlight object edges");
                suggestion.AppendLine("- Fill lights to soften harsh shadows");
                suggestion.AppendLine("- Accent lights to highlight important features");
            }
            else if (_currentAnalysis.MaterialCount < 3)
            {
                suggestion.AppendLine("Your materials look a bit uniform. Consider:");
                suggestion.AppendLine("- Adding material variation for visual interest");
                suggestion.AppendLine("- Using PBR materials with metallic/smoothness variation");
                suggestion.AppendLine("- Adding subtle textures to break up flat surfaces");
            }
            else
            {
                // General creative suggestions
                string[] suggestions = new string[]
                {
                    "Try adding atmospheric effects like fog or particles to add depth and mood.",
                    "Consider the rule of thirds when positioning key elements in your scene.",
                    "Add subtle animation to static elements to bring your scene to life.",
                    "Use color theory to create a more cohesive visual style - try a complementary or analogous color scheme.",
                    "Add environmental storytelling elements that hint at the history or purpose of the space.",
                    "Consider the flow of the viewer's eye through the scene - create a visual path with your composition.",
                    "Add small details and imperfections to make the scene feel more realistic and lived-in."
                };
                
                // Pick a random suggestion
                suggestion.Append(suggestions[UnityEngine.Random.Range(0, suggestions.Length)]);
            }
            
            return suggestion.ToString();
        }
    }
    
    public class AssistantCommandHandler : CommandHandler
    {
        private UnityMCPAssistant _assistant;
        
        public AssistantCommandHandler(UnityMCPAssistant assistant)
        {
            _assistant = assistant;
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetInsights(int count = 5)
        {
            var insights = _assistant.GetRecentInsights(count);
            
            var result = new Dictionary<string, object>
            {
                { "count", insights.Count },
                { "insights", insights.Select(i => new Dictionary<string, object>
                    {
                        { "type", i.Type.ToString() },
                        { "title", i.Title },
                        { "description", i.Description },
                        { "suggestion", i.Suggestion },
                        { "timestamp", i.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") }
                    }).ToList()
                }
            };
            
            return result;
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetAnalysis()
        {
            var analysis = _assistant.GetCurrentAnalysis();
            
            return new Dictionary<string, object>
            {
                { "objectCounts", new Dictionary<string, object>
                    {
                        { "total", analysis.TotalObjectCount },
                        { "root", analysis.RootObjectCount },
                        { "meshes", analysis.MeshCount },
                        { "lights", analysis.LightCount },
                        { "cameras", analysis.CameraCount },
                        { "ui", analysis.UIElementCount }
                    }
                },
                { "lighting", new Dictionary<string, object>
                    {
                        { "style", analysis.LightingStyle },
                        { "directionalLights", analysis.DirectionalLightCount },
                        { "pointLights", analysis.PointLightCount },
                        { "spotLights", analysis.SpotLightCount },
                        { "areaLights", analysis.AreaLightCount },
                        { "totalIntensity", analysis.TotalLightIntensity }
                    }
                },
                { "performance", new Dictionary<string, object>
                    {
                        { "rating", analysis.PerformanceRating },
                        { "vertices", analysis.TotalVertices },
                        { "triangles", analysis.TotalTriangles },
                        { "estimatedDrawCalls", analysis.EstimatedDrawCalls }
                    }
                },
                { "materials", new Dictionary<string, object>
                    {
                        { "count", analysis.MaterialCount }
                    }
                }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetSuggestions()
        {
            return GetCreativeSuggestion();
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetCreativeSuggestion()
        {
            string suggestion = _assistant.GenerateCreativeSuggestion();
            
            return new Dictionary<string, object>
            {
                { "suggestion", suggestion }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> AnalyzeProject()
        {
            _assistant.AnalyzeProject();
            
            return new Dictionary<string, object>
            {
                { "analyzed", true },
                { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            };
        }
    }
    
    public class ProjectAnalysis
    {
        // Object counts
        public int TotalObjectCount { get; set; }
        public int RootObjectCount { get; set; }
        public int MeshCount { get; set; }
        public int LightCount { get; set; }
        public int CameraCount { get; set; }
        public int UIElementCount { get; set; }
        
        // Scene composition
        public Bounds SceneBounds { get; set; }
        public float AverageDistanceFromCenter { get; set; }
        public float SceneRadius { get; set; }
        public float DistributionRatio { get; set; }
        
        // Visual style
        public int MaterialCount { get; set; }
        public List<Color> ColorPalette { get; set; } = new List<Color>();
        
        // Lighting
        public string LightingStyle { get; set; }
        public Color AverageLightColor { get; set; }
        public float TotalLightIntensity { get; set; }
        public int DirectionalLightCount { get; set; }
        public int PointLightCount { get; set; }
        public int SpotLightCount { get; set; }
        public int AreaLightCount { get; set; }
        
        // Performance
        public int TotalVertices { get; set; }
        public int TotalTriangles { get; set; }
        public int EstimatedDrawCalls { get; set; }
        public string PerformanceRating { get; set; }
    }
    
    public class AssistantInsight
    {
        public InsightType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Suggestion { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public enum InsightType
    {
        Composition,
        Lighting,
        Materials,
        Performance,
        Camera,
        Animation,
        Organization
    }
} 