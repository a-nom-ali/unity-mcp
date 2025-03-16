using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace UnityMCP
{
    public class UnityMCPServer : MonoBehaviour
    {
        [Header("Server Settings")]
        public string host = "127.0.0.1";
        public int port = 9876;
        public bool autoStartOnPlay = false;

        [field: Header("Status")]
        [field: SerializeField]
        public bool IsRunning { get; private set; } = false;

        [SerializeField] private string statusMessage = "Server not started";
        [SerializeField] private int connectedClients = 0;

        private TcpListener server;
        private Thread serverThread;
        private Queue<ClientRequest> requestQueue = new Queue<ClientRequest>();
        private object queueLock = new object();
        private List<TcpClient> clients = new List<TcpClient>();
        private object clientsLock = new object();

        private class ClientRequest
        {
            public string Request { get; set; }
            public TcpClient Client { get; set; }
            public bool KeepConnectionOpen { get; set; } = true; // Add this flag to indicate whether to keep the connection open
        }

        private void Awake()
        {
            // Make sure the GameObject persists between scene loads
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartServer();
            }
        }

        public void Update()
        {
            // Process any pending requests on the main thread
            ProcessRequestQueue();
        }

        private void ProcessRequestQueue()
        {
            ClientRequest request = null;
            
            // Get a request from the queue
            lock (queueLock)
            {
                if (requestQueue.Count > 0)
                {
                    request = requestQueue.Dequeue();
                }
            }
            
            // Process the request on the main thread
            if (request != null)
            {
                try
                {
                    string response = ProcessCommand(request.Request);
                    
                    WriteToClient(response, request.Client);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing request: {e.Message}");
                    
                    // Send error response
                    string errorResponse = CreateErrorResponse($"Error processing request: {e.Message}");
                    try
                    {
                        WriteToClient(errorResponse, request.Client);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error sending error response: {ex.Message}");
                    }
                }
                
                // Only close the connection if specifically requested to do so
                // This is the key change - we're not automatically closing connections anymore
                if (!request.KeepConnectionOpen)
                {
                    try
                    {
                        request.Client.Close();
                        
                        // Remove the client from our list
                        lock (clientsLock)
                        {
                            clients.Remove(request.Client);
                            connectedClients = clients.Count;
                        }
                        Debug.Log("Client disconnected (connection closed by request)");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error closing client connection: {e.Message}");
                    }
                }
            }
        }

        private void WriteToClient(string response, TcpClient client)
        {
            try
            {
                // Send the response back to the client
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                NetworkStream stream = client.GetStream();

                stream.Write(responseBytes, 0, responseBytes.Length);
                Debug.Log($"Sent response: {response}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error writing to client: {e.Message}");
                throw; // Rethrow to handle it in the calling method
            }
        }

        private string CreateErrorResponse(string message)
        {
            var response = new Dictionary<string, object>
            {
                { "status", "error" },
                { "message", message }
            };
            
            return JsonConvert.SerializeObject(response);
        }

        private void OnDestroy()
        {
            StopServer();
        }

        public void StartServer()
        {
            if (IsRunning) return;

            try
            {
                serverThread = new Thread(ServerLoop)
                {
                    IsBackground = true
                };
                serverThread.Start();
                IsRunning = true;
                statusMessage = $"MCP Server started on {host}:{port}";
                Debug.Log(statusMessage);
            }
            catch (Exception e)
            {
                statusMessage = $"Error starting server: {e.Message}";
                Debug.LogError(statusMessage);
            }
        }

        public void StopServer()
        {
            if (!IsRunning) return;

            try
            {
                IsRunning = false;
                
                // Close the server
                if (server != null)
                {
                    server.Stop();
                }
                
                // Close all client connections
                lock (clientsLock)
                {
                    foreach (var client in clients)
                    {
                        try
                        {
                            client.Close();
                        }
                        catch (Exception) { }
                    }
                    clients.Clear();
                    connectedClients = 0;
                }
                
                // Stop the thread
                if (serverThread != null && serverThread.IsAlive)
                {
                    serverThread.Join(1000); // Wait up to 1 second
                    if (serverThread.IsAlive)
                    {
                        serverThread.Interrupt();
                    }
                }
                
                statusMessage = "Server stopped";
                Debug.Log(statusMessage);
            }
            catch (Exception e)
            {
                statusMessage = $"Error stopping server: {e.Message}";
                Debug.LogError(statusMessage);
            }
        }

        public void ServerLoop()
        {
            try
            {
                server = new TcpListener(IPAddress.Parse(host), port);
                server.Start();
                
                while (IsRunning)
                {
                    // Check for new client connections
                    if (server.Pending())
                    {
                        TcpClient client = server.AcceptTcpClient();
                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                        clientThread.IsBackground = true;
                        clientThread.Start(client);
                        
                        lock (clientsLock)
                        {
                            clients.Add(client);
                            connectedClients = clients.Count;
                        }
                        
                        Debug.Log("Client connected");
                    }
                    
                    // Small delay to prevent CPU hogging
                    Thread.Sleep(10);
                }
            }
            catch (ThreadInterruptedException)
            {
                // Thread was interrupted, just exit
            }
            catch (Exception e)
            {
                if (IsRunning) // Only log if we didn't stop intentionally
                {
                    statusMessage = $"Server error: {e.Message}";
                    Debug.LogError(statusMessage);
                }
            }
            finally
            {
                if (server != null)
                {
                    server.Stop();
                }
            }
        }

        private void HandleClientComm(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];
            StringBuilder messageBuilder = new StringBuilder();
            
            try
            {
                // Set longer timeouts to prevent premature disconnection
                client.ReceiveTimeout = 30000; // 30 seconds
                client.SendTimeout = 30000;    // 30 seconds
                
                while (IsRunning && client.Connected)
                {
                    messageBuilder.Clear();
                    int bytesRead;
                    
                    // Read data in chunks until we have a complete message
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        
                        // Check if we have a complete JSON object
                        string message = messageBuilder.ToString();
                        if (IsValidJson(message))
                        {
                            // Queue the request for processing on the main thread
                            lock (queueLock)
                            {
                                requestQueue.Enqueue(new ClientRequest
                                {
                                    Request = message,
                                    Client = client,
                                    KeepConnectionOpen = true // Keep the connection open by default
                                });
                            }

                            // Clear the buffer for the next message
                            messageBuilder.Clear();
                            break;
                        }
                    }
                    
                    // If we didn't read any bytes, the client disconnected
                    if (bytesRead == 0)
                    {
                        Debug.Log("Client sent 0 bytes - likely disconnected");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Client communication error: {e.Message}");
            }
            finally
            {
                // Clean up
                try
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                    client.Close();
                }
                catch (Exception) { }
                
                lock (clientsLock)
                {
                    clients.Remove(client);
                    connectedClients = clients.Count;
                }
                
                Debug.Log("Client disconnected");
            }
        }

        // Helper method to check if a string is valid JSON
        private bool IsValidJson(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            str = str.Trim();
            if ((str.StartsWith("{") && str.EndsWith("}")) || // For object
                (str.StartsWith("[") && str.EndsWith("]")))   // For array
            {
                try
                {
                    // Attempt to parse it with Newtonsoft.Json
                    JsonConvert.DeserializeObject(str);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            
            return false;
        }

        private string ProcessCommand(string commandJson)
        {
            try
            {
                // Parse the command
                var command = JsonConvert.DeserializeObject<CommandData>(commandJson);
                
                if (command == null || string.IsNullOrEmpty(command.type))
                {
                    return CreateErrorResponse("Command type is required");
                }
                
                Debug.Log($"Processing command: {command.type} with parameters: {command.parameters}");
                
                // Map the command to the appropriate subsystem and action
                string subsystem = "core"; // Default to core
                string action = command.type;
                
                // Check if the command has a subsystem prefix (e.g., "core.GetSystemInfo")
                if (command.type.Contains("."))
                {
                    var parts = command.type.Split(new[] { '.' }, 2);
                    subsystem = parts[0].ToLower();
                    action = parts[1];
                }
                
                // Execute the command using the brain
                return UnityMCPBrain.Instance.ExecuteCommand(subsystem, action, command.parameters);
            }
            catch (Exception e)
            {
                return CreateErrorResponse($"Error processing command: {e.Message}");
            }
        }
    }

    [Serializable]
    public class CommandData
    {
        public string type;
        public string parameters;
    }
}