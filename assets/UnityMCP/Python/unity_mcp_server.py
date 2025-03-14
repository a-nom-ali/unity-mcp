#!/usr/bin/env python3
"""Unity integration through the Model Context Protocol."""

import argparse
import asyncio
import json
import logging
import socket
import sys
from contextlib import asynccontextmanager
from dataclasses import dataclass
from typing import Any, AsyncIterator, Dict, List, Optional
from mcp.server.fastmcp import Context, FastMCP
import mcp

__version__ = "1.0.0"

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    handlers=[logging.StreamHandler()],
)
logger = logging.getLogger("unity_mcp")

@dataclass
class UnityConnection:
    host: str
    port: int
    sock: socket.socket = None
    
    def connect(self) -> bool:
        """Connect to the Unity plugin socket server"""
        if self.sock:
            return True
            
        try:
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock.connect((self.host, self.port))
            logger.info(f"Connected to Unity at {self.host}:{self.port}")
            return True
        except Exception as e:
            logger.error(f"Failed to connect to Unity: {str(e)}")
            self.sock = None
            return False
    
    def disconnect(self):
        """Disconnect from the Unity plugin"""
        if self.sock:
            try:
                self.sock.close()
            except Exception as e:
                logger.error(f"Error disconnecting from Unity: {str(e)}")
            finally:
                self.sock = None

    def receive_full_response(self, sock, buffer_size=8192):
        """Receive the complete response, potentially in multiple chunks"""
        chunks = []
        sock.settimeout(15.0)
        
        try:
            while True:
                try:
                    chunk = sock.recv(buffer_size)
                    if not chunk:
                        if not chunks:
                            raise Exception("Connection closed before receiving any data")
                        break
                    
                    chunks.append(chunk)
                    
                    # Check if we've received a complete JSON object
                    try:
                        data = b''.join(chunks)
                        json.loads(data.decode('utf-8'))
                        logger.info(f"Received complete response ({len(data)} bytes)")
                        return data
                    except json.JSONDecodeError:
                        # Incomplete JSON, continue receiving
                        continue
                except socket.timeout:
                    logger.warning("Socket timeout during chunked receive")
                    break
                except (ConnectionError, BrokenPipeError, ConnectionResetError) as e:
                    logger.error(f"Socket connection error during receive: {str(e)}")
                    raise
        except socket.timeout:
            logger.warning("Socket timeout during chunked receive")
        except Exception as e:
            logger.error(f"Error during receive: {str(e)}")
            raise
            
        # If we get here, we either timed out or broke out of the loop
        if chunks:
            data = b''.join(chunks)
            logger.info(f"Returning data after receive completion ({len(data)} bytes)")
            try:
                json.loads(data.decode('utf-8'))
                return data
            except json.JSONDecodeError:
                raise Exception("Incomplete JSON response received")
        else:
            raise Exception("No data received")

    def send_command(self, command_type: str, params: Dict[str, Any] = None) -> Dict[str, Any]:
        """Send a command to Unity and return the response"""
        if not self.sock and not self.connect():
            raise ConnectionError("Not connected to Unity")
        
        command = {
            "type": command_type,
            "parameters": json.dumps(params or {})
        }
        
        try:
            logger.info(f"Sending command: {command_type} with params: {params}")
            
            self.sock.sendall(json.dumps(command).encode('utf-8'))
            logger.info(f"Command sent, waiting for response...")
            
            self.sock.settimeout(15.0)
            
            response_data = self.receive_full_response(self.sock)
            logger.info(f"Received {len(response_data)} bytes of data")
            
            response = json.loads(response_data.decode('utf-8'))
            logger.info(f"Response parsed, status: {response.get('status', 'unknown')}")
            
            if response.get("status") == "error":
                logger.error(f"Unity error: {response.get('message')}")
                raise Exception(response.get("message", "Unknown error from Unity"))
            
            return response.get("result", {})
        except socket.timeout:
            logger.error("Socket timeout while waiting for response from Unity")
            self.sock = None
            raise Exception("Timeout waiting for Unity response - try simplifying your request")
        except (ConnectionError, BrokenPipeError, ConnectionResetError) as e:
            logger.error(f"Socket connection error: {str(e)}")
            self.sock = None
            raise Exception(f"Connection to Unity lost: {str(e)}")
        except json.JSONDecodeError as e:
            logger.error(f"Invalid JSON response from Unity: {str(e)}")
            if 'response_data' in locals() and response_data:
                logger.error(f"Raw response (first 200 bytes): {response_data[:200]}")
            raise Exception(f"Invalid response from Unity: {str(e)}")
        except Exception as e:
            logger.error(f"Error communicating with Unity: {str(e)}")
            self.sock = None
            raise Exception(f"Communication error with Unity: {str(e)}")


