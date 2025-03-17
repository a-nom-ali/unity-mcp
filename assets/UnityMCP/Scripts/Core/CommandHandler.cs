using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

namespace UnityMCP
{
    /// <summary>
    /// Base class for command handlers that process specific types of commands
    /// </summary>
    public class CommandHandler
    {
        protected UnityMCPContext _context;
        private Dictionary<string, MethodInfo> _commandMethods;
        private Dictionary<string, Func<string, string>> _legacyCommandHandlers;
        
        public CommandHandler()
        {
            // Discover command methods using reflection
            DiscoverCommandMethods();
            
            // Initialize legacy command handlers
            InitializeLegacyHandlers();
        }
        
        protected void DiscoverCommandMethods()
        {
            _commandMethods = new Dictionary<string, MethodInfo>();
            
            // Get all methods in the derived class
            var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var method in methods)
            {
                // Check if the method has the CommandMethod attribute
                var attribute = method.GetCustomAttribute<CommandMethodAttribute>();
                if (attribute != null)
                {
                    string commandName = attribute.CommandName ?? method.Name;
                    _commandMethods[commandName.ToLower()] = method;
                }
            }
        }
        
        protected virtual void InitializeLegacyHandlers()
        {
            // Initialize legacy command handlers
            _legacyCommandHandlers = new Dictionary<string, Func<string, string>>
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
        
        public void SetContext(UnityMCPContext context)
        {
            _context = context;
        }
        
        public string ExecuteCommand(string action, string parameters)
        {
            // Convert action to lowercase for case-insensitive matching
            action = action.ToLower();
            
            // First try the attribute-based command methods
            if (_commandMethods.TryGetValue(action, out var method))
            {
                try
                {
                    // Parse parameters based on the method's parameter types
                    object[] methodParams = ParseParameters(method, parameters);
                    
                    // Invoke the method
                    object result = method.Invoke(this, methodParams);
                    
                    // Convert result to JSON response
                    return CreateSuccessResponse(result);
                }
                catch (Exception e)
                {
                    return JsonUtility.CreateErrorResponse($"Error executing command {action}: {e.Message}");
                }
            }
            // Then try legacy command handlers
            else if (_legacyCommandHandlers != null && _legacyCommandHandlers.TryGetValue(action, out var handler))
            {
                try
                {
                    return handler(parameters);
                }
                catch (Exception e)
                {
                    return JsonUtility.CreateErrorResponse($"Error executing {action}: {e.Message}");
                }
            }
            else
            {
                return JsonUtility.CreateErrorResponse($"Unknown command action: {action}");
            }
        }
        
        // Support for legacy CommandData format
        public string ExecuteCommand(CommandData command)
        {
            if (string.IsNullOrEmpty(command.type))
            {
                return JsonUtility.CreateErrorResponse("Command type is required");
            }
            
            return ExecuteCommand(command.type, command.parameters);
        }
        
        protected virtual object[] ParseParameters(MethodInfo method, string parametersJson)
        {
            var parameters = method.GetParameters();
            
            // If there are no parameters, return an empty array
            if (parameters.Length == 0)
            {
                return new object[0];
            }
            
            // Parse the JSON parameters
            var parsedParams = string.IsNullOrEmpty(parametersJson) 
                ? new Dictionary<string, object>() 
                : JsonUtility.FromJson<Dictionary<string, object>>(parametersJson);
            
            // Create an array to hold the parameter values
            object[] paramValues = new object[parameters.Length];
            
            // Fill in the parameter values
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                
                // Check if the parameter exists in the parsed JSON
                if (parsedParams.TryGetValue(param.Name, out var value))
                {
                    // Convert the value to the parameter type
                    paramValues[i] = ConvertValue(value, param.ParameterType);
                }
                else if (param.HasDefaultValue)
                {
                    // Use the default value
                    paramValues[i] = param.DefaultValue;
                }
                else
                {
                    // Parameter is required but not provided
                    throw new ArgumentException($"Required parameter '{param.Name}' not provided");
                }
            }
            
            return paramValues;
        }
        
