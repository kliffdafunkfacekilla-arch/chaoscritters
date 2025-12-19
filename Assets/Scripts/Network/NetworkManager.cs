using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ChaosCritters.Network
{
    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NetworkManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("NetworkManager");
                        _instance = go.AddComponent<NetworkManager>();
                    }
                }
                return _instance;
            }
        }

        private const string BASE_URL = "http://127.0.0.1:8000";

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void Get(string endpoint, Action<string> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(GetRequest(endpoint, onSuccess, onError));
        }

        public void Post(string endpoint, string jsonPayload, Action<string> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(PostRequest(endpoint, jsonPayload, onSuccess, onError));
        }

        private IEnumerator GetRequest(string endpoint, Action<string> onSuccess, Action<string> onError)
        {
            string url = BASE_URL + endpoint;
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error: {webRequest.error}");
                    onError?.Invoke(webRequest.error);
                }
                else
                {
                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                }
            }
        }

        private IEnumerator PostRequest(string endpoint, string jsonPayload, Action<string> onSuccess, Action<string> onError)
        {
            string url = BASE_URL + endpoint;
            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error: {webRequest.error}");
                    onError?.Invoke(webRequest.error);
                }
                else
                {
                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                }
            }
        }
    }
}
