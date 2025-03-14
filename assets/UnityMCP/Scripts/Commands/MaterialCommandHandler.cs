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
                        material = new Material(Shader.Find("Standard"));
                        material.name = materialName;
                    }
                }
                
                // Create a new instance to avoid modifying the original
                material = new Material(material);
            }
            else
            {
                // Create a new material based on the current one
                material = new Material(renderer.sharedMaterial);
            }
            
            // Apply color if provided
            if (color.HasValue)
            {
                material.color = color.Value;
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
        public Dictionary<string, object> CreateMaterial(string name, Color? color = null, string shader = "Standard")
        {
            // Find the shader
            Shader shaderObj = Shader.Find(shader);
            if (shaderObj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Shader '{shader}' not found" }
                };
            }
            
            // Create the material
            Material material = new Material(shaderObj);
            material.name = name;
            
            // Set color if provided
            if (color.HasValue)
            {
                material.color = color.Value;
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
                
                materials.Add(new Dictionary<string, object>
                {
                    { "name", material.name },
                    { "path", path },
                    { "shader", material.shader.name }
                });
            }
            #else
            // At runtime, we can only find materials that are loaded
            var loadedMaterials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var material in loadedMaterials)
            {
                materials.Add(new Dictionary<string, object>
                {
                    { "name", material.name },
                    { "shader", material.shader.name }
                });
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
                    if (material.HasProperty(propertyName))
                    {
                        material.SetColor(propertyName, parsedColor);
                        propertySet = true;
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
                }
            }
            
            if (!propertySet)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Could not set property '{propertyName}' on material" }
                };
            }
            
            // Apply the modified material
            renderer.material = material;
            
            return new Dictionary<string, object>
            {
                { "objectName", objectName },
                { "propertyName", propertyName },
                { "set", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetShaderList()
        {
            var shaders = new List<Dictionary<string, object>>();
            
            var allShaders = Resources.FindObjectsOfTypeAll<Shader>();
            foreach (var shader in allShaders)
            {
                shaders.Add(new Dictionary<string, object>
                {
                    { "name", shader.name },
                    { "renderQueue", shader.renderQueue },
                    { "isSupported", shader.isSupported }
                });
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
            // Create a new material with standard shader
            Material material = new Material(Shader.Find("Standard"));
            material.name = name;
            
            // Set properties
            if (albedo.HasValue)
            {
                material.color = albedo.Value;
            }
            
            material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            material.SetFloat("_Glossiness", Mathf.Clamp01(smoothness));
            
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
                    material.SetTexture("_MainTex", texture);
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
            var result = new Dictionary<string, object>
            {
                { "name", material.name },
                { "shader", material.shader.name },
                { "renderQueue", material.renderQueue }
            };
            
            // Add common properties
            if (material.HasProperty("_Color"))
            {
                Color color = material.color;
                result["color"] = new Dictionary<string, float>
                {
                    { "r", color.r },
                    { "g", color.g },
                    { "b", color.b },
                    { "a", color.a }
                };
            }
            
            if (material.HasProperty("_MainTex"))
            {
                Texture mainTex = material.GetTexture("_MainTex");
                if (mainTex != null)
                {
                    result["mainTexture"] = mainTex.name;
                }
            }
            
            // Add PBR properties for Standard shader
            if (material.shader.name.Contains("Standard"))
            {
                if (material.HasProperty("_Metallic"))
                {
                    result["metallic"] = material.GetFloat("_Metallic");
                }
                
                if (material.HasProperty("_Glossiness"))
                {
                    result["smoothness"] = material.GetFloat("_Glossiness");
                }
                
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
                }
            }
            
            return result;
        }
    }
} 