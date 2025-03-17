using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Subsystems
{
    /// <summary>
    /// Subsystem for handling batch operations and command optimization
    /// </summary>
    public class BatchSubsystem : MonoBehaviour, IUnityMCPSubsystem, ICommandProvider
    {
        private UnityMCPBrain _brain;
        private bool _initialized = false;
        private BatchCommandHandler _commandHandler;
        
        // Performance metrics
        private Dictionary<string, CommandMetrics> _commandMetrics = new Dictionary<string, CommandMetrics>();
        
        public void Initialize(UnityMCPBrain brain)
        {
            if (_initialized) return;
            
            _brain = brain;
            _commandHandler = new BatchCommandHandler(this);
            
            _initialized = true;
            _brain.LogInfo("Batch subsystem initialized");
        }
        
        public void Shutdown()
        {
            _initialized = false;
            _brain.LogInfo("Batch subsystem shut down");
        }
        
        public string GetName()
        {
            return "Batch";
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
                { "batch", _commandHandler }
            };
        }
        
        /// <summary>
        /// Record performance metrics for a command
        /// </summary>
        public void RecordCommandMetrics(string commandType, float executionTime)
        {
            if (!_commandMetrics.ContainsKey(commandType))
            {
                _commandMetrics[commandType] = new CommandMetrics();
            }
            
            _commandMetrics[commandType].RecordExecution(executionTime);
        }
        
        /// <summary>
        /// Get performance metrics for a specific command type
        /// </summary>
        public CommandMetrics GetCommandMetrics(string commandType)
        {
            if (_commandMetrics.TryGetValue(commandType, out var metrics))
            {
                return metrics;
            }
            
            return null;
        }
        
        /// <summary>
        /// Get performance metrics for all commands
        /// </summary>
        public Dictionary<string, CommandMetrics> GetAllCommandMetrics()
        {
            return _commandMetrics;
        }
    }
    
    /// <summary>
    /// Class to track performance metrics for commands
    /// </summary>
    public class CommandMetrics
    {
        public int ExecutionCount { get; private set; }
        public float TotalExecutionTime { get; private set; }
        public float AverageExecutionTime => ExecutionCount > 0 ? TotalExecutionTime / ExecutionCount : 0;
        public float MinExecutionTime { get; private set; } = float.MaxValue;
        public float MaxExecutionTime { get; private set; } = 0;
        
        public void RecordExecution(float executionTime)
        {
            ExecutionCount++;
            TotalExecutionTime += executionTime;
            
            if (executionTime < MinExecutionTime)
            {
                MinExecutionTime = executionTime;
            }
            
            if (executionTime > MaxExecutionTime)
            {
                MaxExecutionTime = executionTime;
            }
        }
        
        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                { "executionCount", ExecutionCount },
                { "totalExecutionTime", TotalExecutionTime },
                { "averageExecutionTime", AverageExecutionTime },
                { "minExecutionTime", MinExecutionTime },
                { "maxExecutionTime", MaxExecutionTime }
            };
        }
    }
    
    /// <summary>
    /// Command handler for batch operations
    /// </summary>
    public class BatchCommandHandler : CommandHandler
    {
        private BatchSubsystem _subsystem;
        
        public BatchCommandHandler(BatchSubsystem subsystem)
        {
            _subsystem = subsystem;
        }
        
        [CommandMethod]
        public Dictionary<string, object> Execute(List<Dictionary<string, object>> commands)
        {
            // Start timing the batch execution
            var startTime = Time.realtimeSinceStartup;
            
            // Validate input
            if (commands == null || commands.Count == 0)
            {
                return new Dictionary<string, object>
                {
                    { "error", "No commands provided" },
                    { "success", false }
                };
            }
            
            // Prepare result container
            var results = new List<Dictionary<string, object>>();
            var errors = new List<Dictionary<string, object>>();
            
            // Execute each command
            foreach (var command in commands)
            {
                try
                {
                    // Extract command type and parameters
                    if (!command.TryGetValue("type", out var typeObj) || !(typeObj is string))
                    {
                        errors.Add(new Dictionary<string, object>
                        {
                            { "error", "Command type is required and must be a string" },
                            { "command", command }
                        });
                        continue;
                    }
                    
                    string type = (string)typeObj;
                    
                    // Extract parameters
                    Dictionary<string, object> parameters = null;
                    if (command.TryGetValue("parameters", out var paramsObj))
                    {
                        if (paramsObj is Dictionary<string, object> paramsDict)
                        {
                            parameters = paramsDict;
                        }
                        else if (paramsObj is string paramsStr)
                        {
                            // Try to parse parameters as JSON
                            try
                            {
                                parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(paramsStr);
                            }
                            catch (Exception ex)
                            {
                                errors.Add(new Dictionary<string, object>
                                {
                                    { "error", $"Failed to parse parameters as JSON: {ex.Message}" },
                                    { "command", command }
                                });
                                continue;
                            }
                        }
                    }
                    
                    // Default to empty parameters if none provided
                    parameters = parameters ?? new Dictionary<string, object>();
                    
                    // Start timing the individual command execution
                    var commandStartTime = Time.realtimeSinceStartup;
                    
                    // Execute the command
                    string result = null;
                    
                    // Check if the command has a subsystem prefix (e.g., "core.GetSystemInfo")
                    string subsystem = "core"; // Default to core
                    string action = type;
                    
                    if (type.Contains("."))
                    {
                        var parts = type.Split(new[] { '.' }, 2);
                        subsystem = parts[0].ToLower();
                        action = parts[1];
                    }
                    
                    // Convert parameters to JSON string
                    string paramsJson = JsonConvert.SerializeObject(parameters);
                    
                    // Execute the command
                    result = UnityMCPBrain.Instance.ExecuteCommand(subsystem, action, paramsJson);
                    
                    // Calculate execution time
                    var commandExecutionTime = Time.realtimeSinceStartup - commandStartTime;
                    
                    // Record metrics
                    _subsystem.RecordCommandMetrics(type, commandExecutionTime);
                    
                    // Parse the result
                    Dictionary<string, object> resultDict;
                    try
                    {
                        resultDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
                    }
                    catch
                    {
                        // If parsing fails, wrap the raw result
                        resultDict = new Dictionary<string, object>
                        {
                            { "rawResult", result }
                        };
                    }
                    
                    // Add command info and execution time
                    resultDict["commandType"] = type;
                    resultDict["executionTime"] = commandExecutionTime;
                    
                    // Add to results
                    results.Add(resultDict);
                }
                catch (Exception ex)
                {
                    errors.Add(new Dictionary<string, object>
                    {
                        { "error", $"Error executing command: {ex.Message}" },
                        { "stack", ex.StackTrace },
                        { "command", command }
                    });
                }
            }
            
            // Calculate total execution time
            var totalExecutionTime = Time.realtimeSinceStartup - startTime;
            
            // Return the combined results
            return new Dictionary<string, object>
            {
                { "results", results },
                { "errors", errors },
                { "commandCount", commands.Count },
                { "successCount", results.Count },
                { "errorCount", errors.Count },
                { "executionTime", totalExecutionTime },
                { "success", errors.Count == 0 }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetMetrics(string commandType = null)
        {
            if (string.IsNullOrEmpty(commandType))
            {
                // Return metrics for all commands
                var allMetrics = _subsystem.GetAllCommandMetrics();
                var metricsDict = new Dictionary<string, object>();
                
                foreach (var kvp in allMetrics)
                {
                    metricsDict[kvp.Key] = kvp.Value.ToDict();
                }
                
                return new Dictionary<string, object>
                {
                    { "metrics", metricsDict },
                    { "commandCount", allMetrics.Count },
                    { "success", true }
                };
            }
            else
            {
                // Return metrics for a specific command
                var metrics = _subsystem.GetCommandMetrics(commandType);
                
                if (metrics != null)
                {
                    return new Dictionary<string, object>
                    {
                        { "commandType", commandType },
                        { "metrics", metrics.ToDict() },
                        { "success", true }
                    };
                }
                else
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"No metrics available for command type: {commandType}" },
                        { "success", false }
                    };
                }
            }
        }
    }
}