# Global connection instance
_unity_connection = None

def get_unity_connection():
    """Get or create a connection to Unity"""
    global _unity_connection
    
    if _unity_connection is None:
        _unity_connection = UnityConnection(host="localhost", port=9876)
    
    # Try to connect if not already connected
    if not _unity_connection.connect():
        # If connection fails, create a new connection and try again
        _unity_connection = UnityConnection(host="localhost", port=9876)
        if not _unity_connection.connect():
            raise ConnectionError("Failed to connect to Unity")
    
    return _unity_connection


@asynccontextmanager
async def server_lifespan(server: FastMCP) -> AsyncIterator[Dict[str, Any]]:
    """Lifecycle manager for the MCP server"""
    # Setup phase
    logger.info("Starting Unity MCP server")
    
    # Try to establish a connection to Unity
    try:
        connection = get_unity_connection()
        logger.info("Connected to Unity successfully")
    except Exception as e:
        logger.warning(f"Could not connect to Unity at startup: {str(e)}")
        logger.warning("Will try to connect when commands are received")
    
    try:
        yield {"status": "running"}
    finally:
        # Cleanup phase
        logger.info("Shutting down Unity MCP server")
        
        # Disconnect from Unity
        if _unity_connection:
            _unity_connection.disconnect()


# MCP Tools

@mcp.tool()
def get_system_info(ctx: Context) -> str:
    """Get information about the Unity system.
    
    Returns:
        A JSON string containing information about the Unity system, including version, platform, etc.
    """
    try:
        connection = get_unity_connection()
        result = connection.send_command("core.GetSystemInfo")
        return json.dumps(result)
    except Exception as e:
        return f"Error getting system info: {str(e)}"


@mcp.tool()
def get_scene_info(ctx: Context) -> str:
    """Get information about the current Unity scene.
    
    Returns:
        A JSON string containing information about the scene, including objects, cameras, and lights.
    """
    try:
        connection = get_unity_connection()
        result = connection.send_command("scene.GetSceneInfo")
        return json.dumps(result)
    except Exception as e:
        return f"Error getting scene info: {str(e)}"


@mcp.tool()
def get_object_info(ctx: Context, object_name: str) -> str:
    """Get detailed information about a specific object in the Unity scene.
    
    Args:
        object_name: The name of the object to get information about.
        
    Returns:
        A JSON string containing detailed information about the object.
    """
    try:
        connection = get_unity_connection()
        result = connection.send_command("object.GetObjectInfo", {"name": object_name})
        return json.dumps(result)
    except Exception as e:
        return f"Error getting object info: {str(e)}"


