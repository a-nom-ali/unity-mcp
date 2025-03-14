using UnityEngine;
using System;

namespace UnityMCP.Commands
{
    public class MaterialCommand
    {
        public static void ApplyMaterial(SetMaterialParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.objectName))
            {
                throw new ArgumentException("Object name is required");
            }
            
            GameObject obj = GameObject.Find(parameters.objectName);
            if (obj == null)
            {
                throw new ArgumentException($"Object '{parameters.objectName}' not found");
            }
            
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
            {
                throw new ArgumentException($"Object '{parameters.objectName}' does not have a renderer component");
            }
            
            // Create a new material instance to avoid modifying shared materials
            Material newMaterial = null;
            
            // If material name is specified, try to load it
            if (!string.IsNullOrEmpty(parameters.materialName))
            {
                newMaterial = Resources.Load<Material>($"DefaultMaterials/{parameters.materialName}");
                if (newMaterial == null)
                {
                    // Create a new default material
                    newMaterial = new Material(Shader.Find("Standard"));
                    newMaterial.name = parameters.materialName;
                }
            }
            else
            {
                // Create a new material based on the current one
                newMaterial = new Material(renderer.material);
            }
            
            // Apply color if specified
            if (parameters.color != null && parameters.color.Length >= 3)
            {
                Color color = new Color(
                    parameters.color[0],
                    parameters.color[1],
                    parameters.color[2],
                    parameters.color.Length >= 4 ? parameters.color[3] : 1.0f
                );
                
                newMaterial.color = color;
            }
            
            // Apply the material
            renderer.material = newMaterial;
        }
        
        public static Material CreateMaterial(string name, Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.name = name;
            material.color = color;
            return material;
        }
    }
} 