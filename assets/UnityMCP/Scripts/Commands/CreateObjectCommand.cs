using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityMCP.Commands
{
    public class CreateObjectCommand
    {
        public static GameObject Execute(CreateObjectParams parameters)
        {
            // Default values
            string type = parameters.type ?? "Cube";
            string name = parameters.name ?? type;
            Vector3 position = parameters.position ?? Vector3.zero;
            Vector3 rotation = parameters.rotation ?? Vector3.zero;
            Vector3 scale = parameters.scale ?? Vector3.one;
            
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
                case "light":
                    newObject = new GameObject();
                    Light light = newObject.AddComponent<Light>();
                    light.type = LightType.Point;
                    break;
                case "camera":
                    newObject = new GameObject();
                    newObject.AddComponent<Camera>();
                    break;
                default:
                    throw new ArgumentException($"Unknown object type: {type}");
            }
            
            // Set properties
            newObject.name = name;
            newObject.transform.position = position;
            newObject.transform.eulerAngles = rotation;
            newObject.transform.localScale = scale;
            
            return newObject;
        }
    }
} 