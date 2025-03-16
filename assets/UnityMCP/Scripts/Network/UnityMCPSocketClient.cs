using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMCP
{
    public class UnityMCPSocketServer : MonoBehaviour
    {
        [SerializeField] private string host = "127.0.0.1";
        [SerializeField] private int port = 9876;
        
        private TcpListener server;
        private Thread serverThread;
        private bool isRunning = false;
        private Queue<ClientRequest> requestQueue = new Queue<ClientRequest>();
        private object queueLock = new object();
        
        private class ClientRequest
        {
            public string Request { get; set; }
            public TcpClient Client { get; set; }
            public NetworkStream Stream { get; set; }
        }
        
        private void Start()
        {
            // Start the server
            StartServer();
        }
        
        private void Update()
        {
            // Process any pending requests on the main thread
            ProcessRequestQueue();
        }
        
        private void OnDestroy()
        {
            StopServer();
        }
        
        private void StartServer()
        {
            if (isRunning) return;
            
            try
            {
                // Create and start the server thread
                serverThread = new Thread(new ThreadStart(ServerLoop));
                serverThread.IsBackground = true;
                isRunning = true;
                serverThread.Start();
                
                Debug.Log($"MCP Socket Server started on {host}:{port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error starting MCP Socket Server: {e.Message}");
            }
        }
        
        private void StopServer()
        {
            isRunning = false;
            
            if (server != null)
            {
                server.Stop();
                server = null;
            }
            
            if (serverThread != null && serverThread.IsAlive)
            {
                try
                {
                    serverThread.Abort();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error aborting server thread: {e.Message}");
                }
                serverThread = null;
            }
            
            Debug.Log("MCP Socket Server stopped");
        }
        
        private void ServerLoop()
        {
            try
            {
                // Create the TCP listener
                server = new TcpListener(IPAddress.Parse(host), port);
                server.Start();
                
                Debug.Log($"Server listening on {host}:{port}");
                
                // Accept client connections
                while (isRunning)
                {
                    if (server.Pending())
                    {
                        // Accept the client connection
                        TcpClient client = server.AcceptTcpClient();
                        
                        // Handle the client in a new thread
                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                        clientThread.IsBackground = true;
                        clientThread.Start(client);
                    }
                    
                    Thread.Sleep(10);
                }
            }
            catch (ThreadAbortException)
            {
                Debug.Log("Server thread aborted");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in server loop: {e.Message}");
            }
            finally
            {
                if (server != null)
                {
                    server.Stop();
                }
            }
        }
        
        private void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            NetworkStream stream = null;
            
            try
            {
                Debug.Log("Client connected");
                
                stream = client.GetStream();
                byte[] buffer = new byte[8192];
                
                // Read the client request
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                Debug.Log($"Received request: {request}");
                
                // Queue the request for processing on the main thread
                lock (queueLock)
                {
                    requestQueue.Enqueue(new ClientRequest
                    {
                        Request = request,
                        Client = client,
                        Stream = stream
                    });
                }
                
                // Note: We don't close the client or stream here
                // They will be closed after the response is sent
            }
            catch (Exception e)
            {
                Debug.LogError($"Error handling client: {e.Message}");
                
                if (stream != null)
                {
                    stream.Close();
                }
                
                client.Close();
            }
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
                    
                    // Send the response back to the client
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    request.Stream.Write(responseBytes, 0, responseBytes.Length);
                    Debug.Log($"Sent response: {response}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing request: {e.Message}");
                    
                    // Send error response
                    string errorResponse = CreateErrorResponse($"Error processing request: {e.Message}");
                    byte[] errorBytes = Encoding.UTF8.GetBytes(errorResponse);
                    try
                    {
                        request.Stream.Write(errorBytes, 0, errorBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error sending error response: {ex.Message}");
                    }
                }
                finally
                {
                    // Close the stream and client
                    try
                    {
                        request.Stream.Close();
                        request.Client.Close();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error closing client connection: {e.Message}");
                    }
                }
            }
        }
        
        private string ProcessCommand(string commandJson)
        {
            try
            {
                // Parse the command JSON
                JObject commandObj = JObject.Parse(commandJson);
                
                // Extract command type and parameters
                string commandType = commandObj["type"]?.ToString();
                string parametersJson = commandObj["parameters"]?.ToString() ?? "{}";
                
                if (string.IsNullOrEmpty(commandType))
                {
                    return CreateErrorResponse("Command type is required");
                }
                
                Debug.Log($"Processing command: {commandType} with parameters: {parametersJson}");
                
                // Map the command to the appropriate subsystem and action
                string subsystem = "core"; // Default to core
                string action = commandType;
                
                // Check if the command has a subsystem prefix (e.g., "core.GetSystemInfo")
                if (commandType.Contains("."))
                {
                    var parts = commandType.Split(new[] { '.' }, 2);
                    subsystem = parts[0].ToLower();
                    action = parts[1];
                }
                
                // Execute the command using the brain
                string result = UnityMCPBrain.Instance.ExecuteCommand(subsystem, action, parametersJson);
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing command: {e.Message}\n{e.StackTrace}");
                return CreateErrorResponse($"Error processing command: {e.Message}");
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
    }
}