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
    [ExecuteInEditMode]
    public class UnityMCPBrain : MonoBehaviour
    {
        private static UnityMCPBrain _instance;
        
        public static UnityMCPBrain Instance
        {
            get
            {
                _instance = FindFirstObjectByType<UnityMCPBrain>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("UnityMCPBrain");
                    _instance = go.AddComponent<UnityMCPBrain>();
                    if (Application.isPlaying)
                        DontDestroyOnLoad(go);
                }

                if (_instance._commandHandlers.Count == 0 || _instance._activeSubsystems.Count == 0)
                    _instance.Initialize();
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
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
            
            // Initialize the brain
            Initialize();
        }

        private void Initialize()
        {
            if (!Uninitialized) return;
            
            Log("Initializing UnityMCP Brain...", LogLevel.Info);
         
            // Initialize command handlers
            InitializeCommandHandlers();
            
            // Initialize context
            _context.Initialize();
            
            _initialized = true;
            Log("UnityMCP Brain initialized successfully", LogLevel.Info);
            
            // Fire initialization event
            RaiseEvent("brain.initialized", null);
        }

        private void InitializeCommandHandlers()
        {
            _commandHandlers = new Dictionary<string, CommandHandler>(StringComparer.OrdinalIgnoreCase);
            
            // Add core command handler
            var coreHandler = new CoreCommandHandler();
            coreHandler.SetContext(_context);
            _commandHandlers["core"] = coreHandler;
            
            // Add other handlers as needed
            Debug.Log("UnityMCPBrain initialized");
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
            
            Debug.Log($"Initialized {_commandHandlers.Count} command handlers");
        }
        
        public string ExecuteCommand(string commandJson)
        {
            if (Uninitialized)
                Initialize();
            try
            {
                Debug.Log($"Executing command: {commandJson}");
                
                // Parse the command
                CommandData command = JsonUtility.FromJson<CommandData>(commandJson);
                
                if (command == null)
                {
                    return JsonUtility.CreateErrorResponse("Invalid command format");
                }
                
                // Extract the command category and action
                string category = "core"; // Default to core
                string action = command.type;
                
                // Check if the command has a category prefix (category.action)
                if (action.Contains("."))
                {
                    var parts = action.Split(new[] { '.' }, 2);
                    category = parts[0].ToLower();
                    action = parts[1];
                }
                
                // Find the appropriate handler
                if (_commandHandlers.TryGetValue(category, out var handler))
                {
                    return handler.ExecuteCommand(action, command.parameters);
                }
                else
                {
                    return JsonUtility.CreateErrorResponse($"Unknown command category: {category}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing command: {e.Message}\n{e.StackTrace}");
                return JsonUtility.CreateErrorResponse($"Error executing command: {e.Message}");
            }
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

        public string ExecuteCommand(string subsystem, string action, string parameters)
        {
            if (Uninitialized)
                Initialize();
            try
            {
                Log($"Executing command: {subsystem}.{action}", LogLevel.Debug);
            
                // Find the appropriate handler
                if (_commandHandlers.TryGetValue(subsystem, out var handler))
                {
                    string result = handler.ExecuteCommand(action, parameters);
                
                    // Record in history
                    CommandData command = new CommandData
                    {
                        type = $"{subsystem}.{action}",
                        parameters = parameters
                    };
                    _history.RecordCommand(command, result);
                
                    // Raise event
                    RaiseEvent("command.executed", new { Command = command, Result = result });
                
                    return result;
                }
                else
                {
                    return JsonUtility.CreateErrorResponse($"Unknown command category: {subsystem}");
                }
            }
            catch (Exception e)
            {
                LogError($"Error executing command {subsystem}.{action}: {e.Message}");
                return JsonUtility.CreateErrorResponse($"Error executing command: {e.Message}");
            }
        }

        public bool Uninitialized => !_initialized || _instance._commandHandlers.Count == 0 ||
                                     _instance._activeSubsystems.Count == 0;

        // Ensure we have a method to get all subsystems
        public List<IUnityMCPSubsystem> GetSubsystems()
        {
            return new List<IUnityMCPSubsystem>(_subsystems.Values);
        }

        // Add this method to get a subsystem by name
        public IUnityMCPSubsystem GetSubsystemByName(string subsystemName)
        {
            foreach (var subsystem in _subsystems.Values)
            {
                if (subsystem.GetType().Name.ToLower() == subsystemName.ToLower() ||
                    subsystem.GetType().Name.ToLower() == $"{subsystemName}subsystem".ToLower())
                {
                    return subsystem;
                }
            }
        
            return null;
        }

        public string ExecuteCommand(CommandData command)
        {
            if (Uninitialized)
                Initialize();
            
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