using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP.Subsystems
{
    /// <summary>
    /// Subsystem for managing animations
    /// </summary>
    public class AnimationSubsystem : MonoBehaviour, IUnityMCPSubsystem, ICommandProvider
    {
        private UnityMCPBrain _brain;
        private bool _initialized = false;
        private AnimationCommandHandler _commandHandler;
        
        public void Initialize(UnityMCPBrain brain)
        {
            if (_initialized) return;
            
            _brain = brain;
            _commandHandler = new AnimationCommandHandler();
            
            _initialized = true;
            _brain.LogInfo("Animation subsystem initialized");
        }
        
        public void Shutdown()
        {
            _initialized = false;
            _brain.LogInfo("Animation subsystem shut down");
        }
        
        public string GetName()
        {
            return "Animation";
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
                { "animation", _commandHandler }
            };
        }
    }
    
    /// <summary>
    /// Command handler for animation operations
    /// </summary>
    public class AnimationCommandHandler : CommandHandler
    {
        [CommandMethod]
        public Dictionary<string, object> StopAnimation(string objectName)
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' not found" }
                };
            }
            
            // Try with Animation component
            Animation animation = obj.GetComponent<Animation>();
            if (animation != null)
            {
                animation.Stop();
                
                return new Dictionary<string, object>
                {
                    { "objectName", objectName },
                    { "stopped", true },
                    { "type", "Legacy" }
                };
            }
            
            // Try with Animator component
            Animator animator = obj.GetComponent<Animator>();
            if (animator != null)
            {
                // Reset all triggers
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Trigger)
                    {
                        animator.ResetTrigger(param.name);
                    }
                }
                
                // Set speed to 0 to "stop" the animation
                animator.speed = 0;
                
                return new Dictionary<string, object>
                {
                    { "objectName", objectName },
                    { "stopped", true },
                    { "type", "Mecanim" }
                };
            }
            
            return new Dictionary<string, object>
            {
                { "error", $"Object '{objectName}' does not have Animation or Animator component" }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> SetAnimationParameter(string objectName, string parameterName, object value)
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' not found" }
                };
            }
            
            Animator animator = obj.GetComponent<Animator>();
            if (animator == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' does not have an Animator component" }
                };
            }
            
            // Find the parameter
            AnimatorControllerParameter param = null;
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == parameterName)
                {
                    param = p;
                    break;
                }
            }
            
            if (param == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Parameter '{parameterName}' not found on animator" }
                };
            }
            
            // Set the parameter based on its type
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    float floatValue = Convert.ToSingle(value);
                    animator.SetFloat(parameterName, floatValue);
                    return new Dictionary<string, object>
                    {
                        { "objectName", objectName },
                        { "parameterName", parameterName },
                        { "type", "Float" },
                        { "value", floatValue },
                        { "set", true }
                    };
                
                case AnimatorControllerParameterType.Int:
                    int intValue = Convert.ToInt32(value);
                    animator.SetInteger(parameterName, intValue);
                    return new Dictionary<string, object>
                    {
                        { "objectName", objectName },
                        { "parameterName", parameterName },
                        { "type", "Int" },
                        { "value", intValue },
                        { "set", true }
                    };
                
                case AnimatorControllerParameterType.Bool:
                    bool boolValue = Convert.ToBoolean(value);
                    animator.SetBool(parameterName, boolValue);
                    return new Dictionary<string, object>
                    {
                        { "objectName", objectName },
                        { "parameterName", parameterName },
                        { "type", "Bool" },
                        { "value", boolValue },
                        { "set", true }
                    };
                
                case AnimatorControllerParameterType.Trigger:
                    bool triggerValue = Convert.ToBoolean(value);
                    if (triggerValue)
                    {
                        animator.SetTrigger(parameterName);
                    }
                    else
                    {
                        animator.ResetTrigger(parameterName);
                    }
                    return new Dictionary<string, object>
                    {
                        { "objectName", objectName },
                        { "parameterName", parameterName },
                        { "type", "Trigger" },
                        { "value", triggerValue },
                        { "set", true }
                    };
                
                default:
                    return new Dictionary<string, object>
                    {
                        { "error", $"Unsupported parameter type: {param.type}" }
                    };
            }
        }
        
        [CommandMethod]
        public Dictionary<string, object> CreateAnimationClip(string name, float length = 1.0f)
        {
            #if UNITY_EDITOR
            // Create a new animation clip
            AnimationClip clip = new AnimationClip();
            clip.name = name;
            clip.legacy = true; // Set to legacy for Animation component
            
            // Save the clip as an asset
            string path = $"Assets/Animations/{name}.anim";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            UnityEditor.AssetDatabase.CreateAsset(clip, path);
            UnityEditor.AssetDatabase.SaveAssets();
            
            return new Dictionary<string, object>
            {
                { "name", name },
                { "path", path },
                { "length", length },
                { "created", true }
            };
            #else
            return new Dictionary<string, object>
            {
                { "error", "Creating animation clips at runtime is not supported" }
            };
            #endif
        }
        
        [CommandMethod]
        public Dictionary<string, object> AddAnimationCurve(string clipName, string objectName, string propertyPath, List<Vector2> keyframes)
        {
            #if UNITY_EDITOR
            // Find the animation clip
            string clipPath = $"Assets/Animations/{clipName}.anim";
            AnimationClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Animation clip '{clipName}' not found" }
                };
            }
            
            // Create a curve
            AnimationCurve curve = new AnimationCurve();
            
            // Add keyframes
            foreach (var keyframe in keyframes)
            {
                curve.AddKey(keyframe.x, keyframe.y);
            }
            
            // Set the curve in the clip
            clip.SetCurve(objectName, typeof(Transform), propertyPath, curve);
            
            // Save the changes
            UnityEditor.EditorUtility.SetDirty(clip);
            UnityEditor.AssetDatabase.SaveAssets();
            
            return new Dictionary<string, object>
            {
                { "clipName", clipName },
                { "objectName", objectName },
                { "propertyPath", propertyPath },
                { "keyframeCount", keyframes.Count },
                { "added", true }
            };
            #else
            return new Dictionary<string, object>
            {
                { "error", "Adding animation curves at runtime is not supported" }
            };
            #endif
        }
        
        [CommandMethod]
        public Dictionary<string, object> AddAnimationToObject(string objectName, string clipName)
        {
            #if UNITY_EDITOR
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Object '{objectName}' not found" }
                };
            }
            
            // Find the animation clip
            string clipPath = $"Assets/Animations/{clipName}.anim";
            AnimationClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"Animation clip '{clipName}' not found" }
                };
            }
            
            // Get or add Animation component
            Animation animation = obj.GetComponent<Animation>();
            if (animation == null)
            {
                animation = obj.AddComponent<Animation>();
            }
            
            // Add the clip to the animation
            animation.AddClip(clip, clipName);
            
            return new Dictionary<string, object>
            {
                { "objectName", objectName },
                { "clipName", clipName },
                { "added", true }
            };
            #else
            return new Dictionary<string, object>
            {
                { "error", "Adding animation clips at runtime is not supported" }
            };
            #endif
        }
    }
} 