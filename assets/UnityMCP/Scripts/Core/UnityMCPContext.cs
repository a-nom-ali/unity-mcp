using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityMCP
{
    /// <summary>
    /// Maintains the current context and state for UnityMCP operations
    /// </summary>
    public class UnityMCPContext
    {
        // Current selection and focus
        private GameObject _focusedObject;
        private List<GameObject> _selectedObjects = new List<GameObject>();
        
        // Context variables
        private Dictionary<string, object> _variables = new Dictionary<string, object>();
        
        // Session information
        private string _sessionId;
        private DateTime _sessionStartTime;
        
        // Project information
        private string _projectName;
        private string _unityVersion;
        
        public void Initialize()
        {
            _sessionId = Guid.NewGuid().ToString();
            _sessionStartTime = DateTime.Now;
            _unityVersion = Application.unityVersion;
            _projectName = Application.productName;
        }
        
        public GameObject GetFocusedObject()
        {
            return _focusedObject;
        }
        
        public void SetFocusedObject(GameObject obj)
        {
            _focusedObject = obj;
        }
        
        public List<GameObject> GetSelectedObjects()
        {
            return _selectedObjects;
        }
        
        public void SetSelectedObjects(List<GameObject> objects)
        {
            _selectedObjects = objects ?? new List<GameObject>();
        }
        
        public void AddSelectedObject(GameObject obj)
        {
            if (obj != null && !_selectedObjects.Contains(obj))
            {
                _selectedObjects.Add(obj);
            }
        }
        
        public void RemoveSelectedObject(GameObject obj)
        {
            if (obj != null)
            {
                _selectedObjects.Remove(obj);
            }
        }
        
        public void ClearSelectedObjects()
        {
            _selectedObjects.Clear();
        }
        
        public void SetVariable(string key, object value)
        {
            _variables[key] = value;
        }
        
        public T GetVariable<T>(string key, T defaultValue = default)
        {
            if (_variables.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            
            return defaultValue;
        }
        
        public bool HasVariable(string key)
        {
            return _variables.ContainsKey(key);
        }
        
        public void RemoveVariable(string key)
        {
            if (_variables.ContainsKey(key))
            {
                _variables.Remove(key);
            }
        }
        
        public string GetSessionId()
        {
            return _sessionId;
        }
        
        public DateTime GetSessionStartTime()
        {
            return _sessionStartTime;
        }
        
        public TimeSpan GetSessionDuration()
        {
            return DateTime.Now - _sessionStartTime;
        }
        
        public string GetProjectName()
        {
            return _projectName;
        }
        
        public string GetUnityVersion()
        {
            return _unityVersion;
        }
        
        public Dictionary<string, object> GetContextSnapshot()
        {
            return new Dictionary<string, object>
            {
                { "sessionId", _sessionId },
                { "sessionStartTime", _sessionStartTime },
                { "sessionDuration", GetSessionDuration().ToString() },
                { "projectName", _projectName },
                { "unityVersion", _unityVersion },
                { "focusedObject", _focusedObject?.name },
                { "selectedObjectCount", _selectedObjects.Count },
                { "variableCount", _variables.Count }
            };
        }
    }
} 