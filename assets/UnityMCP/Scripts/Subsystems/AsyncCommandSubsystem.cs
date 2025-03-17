using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UnityMCP.Subsystems
{
    /// <summary>
    /// Subsystem for handling asynchronous command execution
    /// </summary>
    public class AsyncCommandSubsystem : MonoBehaviour, IUnityMCPSubsystem, ICommandProvider
    {
        private UnityMCPBrain _brain;
        private bool _initialized = false;
        private AsyncCommandHandler _commandHandler;
        
        // Dictionary to track running tasks
        private Dictionary<string, AsyncOperation> _runningOperations = new Dictionary<string, AsyncOperation>();
        
        public void Initialize(UnityMCPBrain brain)
        {
            if (_initialized) return;
            
            _brain = brain;
            _commandHandler = new AsyncCommandHandler(this);
            
            _initialized = true;
            _brain.LogInfo("Async Command subsystem initialized");
            
            // Start the cleanup coroutine to remove completed operations
            StartCoroutine(CleanupCompletedOperations());
        }
        
        public void Shutdown()
        {
            // Cancel all running operations
            foreach (var operation in _runningOperations.Values)
            {
                operation.Cancel();
            }
            
            _runningOperations.Clear();
            _initialized = false;
            _brain.LogInfo("Async Command subsystem shut down");
        }
        
        public string GetName()
        {
            return "AsyncCommand";
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
                { "async", _commandHandler }
            };
        }
        
        /// <summary>
        /// Register a new async operation
        /// </summary>
        public string RegisterOperation(string commandType, Dictionary<string, object> parameters)
        {
            string operationId = Guid.NewGuid().ToString();
            var operation = new AsyncOperation(operationId, commandType, parameters);
            
            _runningOperations[operationId] = operation;
            
            // Start the operation
            operation.Start();
            
            return operationId;
        }
        
        /// <summary>
        /// Get the status of an async operation
        /// </summary>
        public AsyncOperation GetOperation(string operationId)
        {
            if (_runningOperations.TryGetValue(operationId, out var operation))
            {
                return operation;
            }
            
            return null;
        }
        
        /// <summary>
        /// Cancel an async operation
        /// </summary>
        public bool CancelOperation(string operationId)
        {
            if (_runningOperations.TryGetValue(operationId, out var operation))
            {
                operation.Cancel();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get all running operations
        /// </summary>
        public Dictionary<string, AsyncOperation> GetAllOperations()
        {
            return new Dictionary<string, AsyncOperation>(_runningOperations);
        }
        
        /// <summary>
        /// Coroutine to clean up completed operations
        /// </summary>
        private IEnumerator CleanupCompletedOperations()
        {
            while (true)
            {
                // Wait for 30 seconds
                yield return new WaitForSeconds(30);
                
                // Find completed operations that are older than 10 minutes
                var keysToRemove = new List<string>();
                var cutoffTime = DateTime.Now.AddMinutes(-10);
                
                foreach (var kvp in _runningOperations)
                {
                    if (kvp.Value.Status == AsyncOperationStatus.Completed && 
                        kvp.Value.CompletionTime < cutoffTime)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                
                // Remove old completed operations
                foreach (var key in keysToRemove)
                {
                    _runningOperations.Remove(key);
                }
                
                if (keysToRemove.Count > 0)
                {
                    Debug.Log($"Cleaned up {keysToRemove.Count} completed async operations");
                }
            }
        }
    }
    
    /// <summary>
    /// Status of an async operation
    /// </summary>
    public enum AsyncOperationStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }
    
    /// <summary>
    /// Class representing an asynchronous operation
    /// </summary>
    public class AsyncOperation
    {
        public string Id { get; private set; }
        public string CommandType { get; private set; }
        public Dictionary<string, object> Parameters { get; private set; }
        public AsyncOperationStatus Status { get; private set; } = AsyncOperationStatus.Pending;
        public DateTime CreationTime { get; private set; }
        public DateTime? StartTime { get; private set; }
        public DateTime? CompletionTime { get; private set; }
        public object Result { get; private set; }
        public string Error { get; private set; }
        public float Progress { get; private set; }
        
        private CancellationTokenSource _cancellationTokenSource;
        
        public AsyncOperation(string id, string commandType, Dictionary<string, object> parameters)
        {
            Id = id;
            CommandType = commandType;
            Parameters = parameters;
            CreationTime = DateTime.Now;
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Start the async operation
        /// </summary>
        public void Start()
        {
            if (Status != AsyncOperationStatus.Pending)
            {
                return;
            }
            
            Status = AsyncOperationStatus.Running;
            StartTime = DateTime.Now;
            Progress = 0;
            
            // Start the operation on a background thread
            Task.Run(() => ExecuteAsync(_cancellationTokenSource.Token));
        }
        
        /// <summary>
        /// Cancel the operation
        /// </summary>
        public void Cancel()
        {
            if (Status == AsyncOperationStatus.Running || Status == AsyncOperationStatus.Pending)
            {
                _cancellationTokenSource.Cancel();
                Status = AsyncOperationStatus.Cancelled;
                CompletionTime = DateTime.Now;
            }
        }
        
        /// <summary>
        /// Execute the command asynchronously
        /// </summary>
        private async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Check if we should cancel
                if (cancellationToken.IsCancellationRequested)
                {
                    Status = AsyncOperationStatus.Cancelled;
                    CompletionTime = DateTime.Now;
                    return;
                }
                
                // Extract subsystem and action from command type
                string subsystem = "core"; // Default to core
                string action = CommandType;
                
                if (CommandType.Contains("."))
                {
                    var parts = CommandType.Split(new[] { '.' }, 2);
                    subsystem = parts[0].ToLower();
                    action = parts[1];
                }
                
                // Convert parameters to JSON string
                string paramsJson = JsonConvert.SerializeObject(Parameters);
                
                // Update progress
                Progress = 0.1f;
                
                // Simulate some work for the demo (remove in production)
                for (int i = 1; i <= 10; i++)
                {
                    // Check if we should cancel
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Status = AsyncOperationStatus.Cancelled;
                        CompletionTime = DateTime.Now;
                        return;
                    }
                    
                    // Update progress
                    Progress = i / 10.0f;
                    
                    // Simulate work
                    await Task.Delay(500, cancellationToken);
                }
                
                // Execute the command on the main thread
                string resultJson = null;
                
                // Use UnityThreadDispatcher to run on the main thread
                UnityThreadDispatcher.Instance().Enqueue(() =>
                {
                    resultJson = UnityMCPBrain.Instance.ExecuteCommand(subsystem, action, paramsJson);
                });
                
                // Wait for the result (with timeout)
                int timeoutMs = 30000; // 30 seconds
                int waitInterval = 100; // 100ms
                int totalWaitTime = 0;
                
                while (string.IsNullOrEmpty(resultJson) && totalWaitTime < timeoutMs)
                {
                    // Check if we should cancel
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Status = AsyncOperationStatus.Cancelled;
                        CompletionTime = DateTime.Now;
                        return;
                    }
                    
                    await Task.Delay(waitInterval, cancellationToken);
                    totalWaitTime += waitInterval;
                }
                
                // Check if we timed out
                if (string.IsNullOrEmpty(resultJson))
                {
                    throw new TimeoutException("Command execution timed out");
                }
                
                // Parse the result
                try
                {
                    Result = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultJson);
                }
                catch
                {
                    // If parsing fails, store the raw result
                    Result = resultJson;
                }
                
                // Update status
                Status = AsyncOperationStatus.Completed;
                CompletionTime = DateTime.Now;
                Progress = 1.0f;
            }
            catch (Exception ex)
            {
                // Handle errors
                Status = AsyncOperationStatus.Failed;
                Error = ex.Message;
                CompletionTime = DateTime.Now;
                Debug.LogError($"Async operation {Id} failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Convert to a dictionary for serialization
        /// </summary>
        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                { "id", Id },
                { "commandType", CommandType },
                { "parameters", Parameters },
                { "status", Status.ToString() },
                { "creationTime", CreationTime.ToString("yyyy-MM-dd HH:mm:ss") },
                { "startTime", StartTime?.ToString("yyyy-MM-dd HH:mm:ss") },
                { "completionTime", CompletionTime?.ToString("yyyy-MM-dd HH:mm:ss") },
                { "result", Result },
                { "error", Error },
                { "progress", Progress },
                { "runTime", StartTime.HasValue ? (CompletionTime ?? DateTime.Now) - StartTime.Value : TimeSpan.Zero }
            };
        }
    }
    
    /// <summary>
    /// Command handler for async operations
    /// </summary>
    public class AsyncCommandHandler : CommandHandler
    {
        private AsyncCommandSubsystem _subsystem;
        
        public AsyncCommandHandler(AsyncCommandSubsystem subsystem)
        {
            _subsystem = subsystem;
        }
        
        [CommandMethod]
        public Dictionary<string, object> Execute(string commandType, Dictionary<string, object> parameters = null)
        {
            // Validate input
            if (string.IsNullOrEmpty(commandType))
            {
                return new Dictionary<string, object>
                {
                    { "error", "Command type is required" },
                    { "success", false }
                };
            }
            
            // Default to empty parameters if none provided
            parameters = parameters ?? new Dictionary<string, object>();
            
            // Register the async operation
            string operationId = _subsystem.RegisterOperation(commandType, parameters);
            
            // Return the operation ID
            return new Dictionary<string, object>
            {
                { "operationId", operationId },
                { "status", "pending" },
                { "message", $"Async operation started for command: {commandType}" },
                { "success", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetStatus(string operationId)
        {
            // Validate input
            if (string.IsNullOrEmpty(operationId))
            {
                return new Dictionary<string, object>
                {
                    { "error", "Operation ID is required" },
                    { "success", false }
                };
            }
            
            // Get the operation
            var operation = _subsystem.GetOperation(operationId);
            
            if (operation == null)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"No operation found with ID: {operationId}" },
                    { "success", false }
                };
            }
            
            // Return the operation status
            return new Dictionary<string, object>
            {
                { "operation", operation.ToDict() },
                { "success", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> Cancel(string operationId)
        {
            // Validate input
            if (string.IsNullOrEmpty(operationId))
            {
                return new Dictionary<string, object>
                {
                    { "error", "Operation ID is required" },
                    { "success", false }
                };
            }
            
            // Cancel the operation
            bool cancelled = _subsystem.CancelOperation(operationId);
            
            if (!cancelled)
            {
                return new Dictionary<string, object>
                {
                    { "error", $"No running operation found with ID: {operationId}" },
                    { "success", false }
                };
            }
            
            // Return success
            return new Dictionary<string, object>
            {
                { "operationId", operationId },
                { "message", "Operation cancelled successfully" },
                { "success", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> ListOperations(string status = null)
        {
            // Get all operations
            var operations = _subsystem.GetAllOperations();
            var filteredOperations = new List<Dictionary<string, object>>();
            
            // Filter by status if provided
            AsyncOperationStatus? filterStatus = null;
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<AsyncOperationStatus>(status, true, out var parsedStatus))
                {
                    filterStatus = parsedStatus;
                }
                else
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"Invalid status filter: {status}" },
                        { "success", false }
                    };
                }
            }
            
            // Convert operations to dictionaries
            foreach (var operation in operations.Values)
            {
                if (!filterStatus.HasValue || operation.Status == filterStatus.Value)
                {
                    filteredOperations.Add(operation.ToDict());
                }
            }
            
            // Return the list of operations
            return new Dictionary<string, object>
            {
                { "operations", filteredOperations },
                { "count", filteredOperations.Count },
                { "success", true }
            };
        }
    }
}