@mcp.tool()
def create_object(
    ctx: Context,
    type: str = "Cube",
    name: str = None,
    location: List[float] = None,
    rotation: List[float] = None,
    scale: List[float] = None,
    color: List[float] = None,
    material: str = None
) -> str:
    """Create a new object in the Unity scene.
    
    Args:
        type: The type of object to create (Cube, Sphere, Cylinder, Plane, Capsule, Quad, Empty, Light, Camera).
        name: The name to give the new object. If not provided, the type will be used.
        location: The [x, y, z] coordinates for the object's position. Defaults to [0, 0, 0].
        rotation: The [x, y, z] Euler angles for the object's rotation. Defaults to [0, 0, 0].
        scale: The [x, y, z] scale factors for the object. Defaults to [1, 1, 1].
        color: The [r, g, b, a] color values (0.0-1.0) for the object's material. Alpha is optional.
        material: The name of a material to apply to the object.
        
    Returns:
        A JSON string with information about the created object.
    """
    try:
        connection = get_unity_connection()
        
        params = {
            "type": type,
            "name": name
        }
        
        if location:
            params["position"] = {
                "x": location[0],
                "y": location[1],
                "z": location[2]
            }
            
        if rotation:
            params["rotation"] = {
                "x": rotation[0],
                "y": rotation[1],
                "z": rotation[2]
            }
            
        if scale:
            params["scale"] = {
                "x": scale[0],
                "y": scale[1],
                "z": scale[2]
            }
            
        if color:
            params["color"] = color
            
        if material:
            params["material"] = material
        
        result = connection.send_command("object.CreatePrimitive", params)
        return json.dumps(result)
    except Exception as e:
        return f"Error creating object: {str(e)}"


@mcp.tool()
def modify_object(
    ctx: Context,
    name: str,
    location: List[float] = None,
    rotation: List[float] = None,
    scale: List[float] = None,
    visible: bool = None
) -> str:
    """Modify an existing object in the Unity scene.
    
    Args:
        name: The name of the object to modify.
        location: The new [x, y, z] coordinates for the object's position.
        rotation: The new [x, y, z] Euler angles for the object's rotation.
        scale: The new [x, y, z] scale factors for the object.
        visible: Whether the object should be visible or not.
        
    Returns:
        A JSON string with information about the modified object.
    """
    try:
        connection = get_unity_connection()
        
        params = {
            "name": name
        }
        
        if location:
            params["position"] = {
                "x": location[0],
                "y": location[1],
                "z": location[2]
            }
            
        if rotation:
            params["rotation"] = {
                "x": rotation[0],
                "y": rotation[1],
                "z": rotation[2]
            }
            
        if scale:
            params["scale"] = {
                "x": scale[0],
                "y": scale[1],
                "z": scale[2]
            }
            
        if visible is not None:
            params["visible"] = visible
        
        result = connection.send_command("object.SetObjectTransform", params)
        return json.dumps(result)
    except Exception as e:
        return f"Error modifying object: {str(e)}"


@mcp.tool()
def delete_object(ctx: Context, name: str) -> str:
    """Delete an object from the Unity scene.
    
    Args:
        name: The name of the object to delete.
        
    Returns:
        A JSON string confirming the deletion.
    """
    try:
        connection = get_unity_connection()
        result = connection.send_command("object.DeleteObject", {"name": name})
        return json.dumps(result)
    except Exception as e:
        return f"Error deleting object: {str(e)}"


@mcp.tool()
def set_material(
    ctx: Context,
    object_name: str,
    material_name: str = None,
    color: List[float] = None
) -> str:
    """Apply or create a material for an object in the Unity scene.
    
    Args:
        object_name: The name of the object to apply the material to.
        material_name: The name of the material to apply or create.
        color: The [r, g, b, a] color values (0.0-1.0) for the material. Alpha is optional.
        
    Returns:
        A JSON string confirming the material application.
    """
    try:
        connection = get_unity_connection()
        
        params = {
            "objectName": object_name
        }
        
        if material_name:
            params["materialName"] = material_name
            
        if color:
            params["color"] = {
                "r": color[0],
                "g": color[1],
                "b": color[2],
                "a": color[3] if len(color) > 3 else 1.0
            }
        
        result = connection.send_command("material.SetMaterial", params)
        return json.dumps(result)
    except Exception as e:
        return f"Error setting material: {str(e)}"


