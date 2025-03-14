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
            using (UnityWebRequest www = Un
    }
} 