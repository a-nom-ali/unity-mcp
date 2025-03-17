#!/usr/bin/env python3
"""
Connection management for Unity MCP.
"""

import json
import logging
import socket
import time
from typing import Any, Dict, Optional

from .error_reporter import error_reporter

# Initialize logger
logger = logging.getLogger("unity_mcp.connection")


class UnityConnection:
    """Manages connection to Unity."""
    
    def __init__(self, host: str, port: int):
        """Initialize a connection to Unity.
        
        Args:
            host: The host to connect to
            port: The port to connect to
        """
        self.host = host
        self.port = port
        self.sock = None
        self.reconnect_attempts = 0
        self.max_reconnect_attempts = 5
        self.reconnect_delay = 2.0
        self.last_heartbeat = 0
        self.heartbeat_interval = 30.0  # Send heartbeat every 30 seconds
        self.socket_timeout = 15.0
        self.connected = False
        
    def connect(self) -> bool:
        """Connect to the Unity plugin.
        
        Returns:
            True if connection was successful, False otherwise
        """
        try:
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock.settimeout(self.socket_timeout)
            self.sock.connect((self.host, self.port))
            self.last_heartbeat = time.time()
            self.reconnect_attempts = 0
            self.connected = True
            logger.info(f"Connected to Unity at {self.host}:{self.port}")
            return True
        except Exception as e:
            # Report connection error
            error_info = error_reporter.format_exception(e)
            error_reporter.report_error(
                error_info["type"],
                error_info["message"],
                {"host": self.host, "port": self.port, "reconnect_attempts": self.reconnect_attempts},
                error_info["stack_trace"]
            )
            
            self.sock = None
            self.reconnect_attempts += 1
            self.connected = False
            logger.error(f"Failed to connect to Unity: {str(e)}")
            return False

    def disconnect(self):
        """Disconnect from the Unity plugin."""
        if self.sock:
            try:
                self.sock.close()
            except Exception as e:
                # Report disconnection error
                error_info = error_reporter.format_exception(e)
                error_reporter.report_error(
                    error_info["type"],
                    error_info["message"],
                    {"host": self.host, "port": self.port},
                    error_info["stack_trace"]
                )
            finally:
                self.sock = None
                self.connected = False

    def reconnect(self) -> bool:
        """Attempt to reconnect to Unity.
        
        Returns:
            True if reconnection was successful, False otherwise
        """
        if self.reconnect_attempts >= self.max_reconnect_attempts:
            logger.error(f"Maximum reconnection attempts reached ({self.max_reconnect_attempts})")
            return False
            
        logger.info(f"Attempting to reconnect to Unity (attempt {self.reconnect_attempts + 1}/{self.max_reconnect_attempts})")
        
        # Disconnect if still connected
        self.disconnect()
        
        # Wait before reconnecting
        time.sleep(self.reconnect_delay * (self.reconnect_attempts + 1))
        
        # Attempt to reconnect
        return self.connect()

    def send_command(self, action: str, params: Dict = None) -> Dict:
        """Send a command to Unity and receive the response.
        
        Args:
            action: The command action to send
            params: The parameters for the command
            
        Returns:
            The response from Unity as a dictionary
            
        Raises:
            ConnectionError: If connection to Unity fails
        """
        if not self.ensure_connected():
            raise ConnectionError("Not connected to Unity")
            
        try:
            # Check if we need to send a heartbeat
            self._check_heartbeat()
            
            # Prepare the command
            command = {
                "action": action,
                "params": params or {}
            }
            
            # Send the command
            self._send_data(json.dumps(command))
            
            # Receive the response
            response_data = self._receive_full_response()
            
            # Parse the response
            try:
                response = json.loads(response_data)
                return response
            except json.JSONDecodeError as e:
                error_info = error_reporter.format_exception(e)
                error_reporter.report_error(
                    error_info["type"],
                    f"Failed to parse response: {response_data[:100]}...",
                    {"action": action},
                    error_info["stack_trace"]
                )
                raise ConnectionError(f"Failed to parse response: {str(e)}")
                
        except socket.error as e:
            # Handle connection errors
            error_info = error_reporter.format_exception(e)
            error_id = error_reporter.report_error(
                error_info["type"],
                error_info["message"],
                {"action": action},
                error_info["stack_trace"]
            )
            
            logger.error(f"Socket error when sending command: {str(e)}")
            
            # Attempt to reconnect
            if "10053" in str(e) or "10054" in str(e):  # Connection aborted or reset
                logger.info("Connection aborted or reset, attempting to reconnect")
                if self.reconnect():
                    logger.info("Reconnected successfully, retrying command")
                    return self.send_command(action, params)
            
            raise ConnectionError(f"Socket error when sending command: {str(e)}")
            
        except Exception as e:
            # Handle other errors
            error_info = error_reporter.format_exception(e)
            error_id = error_reporter.report_error(
                error_info["type"],
                error_info["message"],
                {"action": action},
                error_info["stack_trace"]
            )
            
            logger.error(f"Error sending command: {str(e)}")
            raise ConnectionError(f"Error sending command: {str(e)}")

    def ensure_connected(self) -> bool:
        """Ensure that we are connected to Unity.
        
        Returns:
            True if connected, False otherwise
        """
        if self.connected and self.sock:
            return True
            
        return self.connect()

    def _send_data(self, data: str):
        """Send data to Unity.
        
        Args:
            data: The data to send
            
        Raises:
            ConnectionError: If sending data fails
        """
        if not self.sock:
            raise ConnectionError("Not connected to Unity")
            
        try:
            # Send the data length as a 4-byte integer
            data_bytes = data.encode('utf-8')
            length = len(data_bytes)
            self.sock.sendall(length.to_bytes(4, byteorder='little'))
            
            # Send the data
            self.sock.sendall(data_bytes)
        except Exception as e:
            raise ConnectionError(f"Failed to send data: {str(e)}")

    def _receive_data(self, size: int) -> bytes:
        """Receive data from Unity.
        
        Args:
            size: The number of bytes to receive
            
        Returns:
            The received data
            
        Raises:
            ConnectionError: If receiving data fails
        """
        if not self.sock:
            raise ConnectionError("Not connected to Unity")
            
        try:
            data = b''
            while len(data) < size:
                chunk = self.sock.recv(size - len(data))
                if not chunk:
                    raise ConnectionError("Connection closed by Unity")
                data += chunk
            return data
        except Exception as e:
            raise ConnectionError(f"Failed to receive data: {str(e)}")

    def _receive_full_response(self) -> str:
        """Receive a full response from Unity.
        
        Returns:
            The received response as a string
            
        Raises:
            ConnectionError: If receiving response fails
        """
        try:
            # Receive the response length
            length_bytes = self._receive_data(4)
            length = int.from_bytes(length_bytes, byteorder='little')
            
            # Receive the response data
            response_bytes = self._receive_data(length)
            
            # Decode the response
            return response_bytes.decode('utf-8')
        except Exception as e:
            raise ConnectionError(f"Failed to receive response: {str(e)}")

    def _check_heartbeat(self):
        """Check if we need to send a heartbeat to keep the connection alive."""
        current_time = time.time()
        if current_time - self.last_heartbeat >= self.heartbeat_interval:
            try:
                logger.debug("Sending heartbeat")
                self._send_data(json.dumps({"action": "heartbeat"}))
                response_data = self._receive_full_response()
                self.last_heartbeat = current_time
                logger.debug(f"Heartbeat response: {response_data}")
            except Exception as e:
                logger.warning(f"Failed to send heartbeat: {str(e)}")


# Global connection instance
_unity_connection = None


def get_unity_connection(host: str = "localhost", port: int = 8080) -> Optional[UnityConnection]:
    """Get or create a connection to Unity.
    
    Args:
        host: The host to connect to
        port: The port to connect to
        
    Returns:
        The connection to Unity, or None if connection failed
    """
    global _unity_connection
    
    if _unity_connection is None:
        _unity_connection = UnityConnection(host, port)
        if not _unity_connection.connect():
            logger.error("Failed to establish initial connection to Unity")
            return None
    
    return _unity_connection
