using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace UnityMCP.Subsystems
{
    /// <summary>
    /// Subsystem for collecting telemetry and performance metrics
    /// </summary>
    public class TelemetrySubsystem : MonoBehaviour, IUnityMCPSubsystem, ICommandProvider
    {
        private UnityMCPBrain _brain;
        private bool _initialized = false;
        private TelemetryCommandHandler _commandHandler;
        
        // Configuration
        [SerializeField] private bool _enableTelemetry = true;
        [SerializeField] private bool _enablePerformanceMetrics = true;
        [SerializeField] private bool _enableUsageStatistics = true;
        [SerializeField] private string _telemetryFilePath = "Logs/mcp_telemetry.json";
        [SerializeField] private int _flushInterval = 60; // Seconds
        
        // Telemetry data
        private Dictionary<string, CommandUsageStats> _commandUsage = new Dictionary<string, CommandUsageStats>();
        private Dictionary<string, PerformanceMetric> _performanceMetrics = new Dictionary<string, PerformanceMetric>();
        private List<ErrorEvent> _errorEvents = new List<ErrorEvent>();
        
        // Session data
        private string _sessionId;
        private DateTime _sessionStartTime;
        private int _totalCommandsExecuted = 0;
        private int _totalErrors = 0;
        
        public void Initialize(UnityMCPBrain brain)
        {
            if (_initialized) return;
            
            _brain = brain;
            _commandHandler = new TelemetryCommandHandler(this);
            
            // Generate a new session ID
            _sessionId = Guid.NewGuid().ToString();
            _sessionStartTime = DateTime.Now;
            
            // Start the telemetry flush coroutine
            if (_enableTelemetry)
            {
                StartCoroutine(FlushTelemetryData());
            }
            
            _initialized = true;
            _brain.LogInfo("Telemetry subsystem initialized");
        }
        
        public void Shutdown()
        {
            if (_enableTelemetry)
            {
                // Flush telemetry data before shutting down
                FlushTelemetryDataNow();
            }
            
            _initialized = false;
            _brain.LogInfo("Telemetry subsystem shut down");
        }
        
        public string GetName()
        {
            return "Telemetry";
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
                { "telemetry", _commandHandler }
            };
        }
        
        /// <summary>
        /// Record a command execution
        /// </summary>
        public void RecordCommandExecution(string commandType, float executionTime, bool success)
        {
            if (!_enableTelemetry) return;
            
            // Update command usage stats
            if (!_commandUsage.ContainsKey(commandType))
            {
                _commandUsage[commandType] = new CommandUsageStats(commandType);
            }
            
            _commandUsage[commandType].RecordExecution(executionTime, success);
            
            // Update total commands executed
            _totalCommandsExecuted++;
        }
        
        /// <summary>
        /// Record a performance metric
        /// </summary>
        public void RecordPerformanceMetric(string metricName, float value)
        {
            if (!_enablePerformanceMetrics) return;
            
            // Update performance metric
            if (!_performanceMetrics.ContainsKey(metricName))
            {
                _performanceMetrics[metricName] = new PerformanceMetric(metricName);
            }
            
            _performanceMetrics[metricName].RecordValue(value);
        }
        
        /// <summary>
        /// Record an error event
        /// </summary>
        public void RecordError(string errorType, string message, string commandType = null)
        {
            if (!_enableTelemetry) return;
            
            // Create error event
            var errorEvent = new ErrorEvent
            {
                Timestamp = DateTime.Now,
                ErrorType = errorType,
                Message = message,
                CommandType = commandType
            };
            
            // Add to error events
            _errorEvents.Add(errorEvent);
            
            // Update total errors
            _totalErrors++;
        }
        
        /// <summary>
        /// Get command usage statistics
        /// </summary>
        public Dictionary<string, CommandUsageStats> GetCommandUsageStats()
        {
            return new Dictionary<string, CommandUsageStats>(_commandUsage);
        }
        
        /// <summary>
        /// Get performance metrics
        /// </summary>
        public Dictionary<string, PerformanceMetric> GetPerformanceMetrics()
        {
            return new Dictionary<string, PerformanceMetric>(_performanceMetrics);
        }
        
        /// <summary>
        /// Get error events
        /// </summary>
        public List<ErrorEvent> GetErrorEvents()
        {
            return new List<ErrorEvent>(_errorEvents);
        }
        
        /// <summary>
        /// Get session statistics
        /// </summary>
        public Dictionary<string, object> GetSessionStats()
        {
            var sessionDuration = DateTime.Now - _sessionStartTime;
            
            return new Dictionary<string, object>
            {
                { "sessionId", _sessionId },
                { "startTime", _sessionStartTime.ToString("yyyy-MM-dd HH:mm:ss") },
                { "duration", sessionDuration.TotalSeconds },
                { "totalCommandsExecuted", _totalCommandsExecuted },
                { "totalErrors", _totalErrors },
                { "commandsPerMinute", _totalCommandsExecuted / sessionDuration.TotalMinutes },
                { "errorRate", _totalCommandsExecuted > 0 ? (float)_totalErrors / _totalCommandsExecuted : 0 }
            };
        }
        
        /// <summary>
        /// Clear telemetry data
        /// </summary>
        public void ClearTelemetryData()
        {
            _commandUsage.Clear();
            _performanceMetrics.Clear();
            _errorEvents.Clear();
            
            // Reset session counters
            _totalCommandsExecuted = 0;
            _totalErrors = 0;
            
            _brain.LogInfo("Telemetry data cleared");
        }
        
        /// <summary>
        /// Coroutine to periodically flush telemetry data
        /// </summary>
        private IEnumerator FlushTelemetryData()
        {
            while (_enableTelemetry)
            {
                // Wait for the flush interval
                yield return new WaitForSeconds(_flushInterval);
                
                // Flush telemetry data
                FlushTelemetryDataNow();
            }
        }
        
        /// <summary>
        /// Flush telemetry data to disk
        /// </summary>
        private void FlushTelemetryDataNow()
        {
            try
            {
                // Create telemetry data object
                var telemetryData = new Dictionary<string, object>
                {
                    { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "session", GetSessionStats() },
                    { "commandUsage", _commandUsage.Values.Select(c => c.ToDict()).ToList() },
                    { "performanceMetrics", _performanceMetrics.Values.Select(p => p.ToDict()).ToList() },
                    { "errorEvents", _errorEvents.Select(e => e.ToDict()).ToList() }
                };
                
                // Convert to JSON
                string json = JsonConvert.SerializeObject(telemetryData, Formatting.Indented);
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(_telemetryFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write to file
                File.WriteAllText(_telemetryFilePath, json);
                
                _brain.LogInfo($"Telemetry data flushed to {_telemetryFilePath}");
            }
            catch (Exception ex)
            {
                _brain.LogError($"Failed to flush telemetry data: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Class to track command usage statistics
    /// </summary>
    public class CommandUsageStats
    {
        public string CommandType { get; private set; }
        public int ExecutionCount { get; private set; }
        public int SuccessCount { get; private set; }
        public int ErrorCount { get; private set; }
        public float TotalExecutionTime { get; private set; }
        public float AverageExecutionTime => ExecutionCount > 0 ? TotalExecutionTime / ExecutionCount : 0;
        public float MinExecutionTime { get; private set; } = float.MaxValue;
        public float MaxExecutionTime { get; private set; } = 0;
        public DateTime FirstExecution { get; private set; }
        public DateTime LastExecution { get; private set; }
        
        public CommandUsageStats(string commandType)
        {
            CommandType = commandType;
            FirstExecution = DateTime.Now;
            LastExecution = DateTime.Now;
        }
        
        public void RecordExecution(float executionTime, bool success)
        {
            ExecutionCount++;
            
            if (success)
            {
                SuccessCount++;
            }
            else
            {
                ErrorCount++;
            }
            
            TotalExecutionTime += executionTime;
            
            if (executionTime < MinExecutionTime)
            {
                MinExecutionTime = executionTime;
            }
            
            if (executionTime > MaxExecutionTime)
            {
                MaxExecutionTime = executionTime;
            }
            
            LastExecution = DateTime.Now;
        }
        
        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                { "commandType", CommandType },
                { "executionCount", ExecutionCount },
                { "successCount", SuccessCount },
                { "errorCount", ErrorCount },
                { "successRate", ExecutionCount > 0 ? (float)SuccessCount / ExecutionCount : 0 },
                { "totalExecutionTime", TotalExecutionTime },
                { "averageExecutionTime", AverageExecutionTime },
                { "minExecutionTime", MinExecutionTime == float.MaxValue ? 0 : MinExecutionTime },
                { "maxExecutionTime", MaxExecutionTime },
                { "firstExecution", FirstExecution.ToString("yyyy-MM-dd HH:mm:ss") },
                { "lastExecution", LastExecution.ToString("yyyy-MM-dd HH:mm:ss") }
            };
        }
    }
    
    /// <summary>
    /// Class to track performance metrics
    /// </summary>
    public class PerformanceMetric
    {
        public string MetricName { get; private set; }
        public int SampleCount { get; private set; }
        public float TotalValue { get; private set; }
        public float AverageValue => SampleCount > 0 ? TotalValue / SampleCount : 0;
        public float MinValue { get; private set; } = float.MaxValue;
        public float MaxValue { get; private set; } = float.MinValue;
        public DateTime FirstSample { get; private set; }
        public DateTime LastSample { get; private set; }
        
        public PerformanceMetric(string metricName)
        {
            MetricName = metricName;
            FirstSample = DateTime.Now;
            LastSample = DateTime.Now;
        }
        
        public void RecordValue(float value)
        {
            SampleCount++;
            TotalValue += value;
            
            if (value < MinValue)
            {
                MinValue = value;
            }
            
            if (value > MaxValue)
            {
                MaxValue = value;
            }
            
            LastSample = DateTime.Now;
        }
        
        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                { "metricName", MetricName },
                { "sampleCount", SampleCount },
                { "totalValue", TotalValue },
                { "averageValue", AverageValue },
                { "minValue", MinValue == float.MaxValue ? 0 : MinValue },
                { "maxValue", MaxValue == float.MinValue ? 0 : MaxValue },
                { "firstSample", FirstSample.ToString("yyyy-MM-dd HH:mm:ss") },
                { "lastSample", LastSample.ToString("yyyy-MM-dd HH:mm:ss") }
            };
        }
    }
    
    /// <summary>
    /// Class to track error events
    /// </summary>
    public class ErrorEvent
    {
        public DateTime Timestamp { get; set; }
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string CommandType { get; set; }
        
        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                { "timestamp", Timestamp.ToString("yyyy-MM-dd HH:mm:ss") },
                { "errorType", ErrorType },
                { "message", Message },
                { "commandType", CommandType }
            };
        }
    }
    
    /// <summary>
    /// Command handler for telemetry operations
    /// </summary>
    public class TelemetryCommandHandler : CommandHandler
    {
        private TelemetrySubsystem _subsystem;
        
        public TelemetryCommandHandler(TelemetrySubsystem subsystem)
        {
            _subsystem = subsystem;
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetStats()
        {
            // Get session stats
            var sessionStats = _subsystem.GetSessionStats();
            
            // Get command usage stats
            var commandUsage = _subsystem.GetCommandUsageStats();
            var commandUsageList = commandUsage.Values.Select(c => c.ToDict()).ToList();
            
            // Get performance metrics
            var performanceMetrics = _subsystem.GetPerformanceMetrics();
            var performanceMetricsList = performanceMetrics.Values.Select(p => p.ToDict()).ToList();
            
            // Get error events
            var errorEvents = _subsystem.GetErrorEvents();
            var errorEventsList = errorEvents.Select(e => e.ToDict()).ToList();
            
            // Return combined stats
            return new Dictionary<string, object>
            {
                { "session", sessionStats },
                { "commandUsage", commandUsageList },
                { "performanceMetrics", performanceMetricsList },
                { "errorEvents", errorEventsList },
                { "success", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetCommandStats(string commandType = null)
        {
            // Get command usage stats
            var commandUsage = _subsystem.GetCommandUsageStats();
            
            if (string.IsNullOrEmpty(commandType))
            {
                // Return stats for all commands
                var commandUsageList = commandUsage.Values.Select(c => c.ToDict()).ToList();
                
                return new Dictionary<string, object>
                {
                    { "commandStats", commandUsageList },
                    { "commandCount", commandUsageList.Count },
                    { "success", true }
                };
            }
            else
            {
                // Return stats for a specific command
                if (commandUsage.TryGetValue(commandType, out var stats))
                {
                    return new Dictionary<string, object>
                    {
                        { "commandType", commandType },
                        { "stats", stats.ToDict() },
                        { "success", true }
                    };
                }
                else
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"No stats available for command type: {commandType}" },
                        { "success", false }
                    };
                }
            }
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetPerformanceStats(string metricName = null)
        {
            // Get performance metrics
            var performanceMetrics = _subsystem.GetPerformanceMetrics();
            
            if (string.IsNullOrEmpty(metricName))
            {
                // Return stats for all metrics
                var metricsList = performanceMetrics.Values.Select(p => p.ToDict()).ToList();
                
                return new Dictionary<string, object>
                {
                    { "performanceMetrics", metricsList },
                    { "metricCount", metricsList.Count },
                    { "success", true }
                };
            }
            else
            {
                // Return stats for a specific metric
                if (performanceMetrics.TryGetValue(metricName, out var metric))
                {
                    return new Dictionary<string, object>
                    {
                        { "metricName", metricName },
                        { "metric", metric.ToDict() },
                        { "success", true }
                    };
                }
                else
                {
                    return new Dictionary<string, object>
                    {
                        { "error", $"No stats available for metric: {metricName}" },
                        { "success", false }
                    };
                }
            }
        }
        
        [CommandMethod]
        public Dictionary<string, object> GetErrorStats()
        {
            // Get error events
            var errorEvents = _subsystem.GetErrorEvents();
            var errorEventsList = errorEvents.Select(e => e.ToDict()).ToList();
            
            // Group errors by type
            var errorsByType = errorEvents.GroupBy(e => e.ErrorType)
                .Select(g => new Dictionary<string, object>
                {
                    { "errorType", g.Key },
                    { "count", g.Count() },
                    { "percentage", errorEvents.Count > 0 ? (float)g.Count() / errorEvents.Count : 0 }
                })
                .ToList();
            
            // Group errors by command type
            var errorsByCommand = errorEvents.Where(e => !string.IsNullOrEmpty(e.CommandType))
                .GroupBy(e => e.CommandType)
                .Select(g => new Dictionary<string, object>
                {
                    { "commandType", g.Key },
                    { "count", g.Count() },
                    { "percentage", errorEvents.Count > 0 ? (float)g.Count() / errorEvents.Count : 0 }
                })
                .ToList();
            
            // Return error stats
            return new Dictionary<string, object>
            {
                { "errorEvents", errorEventsList },
                { "errorCount", errorEventsList.Count },
                { "errorsByType", errorsByType },
                { "errorsByCommand", errorsByCommand },
                { "success", true }
            };
        }
        
        [CommandMethod]
        public Dictionary<string, object> ClearStats()
        {
            // Clear telemetry data
            _subsystem.ClearTelemetryData();
            
            // Return success
            return new Dictionary<string, object>
            {
                { "message", "Telemetry data cleared successfully" },
                { "success", true }
            };
        }
    }
}
