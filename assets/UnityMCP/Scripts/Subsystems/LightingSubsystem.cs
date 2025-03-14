using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityMCP.Subsystems
{
    /// <summary>
    /// Subsystem for managing lighting
    /// </summary>
    public class LightingSubsystem : MonoBehaviour, IUnityMCPSubsystem, ICommandProvider
    {
        private UnityMCPBrain _brain;
        private bool _initialized = false;
        private LightingCommandHandler _commandHandler;
        
        public void Initialize(UnityMCPBrain brain)
        {
            if (_initialized) return;
            
            _brain = brain;
            _commandHandler = new LightingCommandHandler();
            
            _initialized = true;
            _brain.LogInfo("Lighting subsystem initialized");
        }
        
        public void Shutdown()
        {
            _initialized = false;
            _brain.LogInfo("Lighting subsystem shut down");
        }
        
        public string GetName()
        {
            return "Lighting";
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
                { "lighting", _commandHandler }
            };
        }
    }
    
    /// <summary>
    /// Command handler for lighting operations
    /// </summary>
    public class LightingCommandHandler : CommandHandler
    {
        [CommandMethod]
        public Dictionary<string, object> GetLightingInfo()
        {
            var result = new Dictionary<string, object>();
            
            // Get ambient light settings
            result["ambientMode"] = RenderSettings.ambientMode.ToString();
            
            if (RenderSettings.ambientMode == AmbientMode.Skybox)
            {
                result["ambientIntensity"] = RenderSettings.ambientIntensity;
            }
            else if (RenderSettings.ambientMode == AmbientMode.Trilight)
            {
                result["ambientSkyColor"] = ColorToDict(RenderSettings.ambientSkyColor);
                result["ambientEquatorColor"] = ColorToDict(RenderSettings.ambientEquatorColor);
                result["ambientGroundColor"] = ColorToDict(RenderSettings.ambientGroundColor);
            }
            else
            {
                result["ambientColor"] = ColorToDict(RenderSettings.ambientLight);
            }
            
            // Get fog settings
            result["fog"] = RenderSettings.fog;
            if (RenderSettings.fog)
            {
                result["fogColor"] = ColorToDict(RenderSettings.fogColor);
                result["fogMode"] = RenderSettings.fogMode.ToString();
                result["fogDensity"] = RenderSettings.fogDensity;
                result["fogStartDistance"] = RenderSettings.fogStartDistance;
                result["fogEndDistance"] = RenderSettings.fogEndDistance;
            }
            
            // Get skybox
            if (RenderSettings.skybox != null)
            {
                result["skybox"] = RenderSettings.skybox.name;
            }
            
            // Get all lights in the scene
            var lights = new List<Dictionary<string, object>>();
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID))
            {
                lights.Add(GetLightInfo(light));
            }
            result["lights"] = lights;
            
            return result;
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreateLight(
            string type = "Point", 
            string name = null, 
            Vector3? position = null, 
            Vector3? rotation = null, 
            Color? color = null, 
            float intensity = 1.0f, 
            float range = 10.0f, 
            float spotAngle = 30.0f, 
            LightShadows shadows = LightShadows.Soft)
        {
            // Parse light type
            LightType lightType;
            if (!Enum.TryParse(type, true, out lightType))
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Invalid light type: {type}" }
                };
            }
            
            // Create a new GameObject for the light
            GameObject lightObj = new GameObject(name ?? $"{type}Light");
            
            // Add Light component
            Light light = lightObj.AddComponent<Light>();
            light.type = lightType;
            
            // Set properties
            if (color.HasValue)
            {
                light.color = color.Value;
            }
            
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = spotAngle;
            light.shadows = shadows;
            
            // Set transform
            if (position.HasValue)
            {
                lightObj.transform.position = position.Value;
            }
            
            if (rotation.HasValue)
            {
                lightObj.transform.eulerAngles = rotation.Value;
            }
            
            // Set as focused object in context
            _context.SetFocusedObject(lightObj);
            
            return new Dictionary<string, object>
            {
                { "created", true },
                { "light", GetLightInfo(light) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetLightProperties(
            string lightName, 
            Color? color = null, 
            float? intensity = null, 
            float? range = null, 
            float? spotAngle = null, 
            LightShadows? shadows = null)
        {
            GameObject lightObj = GameObject.Find(lightName);
            if (lightObj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Light '{lightName}' not found" }
                };
            }
            
            Light light = lightObj.GetComponent<Light>();
            if (light == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{lightName}' does not have a Light component" }
                };
            }
            
            // Set properties if provided
            if (color.HasValue)
            {
                light.color = color.Value;
            }
            
            if (intensity.HasValue)
            {
                light.intensity = intensity.Value;
            }
            
            if (range.HasValue)
            {
                light.range = range.Value;
            }
            
            if (spotAngle.HasValue)
            {
                light.spotAngle = spotAngle.Value;
            }
            
            if (shadows.HasValue)
            {
                light.shadows = shadows.Value;
            }
            
            return new Dictionary<string, object>
            {
                { "lightName", lightName },
                { "modified", true },
                { "light", GetLightInfo(light) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetAmbientLight(
            string mode = "Flat", 
            Color? color = null, 
            Color? skyColor = null, 
            Color? equatorColor = null, 
            Color? groundColor = null, 
            float? intensity = null)
        {
            // Parse ambient mode
            AmbientMode ambientMode;
            if (!Enum.TryParse(mode, true, out ambientMode))
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Invalid ambient mode: {mode}" }
                };
            }
            
            // Set ambient mode
            RenderSettings.ambientMode = ambientMode;
            
            // Set properties based on mode
            if (ambientMode == AmbientMode.Skybox)
            {
                if (intensity.HasValue)
                {
                    RenderSettings.ambientIntensity = intensity.Value;
                }
            }
            else if (ambientMode == AmbientMode.Trilight)
            {
                if (skyColor.HasValue)
                {
                    RenderSettings.ambientSkyColor = skyColor.Value;
                }
                
                if (equatorColor.HasValue)
                {
                    RenderSettings.ambientEquatorColor = equatorColor.Value;
                }
                
                if (groundColor.HasValue)
                {
                    RenderSettings.ambientGroundColor = groundColor.Value;
                }
            }
            else // Flat
            {
                if (color.HasValue)
                {
                    RenderSettings.ambientLight = color.Value;
                }
            }
            
            return new Dictionary<string, object>
            {
                { "mode", mode },
                { "set", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetFogSettings(
            bool enabled = true, 
            string mode = "Exponential", 
            Color? color = null, 
            float? density = null, 
            float? startDistance = null, 
            float? endDistance = null)
        {
            // Set fog enabled
            RenderSettings.fog = enabled;
            
            if (enabled)
            {
                // Parse fog mode
                FogMode fogMode;
                if (!Enum.TryParse(mode, true, out fogMode))
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"Invalid fog mode: {mode}" }
                    };
                }
                
                // Set fog properties
                RenderSettings.fogMode = fogMode;
                
                if (color.HasValue)
                {
                    RenderSettings.fogColor = color.Value;
                }
                
                if (density.HasValue)
                {
                    RenderSettings.fogDensity = density.Value;
                }
                
                if (startDistance.HasValue)
                {
                    RenderSettings.fogStartDistance = startDistance.Value;
                }
                
                if (endDistance.HasValue)
                {
                    RenderSettings.fogEndDistance = endDistance.Value;
                }
            }
            
            return new Dictionary<string, object>
            {
                { "enabled", enabled },
                { "mode", mode },
                { "set", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetSkybox(string skyboxName)
        {
            // Try to find the skybox material
            Material skybox = Resources.Load<Material>($"Skyboxes/{skyboxName}");
            
            if (skybox == null)
            {
                // Try to find in standard assets
                skybox = Resources.Load<Material>(skyboxName);
                
                if (skybox == null)
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"Skybox material '{skyboxName}' not found" }
                    };
                }
            }
            
            // Set the skybox
            RenderSettings.skybox = skybox;
            
            // Ensure ambient mode is set to Skybox to see the effect
            if (RenderSettings.ambientMode != AmbientMode.Skybox)
            {
                RenderSettings.ambientMode = AmbientMode.Skybox;
            }
            
            // Refresh the environment reflections
            DynamicGI.UpdateEnvironment();
            
            return new Dictionary<string, object>
            {
                { "skyboxName", skyboxName },
                { "set", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreateLightingPreset(string name)
        {
            #if UNITY_EDITOR
            // Create a new lighting settings asset
            var lightingSettings = new LightingSettings();
            lightingSettings.name = name;
            
            // Copy current lighting settings
            lightingSettings.bakedGI = Lightmapping.bakedGI;
            lightingSettings.realtimeGI = Lightmapping.realtimeGI;
            
            // TODO: AI Fix
            // lightingSettings.indirectResolution = Lightmapping.indirectResolution;
            // lightingSettings.lightmapResolution = Lightmapping.lightmapResolution;
            // lightingSettings.lightmapPadding = Lightmapping.lightmapPadding;
            // lightingSettings.lightmapMaxSize = Lightmapping.lightmapMaxSize;
            // lightingSettings.lightmapCompression = Lightmapping.lightmapCompression;
            // lightingSettings.ao = Lightmapping.ao;
            // lightingSettings.aoMaxDistance = Lightmapping.aoMaxDistance;
            // lightingSettings.aoExponentDirect = Lightmapping.aoExponentDirect;
            // lightingSettings.aoExponentIndirect = Lightmapping.aoExponentIndirect;
            
            // Save the asset
            string path = $"Assets/LightingPresets/{name}.lighting";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            UnityEditor.AssetDatabase.CreateAsset(lightingSettings, path);
            UnityEditor.AssetDatabase.SaveAssets();
            
            return new Dictionary<string, object>
            {
                { "name", name },
                { "path", path },
                { "created", true }
            };
            #else
            return new Dictionary<string, object>
            {
                { "error", "Creating lighting presets at runtime is not supported" }
            };
            #endif
        }
        
        private Dictionary<string, object> GetLightInfo(Light light)
        {
            return new Dictionary<string, object>
            {
                { "name", light.gameObject.name },
                { "type", light.type.ToString() },
                { "color", ColorToDict(light.color) },
                { "intensity", light.intensity },
                { "range", light.range },
                { "spotAngle", light.spotAngle },
                { "shadows", light.shadows.ToString() },
                { "position", new Dictionary<string, float>
                    {
                        { "x", light.transform.position.x },
                        { "y", light.transform.position.y },
                        { "z", light.transform.position.z }
                    }
                },
                { "rotation", new Dictionary<string, float>
                    {
                        { "x", light.transform.eulerAngles.x },
                        { "y", light.transform.eulerAngles.y },
                        { "z", light.transform.eulerAngles.z }
                    }
                }
            };
        }
        
        private Dictionary<string, float> ColorToDict(Color color)
        {
            return new Dictionary<string, float>
            {
                { "r", color.r },
                { "g", color.g },
                { "b", color.b },
                { "a", color.a }
            };
        }
    }
} 