        protected virtual object ConvertValue(object value, Type targetType)
        {
            // Handle null values
            if (value == null)
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
            
            // Handle conversion to string
            if (targetType == typeof(string))
            {
                return value.ToString();
            }
            
            // Handle conversion to primitive types
            if (targetType.IsPrimitive)
            {
                return Convert.ChangeType(value, targetType);
            }
            
            // Handle conversion to enum
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value.ToString(), true);
            }
            
            // Handle conversion to Unity types
            if (targetType == typeof(Vector3?))
            {
                Dictionary<string, object> dict = (value as JObject).ToObject<Dictionary<string, object>>();
                float x = Convert.ToSingle(dict.GetValueOrDefault("x", 0));
                float y = Convert.ToSingle(dict.GetValueOrDefault("y", 0));
                float z = Convert.ToSingle(dict.GetValueOrDefault("z", 0));
                return new Vector3(x, y, z);
            }
            
            // Handle conversion to Unity types
            if (targetType == typeof(Color?))
            {
                float[] list = value.GetType() == typeof(JArray) 
                    ? (value as JArray).ToObject<float[]>() 
                    : (value as JObject).ToObject<float[]>();
                float r = Convert.ToSingle(list[0]);
                float g = Convert.ToSingle(list[1]);
                float b = Convert.ToSingle(list[2]);
                float a = Convert.ToSingle(list[3]);
                return new Color(r, g, b, a);
            }
            
            // Handle conversion to lists and arrays
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = targetType.GetGenericArguments()[0];
                var list = Activator.CreateInstance(targetType);
                var addMethod = targetType.GetMethod("Add");
                
                if (value is IEnumerable<object> enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        var convertedItem = ConvertValue(item, elementType);
                        addMethod.Invoke(list, new[] { convertedItem });
                    }
                }
                
                return list;
            }
            
            // Handle conversion to complex types
            return Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(JsonUtility.ToJson(value), targetType);
        }
        
        protected virtual string CreateSuccessResponse(object result)
        {
            return JsonUtility.CreateSuccessResponse(result);
        }
        
        // Legacy command handler methods
        protected virtual string GetSceneInfo(string parameters)
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
            
            foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.InstanceID))
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

        protected virtual string GetObjectInfo(string parameters)
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

        protected virtual string CreateObject(string parameters)
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

        protected virtual string ModifyObject(string parameters)
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
                    obj.transform.position = (Vector3)paramsObj.position;
                }
                
                // Modify rotation if specified
                if (paramsObj.rotation != null)
                {
                    obj.transform.eulerAngles = (Vector3)paramsObj.rotation;
                }
                
                // Modify scale if specified
                if (paramsObj.scale != null)
                {
                    obj.transform.localScale = (Vector3)paramsObj.scale;
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

        protected virtual string DeleteObject(string parameters)
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

        protected virtual string SetMaterial(string parameters)
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

        protected virtual string ExecuteUnityCode(string parameters)
        {
            // This is a placeholder - executing arbitrary C# code at runtime is complex in Unity
            // and would require a C# script evaluation library or custom implementation
            return JsonUtility.CreateErrorResponse("Executing arbitrary Unity code is not supported in this version");
        }

        protected virtual string GetAssetCategories(string parameters)
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

        protected virtual string SearchAssets(string parameters)
        {
            // Placeholder for asset store integration
            return JsonUtility.CreateErrorResponse("Asset store integration is not implemented in this version");
        }

        protected virtual string DownloadAsset(string parameters)
        {
            // Placeholder for asset store integration
            return JsonUtility.CreateErrorResponse("Asset store integration is not implemented in this version");
        }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandMethodAttribute : Attribute
    {
        public string CommandName { get; }
        
        public CommandMethodAttribute(string commandName = null)
        {
            CommandName = commandName;
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