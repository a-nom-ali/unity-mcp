using UnityEngine;
using System;

namespace UnityMCP.Commands
{
    public class ModifyObjectCommand
    {
        public static void Execute(ModifyObjectParams parameters)
        {
            if (string.IsNullOrEmpty(parameters.name))
            {
                throw new ArgumentException("Object name is required");
            }
            
            GameObject obj = GameObject.Find(parameters.name);
            if (obj == null)
            {
                throw new ArgumentException($"Object '{parameters.name}' not found");
            }
            
            // Modify position if specified
            if (parameters.position != null)
            {
                obj.transform.position = (Vector3)parameters.position;
            }
            
            // Modify rotation if specified
            if (parameters.rotation != null)
            {
                obj.transform.eulerAngles = (Vector3)parameters.rotation;
            }
            
            // Modify scale if specified
            if (parameters.scale != null)
            {
                obj.transform.localScale = (Vector3)parameters.scale;
            }
            
            // Modify visibility if specified
            if (parameters.visible.HasValue)
            {
                obj.SetActive(parameters.visible.Value);
            }
        }
    }
} 