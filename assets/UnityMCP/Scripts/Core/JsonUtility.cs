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

        public static T FromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deserializing JSON: {e.Message}");
                throw;
            }
        }

        public static string ToJson(object obj)
        {
            if (obj == null)
                return "{}";

            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error serializing to JSON: {e.Message}");
                throw;
            }
        }

        public static string CreateSuccessResponse(object result)
        {
            var response = new Dictionary<string, object>
            {
                { "status", "success" },
                { "result", result }
            };

            return ToJson(response);
        }

        public static string CreateErrorResponse(string message)
        {
            var response = new Dictionary<string, object>
            {
                { "status", "error" },
                { "message", message }
            };

            return ToJson(response);
        }
    }
} 