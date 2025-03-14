using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

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
        private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private CommandHandler commandHandler;
        private List<TcpClient> clients = new List<TcpClient>();
        private object clientsLock = new object();

        private void Awake()
        {
            // Make sure the GameObject persists between scene loads
            DontDestroyOnLoad(this.gameObject);
            
            // Initialize the command handler
            commandHandler = new CommandHandler();
        }

        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartServer();
            }
        }

        private void Update()
        {
            // Execute any queued actions on the main thread
            while (mainThreadActions.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error executing action on main thread: {e.Message}");
                }
            }
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
                serverThread = new Thread(new ThreadStart(ServerLoop));
                serverThread.IsBackground = true;
                serverThread.Start();
                IsRunning = true;
                statusMessage = $"Server started on {host}:{port}";
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

        private void ServerLoop()
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
                    mainThreadActions.Enqueue(() => {
                        statusMessage = $"Server error: {e.Message}";
                        Debug.LogError(statusMessage);
                    });
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
                        if (JsonUtility.IsValidJson(message))
                        {
                            // Process the command
                            string response = ProcessCommand(message);
                            
                            // Send the response
                            byte[] responseData = Encoding.UTF8.GetBytes(response);
                            stream.Write(responseData, 0, responseData.Length);
                            
                            // Clear the buffer for the next message
                            messageBuilder.Clear();
                            break;
                        }
                    }
                    
                    // If we didn't read any bytes, the client disconnected
                    if (bytesRead == 0)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                mainThreadActions.Enqueue(() => {
                    Debug.LogWarning($"Client communication error: {e.Message}");
                });
            }
            finally
            {
                // Clean up
                try
                {
                    stream.Close();
                    client.Close();
                }
                catch (Exception) { }
                
                lock (clientsLock)
                {
                    clients.Remove(client);
                    connectedClients = clients.Count;
                }
                
                mainThreadActions.Enqueue(() => {
                    Debug.Log("Client disconnected");
                });
            }
        }

        private string ProcessCommand(string commandJson)
        {
            try
            {
                // Parse the command
                var command = JsonUtility.FromJson<CommandData>(commandJson);
                
                // Execute the command on the main thread and wait for the result
                string result = null;
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                
                mainThreadActions.Enqueue(() => {
                    try
                    {
                        result = commandHandler.ExecuteCommand(command);
                    }
                    catch (Exception e)
                    {
                        result = JsonUtility.CreateErrorResponse($"Error executing command: {e.Message}");
                    }
                    finally
                    {
                        waitHandle.Set();
                    }
                });
                
                // Wait for the command to be executed
                waitHandle.WaitOne();
                
                return result;
            }
            catch (Exception e)
            {
                return JsonUtility.CreateErrorResponse($"Error processing command: {e.Message}");
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