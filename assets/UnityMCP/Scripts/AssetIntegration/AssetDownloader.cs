using UnityEngine;
using System;
using System.Collections;
using System.IO;
using UnityEngine.Networking;

namespace UnityMCP
{
    public class AssetDownloader : MonoBehaviour
    {
        private static AssetDownloader instance;

        public static AssetDownloader Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("AssetDownloader");
                    instance = go.AddComponent<AssetDownloader>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public IEnumerator DownloadTexture(string url, Action<Texture2D> onComplete, Action<string> onError)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Failed to download texture: {www.error}");
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    onComplete?.Invoke(texture);
                }
            }
        }

        public IEnumerator DownloadAssetBundle(string url, Action<AssetBundle> onComplete, Action<string> onError)
        {
            using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(url))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Failed to download asset bundle: {www.error}");
                }
                else
                {
                    AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                    onComplete?.Invoke(bundle);
                }
            }
        }

        public IEnumerator DownloadFile(string url, string savePath, Action<string> onComplete, Action<string> onError)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Failed to download file: {www.error}");
                }
                else
                {
                    try
                    {
                        // Ensure directory exists
                        string directory = Path.GetDirectoryName(savePath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Save the file
                        File.WriteAllBytes(savePath, www.downloadHandler.data);
                        onComplete?.Invoke(savePath);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Failed to save file: {e.Message}");
                    }
                }
            }
        }

        public IEnumerator DownloadJson<T>(string url, Action<T> onComplete, Action<string> onError)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("Content-Type", "application/json");
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Failed to download JSON: {www.error}");
                }
                else
                {
                    try
                    {
                        string jsonText = www.downloadHandler.text;
                        T data = JsonUtility.FromJson<T>(jsonText);
                        onComplete?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Failed to parse JSON: {e.Message}");
                    }
                }
            }
        }
    }
} 