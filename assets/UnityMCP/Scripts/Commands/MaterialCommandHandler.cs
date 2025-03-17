using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP
{
    /// <summary>
    /// Handles material-related commands
    /// </summary>
    public class MaterialCommandHandler : CommandHandler
    {
        [CommandMethod]
        public Dictionary<string, object> SetMaterial(string objectName, string materialName = null, Color? color = null)
        {
            // Validate object name
            if (string.IsNullOrEmpty(objectName))
            {
                return new Dictionary<string, object>
                {
                    { "error", "Object name cannot be null or empty" }
                };
            }
            
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' not found" }
                };
            }
            
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' does not have a Renderer component" }
                };
            }
            
            Material material = null;
            
            // If material name is provided, try to find it
            if (!string.IsNullOrEmpty(materialName))
            {
                material = Resources.Load<Material>($"Materials/{materialName}");
                
                if (material == null)
                {
                    // Try to find in standard assets
                    material = Resources.Load<Material>(materialName);
                    
                    if (material == null)
                    {
                        // Create a new material with standard shader
                        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                        if (shader == null)
                        {
                            // Fallback to Standard shader if URP is not available
                            shader = Shader.Find("Standard");
                            if (shader == null)
                            {
                                // Ultimate fallback
                                shader = Shader.Find("Diffuse");
                            }
                        }
                        
                        material = new Material(shader);
                        material.name = materialName;
                    }
                }
                
                // Create a new instance to avoid modifying the original
                material = new Material(material);
            }
            else
            {
                // Create a new material based on the current one or use a default if null
                if (renderer.sharedMaterial != null)
                {
                    material = new Material(renderer.sharedMaterial);
                }
                else
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    material = new Material(shader);
                    material.name = "Default Material";
                }
            }
            
            // Apply color if provided
            if (color.HasValue)
            {
                // Ensure we're setting the color to the right property based on shader
                if (material.HasProperty("_BaseColor")) // URP
                {
                    material.SetColor("_BaseColor", color.Value);
                }
                else if (material.HasProperty("_Color")) // Standard/Legacy
                {
                    material.SetColor("_Color", color.Value);
                }
                else
                {
                    material.color = color.Value;
                }
            }
            
            // Apply the material
            renderer.material = material;
            
            return new Dictionary<string, object>
            {
                { "objectName", objectName },
                { "materialName", material.name },
                { "applied", true },
                { "materialInfo", GetMaterialInfo(material) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreateMaterial(string name, Color? color = null, string shader = "Universal Render Pipeline/Lit")
        {
            // Validate name
            if (string.IsNullOrEmpty(name))
            {
                return new Dictionary<string, object>
                {
                    { "error", "Material name cannot be null or empty" }
                };
            }
            
            // Find the shader
            Shader shaderObj = Shader.Find(shader);
            if (shaderObj == null)
            {
                // Try fallback shaders
                shaderObj = Shader.Find("Standard");
                if (shaderObj == null)
                {
                    shaderObj = Shader.Find("Diffuse");
                    if (shaderObj == null)
                    {
                        return new Dictionary<string, object>
                        {
                            { "error", $"Shader '{shader}' not found and no fallback shaders available" }
                        };
                    }
                }
                
                Debug.LogWarning($"Shader '{shader}' not found. Using '{shaderObj.name}' instead.");
            }
            
            // Create the material
            Material material = new Material(shaderObj);
            material.name = name;
            
            // Set color if provided
            if (color.HasValue)
            {
                // Ensure we're setting the color to the right property based on shader
                if (material.HasProperty("_BaseColor")) // URP
                {
                    material.SetColor("_BaseColor", color.Value);
                }
                else if (material.HasProperty("_Color")) // Standard/Legacy
                {
                    material.SetColor("_Color", color.Value);
                }
                else
                {
                    material.color = color.Value;
                }
            }
            
            // Save the material in the editor
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string path = $"Assets/Materials/{name}.mat";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                UnityEditor.AssetDatabase.CreateAsset(material, path);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            #endif
            
            return new Dictionary<string, object>
            {
                { "name", name },
                { "created", true },
                { "materialInfo", GetMaterialInfo(material) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetMaterialList()
        {
            var materials = new List<Dictionary<string, object>>();
            
            #if UNITY_EDITOR
            // In the editor, we can find all materials in the project
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Material");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (material != null)
                {
                    materials.Add(new Dictionary<string, object>
                    {
                        { "name", material.name },
                        { "path", path },
                        { "shader", material.shader != null ? material.shader.name : "Unknown" }
                    });
                }
            }
            #else
            // At runtime, we can only find materials that are loaded
            var loadedMaterials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var material in loadedMaterials)
            {
                if (material != null)
                {
                    materials.Add(new Dictionary<string, object>
                    {
                        { "name", material.name },
                        { "shader", material.shader != null ? material.shader.name : "Unknown" }
                    });
                }
            }
            #endif
            
            return new Dictionary<string, object>
            {
                { "count", materials.Count },
                { "materials", materials }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetMaterialProperty(string objectName, string propertyName, object value)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(objectName))
            {
                return new Dictionary<string, object>
                {
                    { "error", "Object name cannot be null or empty" }
                };
            }
            
            if (string.IsNullOrEmpty(propertyName))
            {
                return new Dictionary<string, object>
                {
                    { "error", "Property name cannot be null or empty" }
                };
            }
            
            if (value == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", "Property value cannot be null" }
                };
            }
            
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' not found" }
                };
            }
            
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' does not have a Renderer component" }
                };
            }
            
            // Check if the renderer has a material
            if (renderer.sharedMaterial == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' does not have a material assigned" }
                };
            }
            
            // Create a new material instance to avoid modifying shared materials
            Material material = new Material(renderer.sharedMaterial);
            
            // Set the property based on its type
            bool propertySet = false;
            
            if (value is float floatValue)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetFloat(propertyName, floatValue);
                    propertySet = true;
                }
            }
            else if (value is int intValue)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetInt(propertyName, intValue);
                    propertySet = true;
                }
            }
            else if (value is Color colorValue)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetColor(propertyName, colorValue);
                    propertySet = true;
                }
                else
                {
                    // Try standard color property names as fallbacks
                    string[] colorPropertyNames = new[] { "_Color", "_BaseColor", "_MainColor" };
                    foreach (string colorProp in colorPropertyNames)
                    {
                        if (material.HasProperty(colorProp))
                        {
                            material.SetColor(colorProp, colorValue);
                            propertySet = true;
                            propertyName = colorProp; // Update the property name for the response
                            break;
                        }
                    }
                }
            }
            else if (value is Vector4 vector4Value)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetVector(propertyName, vector4Value);
                    propertySet = true;
                }
            }
            else if (value is string stringValue)
            {
                // Try to parse as color
                if (ColorUtility.TryParseHtmlString(stringValue, out Color parsedColor))
                {
                    // Try the specified property first
                    if (material.HasProperty(propertyName))
                    {
                        material.SetColor(propertyName, parsedColor);
                        propertySet = true;
                    }
                    else
                    {
                        // Try standard color property names as fallbacks
                        string[] colorPropertyNames = new[] { "_Color", "_BaseColor", "_MainColor" };
                        foreach (string colorProp in colorPropertyNames)
                        {
                            if (material.HasProperty(colorProp))
                            {
                                material.SetColor(colorProp, parsedColor);
                                propertySet = true;
                                propertyName = colorProp; // Update the property name for the response
                                break;
                            }
                        }
                    }
                }
                // Try to parse as texture path
                else
                {
                    Texture texture = Resources.Load<Texture>(stringValue);
                    if (texture != null && material.HasProperty(propertyName))
                    {
                        material.SetTexture(propertyName, texture);
                        propertySet = true;
                    }
                    else if (texture != null)
                    {
                        // Try standard texture property names as fallbacks
                        string[] texturePropertyNames = new[] { "_MainTex", "_BaseMap" };
                        foreach (string texProp in texturePropertyNames)
                        {
                            if (material.HasProperty(texProp))
                            {
                                material.SetTexture(texProp, texture);
                                propertySet = true;
                                propertyName = texProp; // Update the property name for the response
                                break;
                            }
                        }
                    }
                }
            }
            
            if (!propertySet)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Could not set property '{propertyName}' on material. Property not found or value type mismatch." }
                };
            }
            
            // Apply the modified material
            renderer.material = material;
            
            return new Dictionary<string, object>
            {
                { "objectName", objectName },
                { "propertyName", propertyName },
                { "set", true },
                { "materialInfo", GetMaterialInfo(material) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetShaderList()
        {
            var shaders = new List<Dictionary<string, object>>();
            
            var allShaders = Resources.FindObjectsOfTypeAll<Shader>();
            foreach (var shader in allShaders)
            {
                if (shader != null)
                {
                    shaders.Add(new Dictionary<string, object>
                    {
                        { "name", shader.name },
                        { "renderQueue", shader.renderQueue },
                        { "isSupported", shader.isSupported }
                    });
                }
            }
            
            return new Dictionary<string, object>
            {
                { "count", shaders.Count },
                { "shaders", shaders }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreatePBRMaterial(
            string name, 
            Color? albedo = null, 
            float metallic = 0.0f, 
            float smoothness = 0.5f, 
            Color? emission = null, 
            string albedoTexture = null)
        {
            // Validate name
            if (string.IsNullOrEmpty(name))
            {
                return new Dictionary<string, object>
                {
                    { "error", "Material name cannot be null or empty" }
                };
            }
            
            // Find appropriate shader
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                // Try URP shader as fallback
                shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    return new Dictionary<string, object>
                    {
                        { "error", "Could not find Standard or URP shader for PBR material" }
                    };
                }
            }
            
            // Create a new material with standard shader
            Material material = new Material(shader);
            material.name = name;
            
            // Set properties based on shader type
            bool isURP = shader.name.Contains("Universal Render Pipeline");
            
            // Set albedo/base color
            if (albedo.HasValue)
            {
                if (isURP)
                {
                    material.SetColor("_BaseColor", albedo.Value);
                }
                else
                {
                    material.SetColor("_Color", albedo.Value);
                }
            }
            
            // Set metallic
            if (isURP)
            {
                material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            }
            else
            {
                material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            }
            
            // Set smoothness
            if (isURP)
            {
                material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            }
            else
            {
                material.SetFloat("_Glossiness", Mathf.Clamp01(smoothness));
            }
            
            // Set emission
            if (emission.HasValue)
            {
                material.SetColor("_EmissionColor", emission.Value);
                material.EnableKeyword("_EMISSION");
            }
            
            // Load and set albedo texture if provided
            if (!string.IsNullOrEmpty(albedoTexture))
            {
                Texture2D texture = Resources.Load<Texture2D>(albedoTexture);
                if (texture != null)
                {
                    if (isURP)
                    {
                        material.SetTexture("_BaseMap", texture);
                    }
                    else
                    {
                        material.SetTexture("_MainTex", texture);
                    }
                }
            }
            
            // Save the material in the editor
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string path = $"Assets/Materials/{name}.mat";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                UnityEditor.AssetDatabase.CreateAsset(material, path);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            #endif
            
            return new Dictionary<string, object>
            {
                { "name", name },
                { "created", true },
                { "materialInfo", GetMaterialInfo(material) }
            };
        }
        
        private Dictionary<string, object> GetMaterialInfo(Material material)
        {
            if (material == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", "Material is null" }
                };
            }
            
            var result = new Dictionary<string, object>
            {
                { "name", material.name },
                { "shader", material.shader != null ? material.shader.name : "Unknown" },
                { "renderQueue", material.renderQueue }
            };
            
            // Determine if this is a URP or Standard shader
            bool isURP = material.shader != null && material.shader.name.Contains("Universal Render Pipeline");
            
            // Add color property
            if (isURP && material.HasProperty("_BaseColor"))
            {
                Color color = material.GetColor("_BaseColor");
                result["color"] = new Dictionary<string, float>
                {
                    { "r", color.r },
                    { "g", color.g },
                    { "b", color.b },
                    { "a", color.a }
                };
                result["hexColor"] = "#" + ColorUtility.ToHtmlStringRGBA(color);
            }
            else if (material.HasProperty("_Color"))
            {
                Color color = material.GetColor("_Color");
                result["color"] = new Dictionary<string, float>
                {
                    { "r", color.r },
                    { "g", color.g },
                    { "b", color.b },
                    { "a", color.a }
                };
                result["hexColor"] = "#" + ColorUtility.ToHtmlStringRGBA(color);
            }
            
            // Add texture property
            if (isURP && material.HasProperty("_BaseMap"))
            {
                Texture mainTex = material.GetTexture("_BaseMap");
                if (mainTex != null)
                {
                    result["mainTexture"] = mainTex.name;
                }
            }
            else if (material.HasProperty("_MainTex"))
            {
                Texture mainTex = material.GetTexture("_MainTex");
                if (mainTex != null)
                {
                    result["mainTexture"] = mainTex.name;
                }
            }
            
            // Add PBR properties
            if (isURP)
            {
                if (material.HasProperty("_Metallic"))
                {
                    result["metallic"] = material.GetFloat("_Metallic");
                }
                
                if (material.HasProperty("_Smoothness"))
                {
                    result["smoothness"] = material.GetFloat("_Smoothness");
                }
            }
            else if (material.shader.name.Contains("Standard"))
            {
                if (material.HasProperty("_Metallic"))
                {
                    result["metallic"] = material.GetFloat("_Metallic");
                }
                
                if (material.HasProperty("_Glossiness"))
                {
                    result["smoothness"] = material.GetFloat("_Glossiness");
                }
            }
            
            // Add emission property
            if (material.HasProperty("_EmissionColor"))
            {
                Color emission = material.GetColor("_EmissionColor");
                result["emission"] = new Dictionary<string, float>
                {
                    { "r", emission.r },
                    { "g", emission.g },
                    { "b", emission.b },
                    { "a", emission.a }
                };
                result["emissionHexColor"] = "#" + ColorUtility.ToHtmlStringRGBA(emission);
                result["emissionEnabled"] = material.IsKeywordEnabled("_EMISSION");
            }
            
            return result;
        }
    }
}