@mcp.tool()
def create_light(
    ctx: Context,
    type: str = "Point",
    name: str = None,
    location: List[float] = None,
    rotation: List[float] = None,
    color: List[float] = None,
    intensity: float = 1.0,
    range: float = 10.0
) -> str:
    """Create a light in the Unity scene.
    
    Args:
        type: The type of light (Point, Directional, Spot, Area).
        name: The name to give the light. If not provided, a default name will be used.
        location: The [x, y, z] coordinates for the light's position.
        rotation: The [x, y, z] Euler angles for the light's rotation.
        color: The [r, g, b] color values (0.0-1.0) for the light.
        intensity: The brightness of the light.
        range: The range of the light (for Point and Spot lights).
        
    Returns:
        A JSON string with information about the created light.
    """
    try:
        connection = get_unity_connection()
        
        params = {
            "type": type,
            "name": name,
            "intensity": intensity,
            "range": range
        }
        
        if location:
            params["position"] = {
                "x": location[0],
                "y": location[1],
                "z": location[2]
            }
            
        if rotation:
            params["rotation"] = {
                "x": rotation[0],
                "y": rotation[1],
                "z": rotation[2]
            }
            
        if color:
            params["color"] = {
                "r": color[0],
                "g": color[1],
                "b": color[2],
                "a": 1.0
            }
        
        result = connection.send_command("lighting.CreateLight", params)
        return json.dumps(result)
    except Exception as e:
        return f"Error creating light: {str(e)}"


@mcp.tool()
def create_camera(
    ctx: Context,
    name: str = None,
    location: List[float] = None,
    rotation: List[float] = None,
    field_of_view: float = 60.0,
    is_main: bool = False
) -> str:
    """Create a camera in the Unity scene.
    
    Args:
        name: The name to give the camera. If not provided, a default name will be used.
        location: The [x, y, z] coordinates for the camera's position.
        rotation: The [x, y, z] Euler angles for the camera's rotation.
        field_of_view: The field of view angle in degrees.
        is_main: Whether this camera should be set as the main camera.
        
    Returns:
        A JSON string with information about the created camera.
    """
    try:
        connection = get_unity_connection()
        
        params = {
            "name": name,
            "fieldOfView": field_of_view,
            "isMain": is_main
        }
        
        if location:
            params["position"] = {
                "x": location[0],
                "y": location[1],
                "z": location[2]
            }
            
        if rotation:
            params["rotation"] = {
                "x": rotation[0],
                "y": rotation[1],
                "z": rotation[2]
            }
        
        result = connection.send_command("camera.CreateCamera", params)
        return json.dumps(result)
    except Exception as e:
        return f"Error creating camera: {str(e)}"


@mcp.tool()
def camera_look_at(
    ctx: Context,
    camera_name: str,
    target_name: str
) -> str:
    """Make a camera look at a specific object.
    
    Args:
        camera_name: The name of the camera.
        target_name: The name of the object to look at.
        
    Returns:
        A JSON string confirming the operation.
    """
    try:
        connection = get_unity_connection()
        result = connection.send_command("camera.LookAt", {
            "cameraName": camera_name,
            "targetName": target_name
        })
        return json.dumps(result)
    except Exception as e:
        return f"Error making camera look at target: {str(e)}"


@mcp.tool()
def instantiate_prefab(
    ctx: Context,
    prefab_path: str,
    name: str = None,
    location: List[float] = None,
    rotation: List[float] = None,
    scale: List[float] = None
) -> str:
    """Instantiate a prefab in the Unity scene.
    
    Args:
        prefab_path: The path to the prefab asset.
        name: The name to give the instantiated prefab. If not provided, the prefab name will be used.
        location: The [x, y, z] coordinates for the prefab's position.
        rotation: The [x, y, z] Euler angles for the prefab's rotation.
        scale: The [x, y, z] scale factors for the prefab.
        
    Returns:
        A JSON string with information about the instantiated prefab.
    """
    try:
        connection = get_unity_connection()
        
        params = {
            "prefabPath": prefab_path,
            "name": name
        }
        
        if location:
            params["position"] = {
                "x": location[0],
                "y": location[1],
                "z": location[2]
            }
            
        if rotation:
            params["rotation"] = {
                "x": rotation[0],
                "y": rotation[1],
                "z": rotation[2]
            }
            
        if scale:
            params["scale"] = {
                "x": scale[0],
                "y": scale[1],
                "z": scale[2]
            }
        
        result = connection.send_command("prefab.InstantiatePrefab", params)
        return json.dumps(result)
    except Exception as e:
        return f"Error instantiating prefab: {str(e)}"


