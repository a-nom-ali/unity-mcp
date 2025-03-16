using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMCP
{
    public static class JsonUtility
    {
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                JToken.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string CreateSuccessResponse(object data)
        {
            var response = new Dictionary<string, object>
            {
                { "success", true },
                { "data", data }
            };
            
            return JsonConvert.SerializeObject(response);
        }
        
        public static string CreateErrorResponse(string message)
        {
            var response = new Dictionary<string, object>
            {
                { "success", false },
                { "error", message }
            };
            
            return JsonConvert.SerializeObject(response);
        }
        
        public static T FromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
            
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deserializing JSON: {e.Message}\nJSON: {json}");
                return default;
            }
        }
        
        public static string ToJson(object obj)
        {
            if (obj == null)
            {
                return "{}";
            }
            
            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error serializing object to JSON: {e.Message}");
                return "{}";
            }
        }
        
        public static Dictionary<string, object> FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new Dictionary<string, object>();
            }
            
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deserializing JSON to dictionary: {e.Message}\nJSON: {json}");
                return new Dictionary<string, object>();
            }
        }
    }
}