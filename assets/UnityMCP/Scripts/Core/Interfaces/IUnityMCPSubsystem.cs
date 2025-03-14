using System.Collections.Generic;

namespace UnityMCP
{
    /// <summary>
    /// Interface for all UnityMCP subsystems
    /// </summary>
    public interface IUnityMCPSubsystem
    {
        /// <summary>
        /// Initialize the subsystem with a reference to the brain
        /// </summary>
        void Initialize(UnityMCPBrain brain);
        
        /// <summary>
        /// Shutdown the subsystem
        /// </summary>
        void Shutdown();
        
        /// <summary>
        /// Get the name of the subsystem
        /// </summary>
        string GetName();
        
        /// <summary>
        /// Get the version of the subsystem
        /// </summary>
        string GetVersion();
        
        /// <summary>
        /// Check if the subsystem is initialized
        /// </summary>
        bool IsInitialized();
    }

    /// <summary>
    /// Interface for subsystems that provide command handlers
    /// </summary>
    public interface ICommandProvider
    {
        /// <summary>
        /// Get the command handlers provided by this subsystem
        /// </summary>
        Dictionary<string, CommandHandler> GetCommandHandlers();
    }
} 