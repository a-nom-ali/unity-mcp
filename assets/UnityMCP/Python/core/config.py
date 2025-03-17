#!/usr/bin/env python3
"""
Configuration settings for Unity MCP.
"""

import json
import logging
import os
from dataclasses import dataclass
from typing import Dict, List, Optional

# Initialize logger
logger = logging.getLogger("unity_mcp.config")


@dataclass
class ConnectionConfig:
    """Configuration for Unity connection."""
    host: str = "localhost"
    port: int = 8080
    socket_timeout: float = 15.0
    max_reconnect_attempts: int = 5
    reconnect_delay: float = 2.0
    heartbeat_interval: float = 30.0


@dataclass
class LoggingConfig:
    """Configuration for logging."""
    level: str = "INFO"
    format: str = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
    log_dir: str = "logs"
    error_log_file: str = "unity_mcp_errors.log"
    max_errors_in_memory: int = 100


@dataclass
class PerformanceConfig:
    """Configuration for performance optimization."""
    enable_caching: bool = True
    cache_ttl: int = 300  # seconds
    max_cache_size: int = 1000  # items
    batch_size_limit: int = 50  # maximum number of commands in a batch
    async_timeout: int = 300  # seconds
    max_async_operations: int = 100


@dataclass
class TelemetryConfig:
    """Configuration for telemetry and metrics."""
    enabled: bool = True
    collect_performance_metrics: bool = True
    collect_usage_metrics: bool = True
    metrics_interval: int = 60  # seconds
    max_metrics_in_memory: int = 1000


@dataclass
class UnityMCPConfig:
    """Main configuration for Unity MCP."""
    connection: ConnectionConfig = ConnectionConfig()
    logging: LoggingConfig = LoggingConfig()
    performance: PerformanceConfig = PerformanceConfig()
    telemetry: TelemetryConfig = TelemetryConfig()
    version: str = "1.0.0"
    
    @classmethod
    def from_file(cls, file_path: str) -> 'UnityMCPConfig':
        """Load configuration from a JSON file.
        
        Args:
            file_path: Path to the configuration file
            
        Returns:
            The loaded configuration
        """
        try:
            with open(file_path, 'r') as f:
                config_dict = json.load(f)
                
            # Create the configuration objects
            connection_config = ConnectionConfig(**config_dict.get('connection', {}))
            logging_config = LoggingConfig(**config_dict.get('logging', {}))
            performance_config = PerformanceConfig(**config_dict.get('performance', {}))
            telemetry_config = TelemetryConfig(**config_dict.get('telemetry', {}))
            
            return cls(
                connection=connection_config,
                logging=logging_config,
                performance=performance_config,
                telemetry=telemetry_config,
                version=config_dict.get('version', '1.0.0')
            )
        except Exception as e:
            logger.error(f"Failed to load configuration from {file_path}: {str(e)}")
            logger.info("Using default configuration")
            return cls()
    
    def to_file(self, file_path: str) -> bool:
        """Save configuration to a JSON file.
        
        Args:
            file_path: Path to save the configuration file
            
        Returns:
            True if successful, False otherwise
        """
        try:
            # Create the directory if it doesn't exist
            os.makedirs(os.path.dirname(file_path), exist_ok=True)
            
            # Convert the configuration to a dictionary
            config_dict = {
                'connection': {
                    'host': self.connection.host,
                    'port': self.connection.port,
                    'socket_timeout': self.connection.socket_timeout,
                    'max_reconnect_attempts': self.connection.max_reconnect_attempts,
                    'reconnect_delay': self.connection.reconnect_delay,
                    'heartbeat_interval': self.connection.heartbeat_interval
                },
                'logging': {
                    'level': self.logging.level,
                    'format': self.logging.format,
                    'log_dir': self.logging.log_dir,
                    'error_log_file': self.logging.error_log_file,
                    'max_errors_in_memory': self.logging.max_errors_in_memory
                },
                'performance': {
                    'enable_caching': self.performance.enable_caching,
                    'cache_ttl': self.performance.cache_ttl,
                    'max_cache_size': self.performance.max_cache_size,
                    'batch_size_limit': self.performance.batch_size_limit,
                    'async_timeout': self.performance.async_timeout,
                    'max_async_operations': self.performance.max_async_operations
                },
                'telemetry': {
                    'enabled': self.telemetry.enabled,
                    'collect_performance_metrics': self.telemetry.collect_performance_metrics,
                    'collect_usage_metrics': self.telemetry.collect_usage_metrics,
                    'metrics_interval': self.telemetry.metrics_interval,
                    'max_metrics_in_memory': self.telemetry.max_metrics_in_memory
                },
                'version': self.version
            }
            
            # Save the configuration to the file
            with open(file_path, 'w') as f:
                json.dump(config_dict, f, indent=4)
                
            logger.info(f"Configuration saved to {file_path}")
            return True
        except Exception as e:
            logger.error(f"Failed to save configuration to {file_path}: {str(e)}")
            return False


# Default configuration
config = UnityMCPConfig()

# Configuration file path
config_file_path = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "config.json")

# Load configuration from file if it exists
if os.path.exists(config_file_path):
    config = UnityMCPConfig.from_file(config_file_path)
else:
    # Save default configuration to file
    config.to_file(config_file_path)
