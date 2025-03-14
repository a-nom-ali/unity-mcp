using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityMCP
{
    /// <summary>
    /// The central intelligence system for UnityMCP that coordinates all subsystems
    /// and provides high-level reasoning about Unity operations.
    /// </summary>
    public class UnityMCPBrain : MonoBehaviour
    {
        private static UnityMCPBrain _instance;
        
        public static UnityMCPBrain Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("UnityMCPBrain");
                    _instance = go.AddComponent<UnityMCPBrain>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("System Status")]
        [SerializeField] private bool _initialized = false;
        [SerializeField] private List<string> _activeSubsystems = new List<string>();
        
        [Header("Configuration")]
        [SerializeField] private bool _autoInitializeSubsystems = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private LogLevel _logLevel = LogLevel.Info;
        
        // Subsystems
        private Dictionary<Type, IUnityMCPSubsystem> _subsystems = new Dictionary<Type, IUnityMCPSubsystem>();
        private Dictionary<string, CommandHandler> _commandHandlers = new Dictionary<string, CommandHandler>();
        
        // Context and state tracking
        private UnityMCPContext _context = new UnityMCPContext();
        private UnityMCPHistory _history = new UnityMCPHistory();
        
        // Event system
        public event Action<string, object> OnSystemEvent;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize the brain
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;
            
            Log("Initializing UnityMCP Brain...", LogLevel.Info);
            
            // Register core command handlers
            RegisterCommandHandler("core", new CoreCommandHandler());
            RegisterCommandHandler("scene", new SceneCommandHandler());
            RegisterCommandHandler("object", new ObjectCommandHandler());
            RegisterCommandHandler("material", new MaterialCommandHandler());
            
            // Discover and initialize subsystems if auto-initialize is enabled
            if (_autoInitializeSubsystems)
            {
                DiscoverAndInitializeSubsystems();
            }
            
            // Initialize context
            _context.Initialize();
            
            _initialized = true;
            Log("UnityMCP Brain initialized successfully", LogLevel.Info);
            
            // Fire initialization event
            RaiseEvent("brain.initialized", null);
        }

        private void DiscoverAndInitializeSubsystems()
        {
            Log("Discovering subsystems...", LogLevel.Debug);
            
            // Find all classes that implement IUnityMCPSubsystem
            var subsystemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IUnityMCPSubsystem).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();
            
            Log($"Found {subsystemTypes.Count} subsystem types", LogLevel.Debug);
            
            // Initialize each subsystem
            foreach (var subsystemType in subsystemTypes)
            {
                try
                {
                    // Create instance
                    var subsystem = (IUnityMCPSubsystem)Activator.CreateInstance(subsystemType);
                    
                    // Initialize the subsystem
                    subsystem.Initialize(this);
                    
                    // Register the subsystem
                    _subsystems[subsystemType] = subsystem;
                    _activeSubsystems.Add(subsystemType.Name);
                    
                    Log($"Initialized subsystem: {subsystemType.Name}", LogLevel.Info);
                    
                    // Register command handlers from the subsystem
                    if (subsystem is ICommandProvider commandProvider)
                    {
                        var handlers = commandProvider.GetCommandHandlers();
                        foreach (var handler in handlers)
                        {
                            RegisterCommandHandler(handler.Key, handler.Value);
                        }
                    }
                }
                catch (Exception e)
                {
                    LogError($"Failed to initialize subsystem {subsystemType.Name}: {e.Message}");
                }
            }
        }

        public void RegisterCommandHandler(string domain, CommandHandler handler)
        {
            if (_commandHandlers.ContainsKey(domain))
            {
                LogWarning($"Command handler for domain '{domain}' already registered. Overwriting.");
            }
            
            _commandHandlers[domain] = handler;
            handler.SetContext(_context);
            Log($"Registered command handler for domain: {domain}", LogLevel.Debug);
        }

        public string ExecuteCommand(CommandData command)
        {
            if (!_initialized)
            {
                return JsonUtility.CreateErrorResponse("UnityMCP Brain not initialized");
            }
            
            Log($"Executing command: {command.type}", LogLevel.Debug);
            
            try
            {
                // Parse the command type to get domain and action
                string[] parts = command.type.Split('.');
                if (parts.Length != 2)
                {
                    return JsonUtility.CreateErrorResponse($"Invalid command type format: {command.type}. Expected format: domain.action");
                }
                
                string domain = parts[0];
                string action = parts[1];
                
                // Find the appropriate command handler
                if (!_commandHandlers.TryGetValue(domain, out var handler))
                {
                    return JsonUtility.CreateErrorResponse($"No command handler registered for domain: {domain}");
                }
                
                // Execute the command
                string result = handler.ExecuteCommand(action, command.parameters);
                
                // Record in history
                _history.RecordCommand(command, result);
                
                // Raise event
                RaiseEvent("command.executed", new { Command = command, Result = result });
                
                return result;
            }
            catch (Exception e)
            {
                LogError($"Error executing command {command.type}: {e.Message}");
                return JsonUtility.CreateErrorResponse($"Error executing command: {e.Message}");
            }
        }

        public T GetSubsystem<T>() where T : class, IUnityMCPSubsystem
        {
            if (_subsystems.TryGetValue(typeof(T), out var subsystem))
            {
                return subsystem as T;
            }
            
            return null;
        }

        public IUnityMCPSubsystem GetSubsystem(Type subsystemType)
        {
            if (_subsystems.TryGetValue(subsystemType, out var subsystem))
            {
                return subsystem;
            }
    
            return null;
        }
        
        public UnityMCPContext GetContext()
        {
            return _context;
        }

        public UnityMCPHistory GetHistory()
        {
            return _history;
        }

        public void RaiseEvent(string eventName, object data)
        {
            OnSystemEvent?.Invoke(eventName, data);
            Log($"Event raised: {eventName}", LogLevel.Debug);
        }

        public void Log(string message, LogLevel level)
        {
            if (!_enableLogging || level < _logLevel) return;
            
            switch (level)
            {
                case LogLevel.Debug:
                    Debug.Log($"[UnityMCP] {message}");
                    break;
                case LogLevel.Info:
                    Debug.Log($"[UnityMCP] {message}");
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning($"[UnityMCP] {message}");
                    break;
                case LogLevel.Error:
                    Debug.LogError($"[UnityMCP] {message}");
                    break;
            }
        }

        public virtual void LogInfo(string message) => Log(message, LogLevel.Info);
        public virtual void LogWarning(string message) => Log(message, LogLevel.Warning);
        public virtual void LogError(string message) => Log(message, LogLevel.Error);
        public virtual void LogDebug(string message) => Log(message, LogLevel.Debug);
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
} 