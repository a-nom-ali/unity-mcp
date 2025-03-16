using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP
{
    /// <summary>
    /// Handles object-related commands
    /// </summary>
    public class ObjectCommandHandler : CommandHandler
    {
        [CommandMethod]
        public Dictionary<string, object> GetObjectInfo(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{name}' not found" }
                };
            }
            
            return GetDetailedObjectInfo(obj);
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetObjectInfoById(int id)
        {
            // Find object by instance ID
            #if UNITY_EDITOR
            GameObject obj = UnityEditor.EditorUtility.InstanceIDToObject(id) as GameObject;
            #else
            GameObject obj = null; // At runtime, we can't easily find by ID
            #endif
            
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object with ID {id} not found" }
                };
            }
            
            return GetDetailedObjectInfo(obj);
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreatePrimitive(
            string type = "Cube", 
            string name = null, 
            Vector3? position = null, 
            Vector3? rotation = null, 
            Vector3? scale = null)
        {
            // Parse primitive type
            PrimitiveType primitiveType;
            if (!Enum.TryParse(type, true, out primitiveType))
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Invalid primitive type: {type}" }
                };
            }
            
            // Create the primitive
            GameObject obj = GameObject.CreatePrimitive(primitiveType);
            
            // Set name if provided
            if (!string.IsNullOrEmpty(name))
            {
                obj.name = name;
            }
            
            // Set transform properties if provided
            if (position.HasValue)
            {
                obj.transform.position = position.Value;
            }
            
            if (rotation.HasValue)
            {
                obj.transform.eulerAngles = rotation.Value;
            }
            
            if (scale.HasValue)
            {
                obj.transform.localScale = scale.Value;
            }
            
            // Set as focused object in context
            _context.SetFocusedObject(obj);
            
            return new Dictionary<string, object>
            {
                { "created", true },
                { "object", GetDetailedObjectInfo(obj) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreateEmpty(
            string name = "New GameObject", 
            Vector3? position = null, 
            Vector3? rotation = null, 
            Vector3? scale = null)
        {
            // Create empty GameObject
            GameObject obj = new GameObject(name);
            
            // Set transform properties if provided
            if (position.HasValue)
            {
                obj.transform.position = position.Value;
            }
            
            if (rotation.HasValue)
            {
                obj.transform.eulerAngles = rotation.Value;
            }
            
            if (scale.HasValue)
            {
                obj.transform.localScale = scale.Value;
            }
            
            // Set as focused object in context
            _context.SetFocusedObject(obj);
            
            return new Dictionary<string, object>
            {
                { "created", true },
                { "object", GetDetailedObjectInfo(obj) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> DeleteObject(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{name}' not found" }
                };
            }
            
            // If this is the focused object, clear it
            if (_context.GetFocusedObject() == obj)
            {
                _context.SetFocusedObject(null);
            }
            
            // If this is in the selected objects, remove it
            _context.RemoveSelectedObject(obj);
            
            // Delete the object
            #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                GameObject.Destroy(obj);
            }
            else
            {
                GameObject.DestroyImmediate(obj);
            }
            #else
            GameObject.Destroy(obj);
            #endif
            
            return new Dictionary<string, object>
            {
                { "name", name },
                { "deleted", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetObjectTransform(
            string name, 
            Vector3? position = null, 
            Vector3? rotation = null, 
            Vector3? scale = null)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{name}' not found" }
                };
            }
            
            // Set transform properties if provided
            if (position.HasValue)
            {
                obj.transform.position = position.Value;
            }
            
            if (rotation.HasValue)
            {
                obj.transform.eulerAngles = rotation.Value;
            }
            
            if (scale.HasValue)
            {
                obj.transform.localScale = scale.Value;
            }
            
            return new Dictionary<string, object>
            {
                { "name", name },
                { "modified", true },
                { "transform", GetTransformInfo(obj.transform) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetObjectActive(string name, bool active)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{name}' not found" }
                };
            }
            
            obj.SetActive(active);
            
            return new Dictionary<string, object>
            {
                { "name", name },
                { "active", obj.activeSelf }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetObjectParent(string name, string parentName = null)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{name}' not found" }
                };
            }
            
            Transform parent = null;
            if (!string.IsNullOrEmpty(parentName))
            {
                GameObject parentObj = GameObject.Find(parentName);
                if (parentObj == null)
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"Parent object '{parentName}' not found" }
                    };
                }
                
                parent = parentObj.transform;
            }
            
            obj.transform.SetParent(parent, true);
            
            return new Dictionary<string, object>
            {
                { "name", name },
                { "parentName", parentName },
                { "reparented", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> AddComponent(string objectName, string componentType)
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' not found" }
                };
            }
            
            // Find the component type
            Type type = System.Type.GetType(componentType) ?? 
                        System.Type.GetType($"UnityEngine.{componentType}") ??
                        System.AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == componentType);
            
            if (type == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Component type '{componentType}' not found" }
                };
            }
            
            // Check if the type is a Component
            if (!typeof(Component).IsAssignableFrom(type))
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Type '{componentType}' is not a Component" }
                };
            }
            
            // Add the component
            Component component = obj.AddComponent(type);
            
            return new Dictionary<string, object>
            {
                { "objectName", objectName },
                { "componentType", componentType },
                { "added", component != null },
                { "componentInfo", GetComponentInfo(component) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> RemoveComponent(string objectName, string componentType)
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' not found" }
                };
            }
            
            // Find the component type
            Type type = System.Type.GetType(componentType) ?? 
                        System.Type.GetType($"UnityEngine.{componentType}") ??
                        System.AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == componentType);
            
            if (type == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Component type '{componentType}' not found" }
                };
            }
            
            // Get the component
            Component component = obj.GetComponent(type);
            if (component == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' does not have a component of type '{componentType}'" }
                };
            }
            
            // Remove the component
            #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                GameObject.Destroy(component);
            }
            else
            {
                GameObject.DestroyImmediate(component);
            }
            #else
            GameObject.Destroy(component);
            #endif
            
            return new Dictionary<string, object>
            {
                { "objectName", objectName },
                { "componentType", componentType },
                { "removed", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> DuplicateObject(string name, string newName = null)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{name}' not found" }
                };
            }
            
            // Duplicate the object
            GameObject duplicate = GameObject.Instantiate(obj, obj.transform.parent);
            
            // Set name if provided
            if (!string.IsNullOrEmpty(newName))
            {
                duplicate.name = newName;
            }
            else
            {
                duplicate.name = $"{obj.name}_Copy";
            }
            
            // Set as focused object in context
            _context.SetFocusedObject(duplicate);
            
            return new Dictionary<string, object>
            {
                { "originalName", name },
                { "duplicated", true },
                { "object", GetDetailedObjectInfo(duplicate) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> FocusObject(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{name}' not found" }
                };
            }
            
            // Set as focused object in context
            _context.SetFocusedObject(obj);
            
            // Also frame the object in the editor view
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Selection.activeGameObject = obj;
                UnityEditor.SceneView.FrameLastActiveSceneView();
            }
            #endif
            
            return new Dictionary<string, object>
            {
                { "name", name },
                { "focused", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SelectObject(string name, bool addToSelection = false)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{name}' not found" }
                };
            }
            
            // Update selection in context
            if (!addToSelection)
            {
                _context.ClearSelectedObjects();
            }
            
            _context.AddSelectedObject(obj);
            
            // Also update editor selection
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (!addToSelection)
                {
                    UnityEditor.Selection.objects = new UnityEngine.Object[] { obj };
                }
                else
                {
                    List<UnityEngine.Object> selection = new List<UnityEngine.Object>(UnityEditor.Selection.objects);
                    if (!selection.Contains(obj))
                    {
                        selection.Add(obj);
                        UnityEditor.Selection.objects = selection.ToArray();
                    }
                }
            }
            #endif
            
            return new Dictionary<string, object>
            {
                { "name", name },
                { "selected", true },
                { "selectionCount", _context.GetSelectedObjects().Count }
            };
        }
        
        private Dictionary<string, object> GetDetailedObjectInfo(GameObject obj)
        {
            var result = new Dictionary<string, object>
            {
                { "name", obj.name },
                { "id", obj.GetInstanceID() },
                { "active", obj.activeSelf },
                { "activeInHierarchy", obj.activeInHierarchy },
                { "layer", obj.layer },
                { "layerName", LayerMask.LayerToName(obj.layer) },
                { "tag", obj.tag },
                { "transform", GetTransformInfo(obj.transform) },
                { "parent", obj.transform.parent != null ? obj.transform.parent.name : null },
                { "childCount", obj.transform.childCount }
            };
            
            // Get components
            var components = new List<Dictionary<string, object>>();
            foreach (var component in obj.GetComponents<Component>())
            {
                if (component != null) // Skip null components (can happen with missing scripts)
                {
                    components.Add(GetComponentInfo(component));
                }
            }
            result["components"] = components;
            
            // Get children
            if (obj.transform.childCount > 0)
            {
                var children = new List<string>();
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    children.Add(obj.transform.GetChild(i).name);
                }
                result["children"] = children;
            }
            
            return result;
        }
        
        private Dictionary<string, object> GetTransformInfo(Transform transform)
        {
            return new Dictionary<string, object>
            {
                { "position", new Dictionary<string, float>
                    {
                        { "x", transform.position.x },
                        { "y", transform.position.y },
                        { "z", transform.position.z }
                    }
                },
                { "rotation", new Dictionary<string, float>
                    {
                        { "x", transform.eulerAngles.x },
                        { "y", transform.eulerAngles.y },
                        { "z", transform.eulerAngles.z }
                    }
                },
                { "scale", new Dictionary<string, float>
                    {
                        { "x", transform.localScale.x },
                        { "y", transform.localScale.y },
                        { "z", transform.localScale.z }
                    }
                },
                { "localPosition", new Dictionary<string, float>
                    {
                        { "x", transform.localPosition.x },
                        { "y", transform.localPosition.y },
                        { "z", transform.localPosition.z }
                    }
                },
                { "localRotation", new Dictionary<string, float>
                    {
                        { "x", transform.localEulerAngles.x },
                        { "y", transform.localEulerAngles.y },
                        { "z", transform.localEulerAngles.z }
                    }
                }
            };
        }
        
        private Dictionary<string, object> GetComponentInfo(Component component)
        {
            var result = new Dictionary<string, object>
            {
                { "type", component.GetType().Name },
                { "enabled", component is Behaviour behaviour ? behaviour.enabled : true }
            };
            
            // Add specific component info based on type
            if (component is Renderer renderer)
            {
                result["rendererInfo"] = new Dictionary<string, object>
                {
                    { "materialCount", renderer.sharedMaterials.Length },
                    { "isVisible", renderer.isVisible },
                    { "bounds", new Dictionary<string, object>
                        {
                            { "center", new Dictionary<string, float>
                                {
                                    { "x", renderer.bounds.center.x },
                                    { "y", renderer.bounds.center.y },
                                    { "z", renderer.bounds.center.z }
                                }
                            },
                            { "size", new Dictionary<string, float>
                                {
                                    { "x", renderer.bounds.size.x },
                                    { "y", renderer.bounds.size.y },
                                    { "z", renderer.bounds.size.z }
                                }
                            }
                        }
                    }
                };
                
                // Get material info
                if (renderer.sharedMaterial != null)
                {
                    result["material"] = new Dictionary<string, object>
                    {
                        { "name", renderer.sharedMaterial.name },
                        { "shader", renderer.sharedMaterial.shader.name },
                        { "color", renderer.sharedMaterial.HasProperty("_Color") ? new Dictionary<string, float>
                            {
                                { "r", renderer.sharedMaterial.color.r },
                                { "g", renderer.sharedMaterial.color.g },
                                { "b", renderer.sharedMaterial.color.b },
                                { "a", renderer.sharedMaterial.color.a }
                            } : null
                        }
                    };
                }
            }
            else if (component is Collider collider)
            {
                result["colliderInfo"] = new Dictionary<string, object>
                {
                    { "isTrigger", collider.isTrigger },
                    { "enabled", collider.enabled },
                    { "bounds", new Dictionary<string, object>
                        {
                            { "center", new Dictionary<string, float>
                                {
                                    { "x", collider.bounds.center.x },
                                    { "y", collider.bounds.center.y },
                                    { "z", collider.bounds.center.z }
                                }
                            },
                            { "size", new Dictionary<string, float>
                                {
                                    { "x", collider.bounds.size.x },
                                    { "y", collider.bounds.size.y },
                                    { "z", collider.bounds.size.z }
                                }
                            }
                        }
                    }
                };
            }
            else if (component is Light light)
            {
                result["lightInfo"] = new Dictionary<string, object>
                {
                    { "type", light.type.ToString() },
                    { "color", new Dictionary<string, float>
                        {
                            { "r", light.color.r },
                            { "g", light.color.g },
                            { "b", light.color.b },
                            { "a", light.color.a }
                        }
                    },
                    { "intensity", light.intensity },
                    { "range", light.range },
                    { "spotAngle", light.spotAngle },
                    { "shadows", light.shadows.ToString() }
                };
            }
            else if (component is Camera camera)
            {
                result["cameraInfo"] = new Dictionary<string, object>
                {
                    { "fieldOfView", camera.fieldOfView },
                    { "nearClipPlane", camera.nearClipPlane },
                    { "farClipPlane", camera.farClipPlane },
                    { "depth", camera.depth },
                    { "clearFlags", camera.clearFlags.ToString() },
                    { "backgroundColor", new Dictionary<string, float>
                        {
                            { "r", camera.backgroundColor.r },
                            { "g", camera.backgroundColor.g },
                            { "b", camera.backgroundColor.b },
                            { "a", camera.backgroundColor.a }
                        }
                    }
                };
            }
            
            return result;
        }
    }
} 