using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP
{
    /// <summary>
    /// Handles scene-related commands
    /// </summary>
    public class SceneCommandHandler : CommandHandler
    {
        [CommandMethod]
        public Dictionary<string, object> GetSceneInfo()
        {
            var activeScene = SceneManager.GetActiveScene();
            
            var result = new Dictionary<string, object>
            {
                { "name", activeScene.name },
                { "path", activeScene.path },
                { "buildIndex", activeScene.buildIndex },
                { "isDirty", activeScene.isDirty },
                { "isLoaded", activeScene.isLoaded },
                { "rootCount", activeScene.rootCount }
            };
            
            // Get root objects
            var rootObjects = new List<Dictionary<string, object>>();
            foreach (var rootObj in activeScene.GetRootGameObjects())
            {
                rootObjects.Add(GetObjectBasicInfo(rootObj));
            }
            
            result["rootObjects"] = rootObjects;
            
            return result;
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetAllScenes()
        {
            var scenes = new List<Dictionary<string, object>>();
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                scenes.Add(new Dictionary<string, object>
                {
                    { "name", scene.name },
                    { "path", scene.path },
                    { "buildIndex", scene.buildIndex },
                    { "isDirty", scene.isDirty },
                    { "isLoaded", scene.isLoaded },
                    { "rootCount", scene.rootCount },
                    { "isActive", scene == SceneManager.GetActiveScene() }
                });
            }
            
            return new Dictionary<string, object>
            {
                { "sceneCount", SceneManager.sceneCount },
                { "scenes", scenes }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            try
            {
                SceneManager.LoadScene(sceneName, mode);
                
                return new Dictionary<string, object>
                {
                    { "sceneName", sceneName },
                    { "loaded", true },
                    { "mode", mode.ToString() }
                };
            }
            catch (System.Exception e)
            {
                return new Dictionary<string, object>
                {
                    { "sceneName", sceneName },
                    { "loaded", false },
                    { "error", e.Message }
                };
            }
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreateNewScene(string sceneName = "New Scene")
        {
            #if UNITY_EDITOR
            // In the editor, we can create a new scene
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single
            );
            
            return new Dictionary<string, object>
            {
                { "sceneName", scene.name },
                { "created", true }
            };
            #else
            // At runtime, we can't create a new scene easily
            return new Dictionary<string, object>
            {
                { "created", false },
                { "error", "Creating new scenes at runtime is not supported" }
            };
            #endif
        }
        
        [CommandMethod]
        public Dictionary<string, object> SaveScene()
        {
            #if UNITY_EDITOR
            var activeScene = SceneManager.GetActiveScene();
            bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            
            return new Dictionary<string, object>
            {
                { "sceneName", activeScene.name },
                { "saved", saved }
            };
            #else
            return new Dictionary<string, object>
            {
                { "saved", false },
                { "error", "Saving scenes at runtime is not supported" }
            };
            #endif
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetSceneHierarchy()
        {
            var activeScene = SceneManager.GetActiveScene();
            var rootObjects = activeScene.GetRootGameObjects();
            
            var hierarchy = new List<Dictionary<string, object>>();
            foreach (var rootObj in rootObjects)
            {
                hierarchy.Add(GetObjectHierarchy(rootObj));
            }
            
            return new Dictionary<string, object>
            {
                { "sceneName", activeScene.name },
                { "hierarchy", hierarchy }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> FindObjectsOfType(string typeName)
        {
            var type = System.Type.GetType(typeName) ?? 
                       System.Type.GetType($"UnityEngine.{typeName}") ??
                       System.AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(a => a.GetTypes())
                           .FirstOrDefault(t => t.Name == typeName);
            
            if (type == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Type '{typeName}' not found" }
                };
            }
            
            var objects = Object.FindObjectsByType(type, FindObjectsSortMode.InstanceID);
            var result = new List<Dictionary<string, object>>();
            
            foreach (var obj in objects)
            {
                if (obj is GameObject gameObj)
                {
                    result.Add(GetObjectBasicInfo(gameObj));
                }
                else if (obj is Component component)
                {
                    result.Add(GetObjectBasicInfo(component.gameObject));
                }
            }
            
            return new Dictionary<string, object>
            {
                { "typeName", typeName },
                { "count", result.Count },
                { "objects", result }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetActiveScene(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName)
                {
                    bool success = SceneManager.SetActiveScene(scene);
                    return new Dictionary<string, object>
                    {
                        { "sceneName", sceneName },
                        { "activated", success }
                    };
                }
            }
            
            return new Dictionary<string, object>
            {
                { "sceneName", sceneName },
                { "activated", false },
                { "error", $"Scene '{sceneName}' not found or not loaded" }
            };
        }
        
        private Dictionary<string, object> GetObjectBasicInfo(GameObject obj)
        {
            return new Dictionary<string, object>
            {
                { "name", obj.name },
                { "id", obj.GetInstanceID() },
                { "active", obj.activeSelf },
                { "layer", obj.layer },
                { "tag", obj.tag },
                { "componentCount", obj.GetComponents<Component>().Length }
            };
        }
        
        private Dictionary<string, object> GetObjectHierarchy(GameObject obj)
        {
            var result = GetObjectBasicInfo(obj);
            
            if (obj.transform.childCount > 0)
            {
                var children = new List<Dictionary<string, object>>();
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    children.Add(GetObjectHierarchy(obj.transform.GetChild(i).gameObject));
                }
                result["children"] = children;
            }
            
            return result;
        }
    }
} 