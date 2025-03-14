using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP.Subsystems
{
    /// <summary>
    /// Subsystem for managing cameras
    /// </summary>
    public class CameraSubsystem : MonoBehaviour, IUnityMCPSubsystem, ICommandProvider
    {
        private UnityMCPBrain _brain;
        private bool _initialized = false;
        private CameraCommandHandler _commandHandler;
        
        public void Initialize(UnityMCPBrain brain)
        {
            if (_initialized) return;
            
            _brain = brain;
            _commandHandler = new CameraCommandHandler();
            
            _initialized = true;
            _brain.LogInfo("Camera subsystem initialized");
        }
        
        public void Shutdown()
        {
            _initialized = false;
            _brain.LogInfo("Camera subsystem shut down");
        }
        
        public string GetName()
        {
            return "Camera";
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
                { "camera", _commandHandler }
            };
        }
    }
    
    /// <summary>
    /// Command handler for camera operations
    /// </summary>
    public class CameraCommandHandler : CommandHandler
    {
        [CommandMethod]
        public Dictionary<string, object> GetCameraInfo(string cameraName = null)
        {
            Camera camera;
            
            if (string.IsNullOrEmpty(cameraName))
            {
                // Get the main camera
                camera = Camera.main;
                if (camera == null)
                {
                    return new Dictionary<string, object>
                    {
                        { "error", "No main camera found in the scene" }
                    };
                }
            }
            else
            {
                // Find the specified camera
                GameObject cameraObj = GameObject.Find(cameraName);
                if (cameraObj == null)
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"Camera '{cameraName}' not found" }
                    };
                }
                
                camera = cameraObj.GetComponent<Camera>();
                if (camera == null)
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"Object '{cameraName}' does not have a Camera component" }
                    };
                }
            }
            
            return GetDetailedCameraInfo(camera);
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreateCamera(
            string name = "New Camera", 
            Vector3? position = null, 
            Vector3? rotation = null, 
            float fieldOfView = 60.0f, 
            float nearClipPlane = 0.3f, 
            float farClipPlane = 1000.0f, 
            bool isMain = false)
        {
            // Create a new GameObject for the camera
            GameObject cameraObj = new GameObject(name);
            
            // Add Camera component
            Camera camera = cameraObj.AddComponent<Camera>();
            
            // Set properties
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = nearClipPlane;
            camera.farClipPlane = farClipPlane;
            
            // Set transform
            if (position.HasValue)
            {
                cameraObj.transform.position = position.Value;
            }
            
            if (rotation.HasValue)
            {
                cameraObj.transform.eulerAngles = rotation.Value;
            }
            
            // Set as main camera if requested
            if (isMain)
            {
                // Remove the tag from any existing main camera
                if (Camera.main != null && Camera.main != camera)
                {
                    Camera.main.gameObject.tag = "Untagged";
                }
                
                cameraObj.tag = "MainCamera";
            }
            
            // Set as focused object in context
            _context.SetFocusedObject(cameraObj);
            
            return new Dictionary<string, object>
            {
                { "created", true },
                { "camera", GetDetailedCameraInfo(camera) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetCameraProperties(
            string cameraName, 
            float? fieldOfView = null, 
            float? nearClipPlane = null, 
            float? farClipPlane = null, 
            CameraClearFlags? clearFlags = null, 
            Color? backgroundColor = null, 
            int? cullingMask = null, 
            bool? orthographic = null, 
            float? orthographicSize = null)
        {
            // Find the camera
            GameObject cameraObj = GameObject.Find(cameraName);
            if (cameraObj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Camera '{cameraName}' not found" }
                };
            }
            
            Camera camera = cameraObj.GetComponent<Camera>();
            if (camera == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{cameraName}' does not have a Camera component" }
                };
            }
            
            // Set properties if provided
            if (fieldOfView.HasValue)
            {
                camera.fieldOfView = fieldOfView.Value;
            }
            
            if (nearClipPlane.HasValue)
            {
                camera.nearClipPlane = nearClipPlane.Value;
            }
            
            if (farClipPlane.HasValue)
            {
                camera.farClipPlane = farClipPlane.Value;
            }
            
            if (clearFlags.HasValue)
            {
                camera.clearFlags = clearFlags.Value;
            }
            
            if (backgroundColor.HasValue)
            {
                camera.backgroundColor = backgroundColor.Value;
            }
            
            if (cullingMask.HasValue)
            {
                camera.cullingMask = cullingMask.Value;
            }
            
            if (orthographic.HasValue)
            {
                camera.orthographic = orthographic.Value;
            }
            
            if (orthographicSize.HasValue)
            {
                camera.orthographicSize = orthographicSize.Value;
            }
            
            return new Dictionary<string, object>
            {
                { "cameraName", cameraName },
                { "modified", true },
                { "camera", GetDetailedCameraInfo(camera) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> LookAt(string cameraName, string targetName)
        {
            // Find the camera
            GameObject cameraObj = GameObject.Find(cameraName);
            if (cameraObj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Camera '{cameraName}' not found" }
                };
            }
            
            // Find the target
            GameObject targetObj = GameObject.Find(targetName);
            if (targetObj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Target '{targetName}' not found" }
                };
            }
            
            // Make the camera look at the target
            cameraObj.transform.LookAt(targetObj.transform);
            
            return new Dictionary<string, object>
            {
                { "cameraName", cameraName },
                { "targetName", targetName },
                { "lookingAt", true },
                { "rotation", new Dictionary<string, float>
                    {
                        { "x", cameraObj.transform.eulerAngles.x },
                        { "y", cameraObj.transform.eulerAngles.y },
                        { "z", cameraObj.transform.eulerAngles.z }
                    }
                }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetCameraAsMain(string cameraName)
        {
            // Find the camera
            GameObject cameraObj = GameObject.Find(cameraName);
            if (cameraObj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Camera '{cameraName}' not found" }
                };
            }
            
            Camera camera = cameraObj.GetComponent<Camera>();
            if (camera == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{cameraName}' does not have a Camera component" }
                };
            }
            
            // Remove the tag from any existing main camera
            if (Camera.main != null && Camera.main != camera)
            {
                Camera.main.gameObject.tag = "Untagged";
            }
            
            // Set this camera as main
            cameraObj.tag = "MainCamera";
            
            return new Dictionary<string, object>
            {
                { "cameraName", cameraName },
                { "setAsMain", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreateCameraRig(
            string name = "CameraRig", 
            Vector3? position = null, 
            Vector3? rotation = null, 
            float fieldOfView = 60.0f)
        {
            // Create a rig with a pivot and camera
            GameObject rig = new GameObject(name);
            GameObject pivot = new GameObject("Pivot");
            GameObject cameraObj = new GameObject("Camera");
            
            // Set up hierarchy
            pivot.transform.SetParent(rig.transform);
            cameraObj.transform.SetParent(pivot.transform);
            
            // Add camera component
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.fieldOfView = fieldOfView;
            
            // Position the camera
            cameraObj.transform.localPosition = new Vector3(0, 0, -10); // 10 units back from pivot
            
            // Set rig transform
            if (position.HasValue)
            {
                rig.transform.position = position.Value;
            }
            
            if (rotation.HasValue)
            {
                rig.transform.eulerAngles = rotation.Value;
            }
            
            // Set as focused object in context
            _context.SetFocusedObject(rig);
            
            return new Dictionary<string, object>
            {
                { "created", true },
                { "rig", new Dictionary<string, object>
                    {
                        { "name", rig.name },
                        { "position", new Dictionary<string, float>
                            {
                                { "x", rig.transform.position.x },
                                { "y", rig.transform.position.y },
                                { "z", rig.transform.position.z }
                            }
                        },
                        { "rotation", new Dictionary<string, float>
                            {
                                { "x", rig.transform.eulerAngles.x },
                                { "y", rig.transform.eulerAngles.y },
                                { "z", rig.transform.eulerAngles.z }
                            }
                        }
                    }
                },
                { "camera", GetDetailedCameraInfo(camera) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreateOrbitCamera(
            string targetName, 
            string cameraName = "OrbitCamera", 
            float distance = 10.0f, 
            float height = 5.0f, 
            float fieldOfView = 60.0f)
        {
            // Find the target
            GameObject targetObj = GameObject.Find(targetName);
            if (targetObj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Target '{targetName}' not found" }
                };
            }
            
            // Create camera
            GameObject cameraObj = new GameObject(cameraName);
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.fieldOfView = fieldOfView;
            
            // Position the camera
            Vector3 targetPosition = targetObj.transform.position;
            cameraObj.transform.position = targetPosition + new Vector3(0, height, -distance);
            
            // Look at target
            cameraObj.transform.LookAt(targetObj.transform);
            
            // Add orbit script (placeholder - would need to implement this)
            // cameraObj.AddComponent<OrbitCamera>().target = targetObj.transform;
            
            // Set as focused object in context
            _context.SetFocusedObject(cameraObj);
            
            return new Dictionary<string, object>
            {
                { "created", true },
                { "targetName", targetName },
                { "camera", GetDetailedCameraInfo(camera) }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> TakeScreenshot(string filename = "Screenshot", int width = 1920, int height = 1080)
        {
            try
            {
                // Ensure directory exists
                string directory = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots");
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                // Generate filename with timestamp
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string path = System.IO.Path.Combine(directory, $"{filename}_{timestamp}.png");
                
                // Take screenshot
                ScreenCapture.CaptureScreenshot(path, 1);
                
                return new Dictionary<string, object>
                {
                    { "filename", path },
                    { "captured", true }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Failed to take screenshot: {e.Message}" }
                };
            }
        }
        
        private Dictionary<string, object> GetDetailedCameraInfo(Camera camera)
        {
            return new Dictionary<string, object>
            {
                { "name", camera.gameObject.name },
                { "isMainCamera", camera.gameObject.CompareTag("MainCamera") },
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
                },
                { "cullingMask", camera.cullingMask },
                { "orthographic", camera.orthographic },
                { "orthographicSize", camera.orthographicSize },
                { "position", new Dictionary<string, float>
                    {
                        { "x", camera.transform.position.x },
                        { "y", camera.transform.position.y },
                        { "z", camera.transform.position.z }
                    }
                },
                { "rotation", new Dictionary<string, float>
                    {
                        { "x", camera.transform.eulerAngles.x },
                        { "y", camera.transform.eulerAngles.y },
                        { "z", camera.transform.eulerAngles.z }
                    }
                }
            };
        }
    }
} 