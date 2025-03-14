using UnityEngine;
using UnityEditor;
using UnityMCP;

namespace UnityMCPEditor
{
    [CustomEditor(typeof(UnityMCPServer))]
    public class UnityMCPServerEditor : Editor
    {
        private bool showAdvancedSettings = false;

        public override void OnInspectorGUI()
        {
            UnityMCPServer server = (UnityMCPServer)target;

            // Server status
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unity MCP Server", EditorStyles.boldLabel);
            
            // Display server status with color
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            if (server.IsRunning)
            {
                statusStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField("Status: Running", statusStyle);
            }
            else
            {
                statusStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField("Status: Stopped", statusStyle);
            }
            
            EditorGUILayout.Space();
            
            // Server controls
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !server.IsRunning;
            if (GUILayout.Button("Start Server", GUILayout.Height(30)))
            {
                server.StartServer();
            }
            
            GUI.enabled = server.IsRunning;
            if (GUILayout.Button("Stop Server", GUILayout.Height(30)))
            {
                server.StopServer();
            }
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Server settings
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Server Settings");
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                // Only allow editing when server is not running
                GUI.enabled = !server.IsRunning;
                
                server.host = EditorGUILayout.TextField("Host", server.host);
                server.port = EditorGUILayout.IntField("Port", server.port);
                server.autoStartOnPlay = EditorGUILayout.Toggle("Auto Start on Play", server.autoStartOnPlay);
                
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Help box
            EditorGUILayout.HelpBox(
                "The Unity MCP Server allows Claude AI to interact with your Unity scene through the Model Context Protocol.\n\n" +
                "1. Start the server in Unity\n" +
                "2. Configure the MCP server in Claude\n" +
                "3. Use Claude to control your Unity scene", 
                MessageType.Info
            );
            
            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }
    }

    // Add menu item to create the server
    public class UnityMCPMenu
    {
        [MenuItem("Tools/Unity MCP/Create Server")]
        public static void CreateServer()
        {
            // Check if server already exists
            UnityMCPServer existingServer = Object.FindObjectOfType<UnityMCPServer>();
            if (existingServer != null)
            {
                EditorUtility.DisplayDialog("Unity MCP", "A Unity MCP Server already exists in the scene.", "OK");
                Selection.activeGameObject = existingServer.gameObject;
                return;
            }
            
            // Create a new GameObject with the server component
            GameObject serverObject = new GameObject("UnityMCPServer");
            serverObject.AddComponent<UnityMCPServer>();
            
            // Select the new object
            Selection.activeGameObject = serverObject;
            
            EditorUtility.DisplayDialog("Unity MCP", "Unity MCP Server created successfully!", "OK");
        }
    }
} 