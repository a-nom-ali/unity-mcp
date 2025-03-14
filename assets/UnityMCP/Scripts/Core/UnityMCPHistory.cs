using System;
using System.Collections.Generic;

namespace UnityMCP
{
    /// <summary>
    /// Maintains a history of commands and their results
    /// </summary>
    public class UnityMCPHistory
    {
        private List<HistoryEntry> _history = new List<HistoryEntry>();
        private int _maxHistorySize = 100;
        
        public void RecordCommand(CommandData command, string result)
        {
            var entry = new HistoryEntry
            {
                Timestamp = DateTime.Now,
                Command = command,
                Result = result
            };
            
            _history.Add(entry);
            
            // Trim history if it exceeds the maximum size
            if (_history.Count > _maxHistorySize)
            {
                _history.RemoveAt(0);
            }
        }
        
        public List<HistoryEntry> GetHistory()
        {
            return new List<HistoryEntry>(_history);
        }
        
        public HistoryEntry GetLastEntry()
        {
            if (_history.Count > 0)
            {
                return _history[_history.Count - 1];
            }
            
            return null;
        }
        
        public void Clear()
        {
            _history.Clear();
        }
        
        public void SetMaxHistorySize(int size)
        {
            _maxHistorySize = Math.Max(1, size);
            
            // Trim history if it exceeds the new maximum size
            while (_history.Count > _maxHistorySize)
            {
                _history.RemoveAt(0);
            }
        }
    }
    
    public class HistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public CommandData Command { get; set; }
        public string Result { get; set; }
    }
} 