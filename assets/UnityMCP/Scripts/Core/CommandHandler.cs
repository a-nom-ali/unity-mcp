using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityMCP
{
    public class CommandHandler
    {
        private Dictionary<string, Func<string, string>> commandHandlers;

        public CommandHandler()
        {
            // Initialize command handlers
            commandHandlers = new Dictionary<string, Func<string, string>>
            {
                { "get_scene_info", GetSceneInfo },
                { "get_object_info", GetObjectInfo },
                { "create_object", CreateObject },
                { "modify_object", ModifyObject },
                { "delete_object", DeleteObject },
                { "set_material", SetMaterial },
                { "execute_unity_code", ExecuteUnityCode },
                { "get_asset_categories", GetAssetCategories },
                { "search_assets", SearchAssets },
                { "download_asset", DownloadAsset }
            };
        }

        public string ExecuteCommand(CommandData command)
        {
            if (string.IsNullOrEmpty(command.type))
            {
                return JsonUtility.CreateErrorResponse("Command type is required");
            }

            if (commandHandlers.TryGetValue(command.type, out var handler))
            {
                try
                {
                    return handler(command.parameters);
                }
                catch (Exception e)
                {
                    return JsonUtility.CreateErrorResponse($"Error executing {command.type}: {e.Message}");
                }
            }
            else
            {
                return JsonUtility.CreateErrorResponse($"Unknown command type: {command.type}");
            }
        }

        private string GetSceneInfo(string parameters)
        {
            // Get information about the current scene
            var sceneInfo = new Dictionary<string, object>
            {
                { "name", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name },
                { "path", UnityEngine.SceneManagement.SceneManager.GetActiveScene().path },
                { "objects", GetSceneObjects() }
            };
            
            return JsonUtility.CreateSuccessResponse(sceneInfo);
        }

        private List<Dictionary<string, object>> GetSceneObjects()
        {
            var objects = new List<Dictionary<string, object>>();
            
            foreach (GameObject obj in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (obj.transform.parent == null) // Only root objects
                {
                    objects.Add(GetObjectData(obj));
                }
            }
            
            return objects;
        }

        private Dictionary<string, object> GetObjectData(GameObject obj)
        {
            var data = new Dictionary<string, object>
            {
                { "name", obj.name },
                { "active", obj.activeSelf },
                { "position", new float[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z } },
                { "rotation", new float[] { obj.transform.eulerAngles.x, obj.transform.eulerAngles.y, obj.transform.eulerAngles.z } },
                { "scale", new float[] { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z } }
            };
            
            // Add children
            if (obj.transform.childCount > 0)
            {
                var children = new List<Dictionary<string, object>>();
                
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    children.Add(GetObjectData(obj.transform.GetChild(i).gameObject));
                }
                
                data["children"] = children;
            }
            
            return data;
        }

        private string GetObjectInfo(string parameters)
        {
            try
            {
                var paramsObj = JsonUtility.FromJson<ObjectInfoParams>(parameters);
                
                if (string.IsNullOrEmpty(paramsObj.name))
                {
                    return JsonUtility.CreateErrorResponse("Object name is required");
                }
                
                GameObject obj = GameObject.Find(paramsObj.name);
                if (obj == null)
                {
                    return JsonUtility.CreateErrorResponse($"Object '{paramsObj.name}' not found");
                }
                
                return JsonUtility.CreateSuccessResponse(GetObjectData(obj));
            }
            catch (Exception e)
            {
                return JsonUtility.CreateErrorResponse($"Error getting object info: {e.Message}");
            }
        }

        private string CreateObject(string parameters)
        {
            try
            {
                var paramsObj = JsonUtility.FromJson<CreateObjectParams>(parameters);
                
                // Default values
                string type = paramsObj.type ?? "Cube";
                string name = paramsObj.name ?? type;
                Vector3 position = paramsObj.position ?? Vector3.zero;
                Vector3 rotation = paramsObj.rotation ?? Vector3.zero;
                Vector3 scale = paramsObj.scale ?? Vector3.one;
                
                GameObject newObject = null;
                
                // Create the object based on type
                switch (type.ToLower())
                {
                    case "cube":
                        newObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        break;
                    case "sphere":
                        newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        break;
                    case "cylinder":
                        newObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        break;
                    case "plane":
                        newObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        break;
                    case "capsule":
                        newObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        break;
                    case "quad":
                        newObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        break;
                    case "empty":
                        newObject = new GameObject();
                        break;
                    default:
                        return JsonUtility.CreateErrorResponse($"Unknown object type: {type}");
                }
                
                // Set properties
                newObject.name = name;
                newObject.transform.position = position;
                newObject.transform.eulerAngles = rotation;
                newObject.transform.localScale = scale;
                
                // Apply material if specified
                if (!string.IsNullOrEmpty(paramsObj.material))
                {
                    var renderer = newObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Try to find the material
                        Material mat = Resources.Load<Material>($"DefaultMaterials/{paramsObj.material}");
                        if (mat != null)
                        {
                            renderer.material = mat;
                        }
                        else
                        {
                            Debug.LogWarning($"Material '{paramsObj.material}' not found");
                        }
                    }
                }
                
                // Apply color if specified
                if (paramsObj.color != null && paramsObj.color.Length >= 3)
                {
                    var renderer = newObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Color color = new Color(
                            paramsObj.color[0],
                            paramsObj.color[1],
                            paramsObj.color[2],
                            paramsObj.color.Length >= 4 ? paramsObj.color[3] : 1.0f
                        );
                        
                        // Create a new material instance to avoid modifying shared materials
                        renderer.material = new Material(renderer.material);
                        renderer.material.color = color;
                    }
                }
                
                return JsonUtility.CreateSuccessResponse(new Dictionary<string, object>
                {
                    { "name", newObject.name },
                    { "created", true }
                });
            }
            catch (Exception e)
            {
                return JsonUtility.CreateErrorResponse($"Error creating object: {e.Message}");
            }
        }

        private string ModifyObject(string parameters)
        {
            try
            {
                var paramsObj = JsonUtility.FromJson<ModifyObjectParams>(parameters);
                
                if (string.IsNullOrEmpty(paramsObj.name))
                {
                    return JsonUtility.CreateErrorResponse("Object name is required");
                }
                
                GameObject obj = GameObject.Find(paramsObj.name);
                if (obj == null)
                {
                    return JsonUtility.CreateErrorResponse($"Object '{paramsObj.name}' not found");
                }
                
                // Modify position if specified
                if (paramsObj.position != null)
                {
                    obj.transform.position = paramsObj.position;
                }
                
                // Modify rotation if specified
                if (paramsObj.rotation != null)
                {
                    obj.transform.eulerAngles = paramsObj.rotation;
                }
                
                // Modify scale if specified
                if (paramsObj.scale != null)
                {
                    obj.transform.localScale = paramsObj.scale;
                }
                
                // Modify visibility if specified
                if (paramsObj.visible.HasValue)
                {
                    obj.SetActive(paramsObj.visible.Value);
                }
                
                return JsonUtility.CreateSuccessResponse(new Dictionary<string, object>
                {
                    { "name", obj.name },
                    { "modified", true }
                });
            }
            catch (Exception e)
            {
                return JsonUtility.CreateErrorResponse($"Error modifying object: {e.Message}");
            }
        }

        private string DeleteObject(string parameters)
        {
            try
            {
                var paramsObj = JsonUtility.FromJson<DeleteObjectParams>(parameters);
                
                if (string.IsNullOrEmpty(paramsObj.name))
                {
                    return JsonUtility.CreateErrorResponse("Object name is required");
                }
                
                GameObject obj = GameObject.Find(paramsObj.name);
                if (obj == null)
                {
                    return JsonUtility.CreateErrorResponse($"Object '{paramsObj.name}' not found");
                }
                
                UnityEngine.Object.Destroy(obj);
                
                return JsonUtility.CreateSuccessResponse(new Dictionary<string, object>
                {
                    { "name", paramsObj.name },
                    { "deleted", true }
                });
            }
            catch (Exception e)
            {
                return JsonUtility.CreateErrorResponse($"Error deleting object: {e.Message}");
            }
        }

        private string SetMaterial(string parameters)
        {
            try
            {
                var paramsObj = JsonUtility.FromJson<SetMaterialParams>(parameters);
                
                if (string.IsNullOrEmpty(paramsObj.objectName))
                {
                    return JsonUtility.CreateErrorResponse("Object name is required");
                }
                
                GameObject obj = GameObject.Find(paramsObj.objectName);
                if (obj == null)
                {
                    return JsonUtility.CreateErrorResponse($"Object '{paramsObj.objectName}' not found");
                }
                
                var renderer = obj.GetComponent<Renderer>();
                if (renderer == null)
                {
                    return JsonUtility.CreateErrorResponse($"Object '{paramsObj.objectName}' does not have a renderer component");
                }
                
                // Create a new material instance to avoid modifying shared materials
                Material newMaterial = null;
                
                // If material name is specified, try to load it
                if (!string.IsNullOrEmpty(paramsObj.materialName))
                {
                    newMaterial = Resources.Load<Material>($"DefaultMaterials/{paramsObj.materialName}");
                    if (newMaterial == null)
                    {
                        // Create a new default material
                        newMaterial = new Material(Shader.Find("Standard"));
                        newMaterial.name = paramsObj.materialName;
                    }
                }
                else
                {
                    // Create a new material based on the current one
                    newMaterial = new Material(renderer.material);
                }
                
                // Apply color if specified
                if (paramsObj.color != null && paramsObj.color.Length >= 3)
                {
                    Color color = new Color(
                        paramsObj.color[0],
                        paramsObj.color[1],
                        paramsObj.color[2],
                        paramsObj.color.Length >= 4 ? paramsObj.color[3] : 1.0f
                    );
                    
                    newMaterial.color = color;
                }
                
                // Apply the material
                renderer.material = newMaterial;
                
                return JsonUtility.CreateSuccessResponse(new Dictionary<string, object>
                {
                    { "objectName", obj.name },
                    { "materialApplied", true }
                });
            }
            catch (Exception e)
            {
                return JsonUtility.CreateErrorResponse($"Error setting material: {e.Message}");
            }
        }

        private string ExecuteUnityCode(string parameters)
        {
            // This is a placeholder - executing arbitrary C# code at runtime is complex in Unity
            // and would require a C# script evaluation library or custom implementation
            return JsonUtility.CreateErrorResponse("Executing arbitrary Unity code is not supported in this version");
        }

        private string GetAssetCategories(string parameters)
        {
            // Placeholder for asset store integration
            var categories = new List<string>
            {
                "3D Models",
                "Textures",
                "Materials",
                "Prefabs",
                "Environments",
                "Characters",
                "Animations"
            };
            
            return JsonUtility.CreateSuccessResponse(new Dictionary<string, object>
            {
                { "categories", categories }
            });
        }

        private string SearchAssets(string parameters)
        {
            // Placeholder for asset store integration
            return JsonUtility.CreateErrorResponse("Asset store integration is not implemented in this version");
        }

        private string DownloadAsset(string parameters)
        {
            // Placeholder for asset store integration
            return JsonUtility.CreateErrorResponse("Asset store integration is not implemented in this version");
        }
    }

    [Serializable]
    public class ObjectInfoParams
    {
        public string name;
    }

    [Serializable]
    public class CreateObjectParams
    {
        public string type;
        public string name;
        public Vector3? position;
        public Vector3? rotation;
        public Vector3? scale;
        public string material;
        public float[] color;
    }

    [Serializable]
    public class ModifyObjectParams
    {
        public string name;
        public Vector3? position;
        public Vector3? rotation;
        public Vector3? scale;
        public bool? visible;
    }

    [Serializable]
    public class DeleteObjectParams
    {
        public string name;
    }

    [Serializable]
    public class SetMaterialParams
    {
        public string objectName;
        public string materialName;
        public float[] color;
    }
} 