@mcp.tool()
def play_animation(
    ctx: Context,
    object_name: str,
    animation_name: str = None,
    crossfade_time: float = 0.3
) -> str:
    """Play an animation on an object.
    
    Args:
        object_name: The name of the object with the animation.
        animation_name: The name of the animation to play. If not provided, the default animation will be played.
        crossfade_time: The time to blend between animations.
        
    Returns:
        A JSON string confirming the animation playback.
    """
    try:
        connection = get_unity_connection()
        result = connection.send_command("animation.PlayAnimation", {
            "objectName": object_name,
            "animationName": animation_name,
            "crossfadeTime": crossfade_time
        })
        return json.dumps(result)
    except Exception as e:
        return f"Error playing animation: {str(e)}"


@mcp.prompt()
def unity_creation_strategy() -> str:
    """Provides guidance on creating assets in Unity through the MCP interface."""
    return """
    # Unity MCP Creation Guide
    
    When creating 3D scenes in Unity through the MCP interface, follow these guidelines:
    
    ## Basic Objects
    
    Start with primitive shapes to block out your scene:
    
    - Use `create_object` with type "Cube", "Sphere", "Cylinder", "Plane", "Capsule", or "Quad"
    - Position objects using the `location` parameter [x, y, z]
    - Rotate objects using the `rotation` parameter [x, y, z] in degrees
    - Scale objects using the `scale` parameter [x, y, z]
    
    ## Materials and Colors
    
    Apply materials and colors to objects:
    
    - Use `set_material` to apply a material to an object
    - Specify colors using RGBA values from 0.0 to 1.0, e.g., [1.0, 0.0, 0.0, 1.0] for red
    
    ## Lighting
    
    Add lights to illuminate your scene:
    
    - Use `create_light` with type "Point", "Directional", "Spot", or "Area"
    - Adjust intensity, range, and color to achieve the desired lighting effect
    
    ## Cameras
    
    Set up cameras to view your scene:
    
    - Use `create_camera` to add a camera to the scene
    - Use `camera_look_at` to point a camera at a specific object
    - Set a camera as the main camera with the `is_main` parameter
    
    ## Prefabs and Assets
    
    Use prefabs for complex objects:
    
    - Use `instantiate_prefab` to add a prefab to your scene
    - Adjust position, rotation, and scale as needed
    
    ## Animation
    
    Animate objects in your scene:
    
    - Use `play_animation` to play animations on objects
    - Specify animation names and crossfade times for smooth transitions
    
    ## Scene Organization
    
    - Use descriptive names for objects to make them easier to reference later
    - Use `get_scene_info` to see all objects in the scene
    - Use `get_object_info` to get detailed information about a specific object
    
    ## Workflow Tips
    
    1. Start with a basic layout using primitive shapes
    2. Add lighting to the scene
    3. Position the camera to frame the scene
    4. Apply materials and colors to objects
    5. Add prefabs and complex assets
    6. Set up animations
    7. Make final adjustments to object positions, rotations, and scales
    
    Remember that complex operations might need to be broken down into smaller steps.
    """


def main():
    """Main entry point for the Unity MCP server."""
    parser = argparse.ArgumentParser(description="Unity MCP Server")
    parser.add_argument("--host", default="localhost", help="Host to bind the server to")
    parser.add_argument("--port", type=int, default=8000, help="Port to bind the server to")
    parser.add_argument("--unity-host", default="localhost", help="Unity host to connect to")
    parser.add_argument("--unity-port", type=int, default=9876, help="Unity port to connect to")
    parser.add_argument("--debug", action="store_true", help="Enable debug logging")
    
    args = parser.parse_args()
    
    if args.debug:
        logger.setLevel(logging.DEBUG)
    
    # Set the global connection parameters
    global _unity_connection
    _unity_connection = UnityConnection(host=args.unity_host, port=args.unity_port)
    
    # Create and run the MCP server
    server = FastMCP(
        title="Unity MCP",
        description="Unity integration through the Model Context Protocol",
        version=__version__,
        lifespan=server_lifespan,
    )
    
    # Run the server
    server.run(host=args.host, port=args.port)


if __name__ == "__main__":
    main() 