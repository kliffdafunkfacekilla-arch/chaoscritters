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
                    _instance = FindFirstObjectByType<NetworkManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("NetworkManager");
                        _instance = go.AddComponent<NetworkManager>();
                    }
                }
                return _instance;
            }
        }

        private const string BASE_URL = "http://localhost:8000";

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Ensure EventSystem exists for UI
                if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject es = new GameObject("EventSystem");
                    es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    Debug.Log("[NetworkManager] Auto-created missing EventSystem.");
                }
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

        // Global Retry Settings
        private const int MAX_RETRIES = 5;
        private const float RETRY_DELAY = 1.0f;

        private IEnumerator GetRequest(string endpoint, Action<string> onSuccess, Action<string> onError)
        {
            string url = BASE_URL + endpoint;
            int attempts = 0;

            while (attempts <= MAX_RETRIES)
            {
                using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
                {
                    yield return webRequest.SendWebRequest();

                    if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                    {
                        attempts++;
                        if (attempts > MAX_RETRIES)
                        {
                            Debug.LogError($"[Network] Connection failed after {attempts} attempts: {webRequest.error}");
                            onError?.Invoke(webRequest.error);
                            break;
                        }
                        
                        Debug.LogWarning($"[Network] Connection failed ({webRequest.error}). Retrying in {RETRY_DELAY}s... ({attempts}/{MAX_RETRIES})");
                        yield return new WaitForSeconds(RETRY_DELAY);
                        continue; // Retry
                    }
                    
                    if (webRequest.result == UnityWebRequest.Result.ProtocolError)
                    {
                        Debug.LogError($"[Network] Protocol Error: {webRequest.error}");
                        onError?.Invoke(webRequest.error);
                        break;
                    }

                    // Success
                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                    break;
                }
            }
        }

        private IEnumerator PostRequest(string endpoint, string jsonPayload, Action<string> onSuccess, Action<string> onError)
        {
            string url = BASE_URL + endpoint;
            int attempts = 0;

            while (attempts <= MAX_RETRIES)
            {
                using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    webRequest.SetRequestHeader("Content-Type", "application/json");

                    yield return webRequest.SendWebRequest();

                    if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                    {
                        attempts++;
                        if (attempts > MAX_RETRIES)
                        {
                            Debug.LogError($"[Network] Connection failed after {attempts} attempts: {webRequest.error}");
                            onError?.Invoke(webRequest.error);
                            break;
                        }

                        Debug.LogWarning($"[Network] Connection failed ({webRequest.error}). Retrying in {RETRY_DELAY}s... ({attempts}/{MAX_RETRIES})");
                        yield return new WaitForSeconds(RETRY_DELAY);
                         continue; // Retry
                    }

                    if (webRequest.result == UnityWebRequest.Result.ProtocolError)
                    {
                        Debug.LogError($"[Network] Protocol Error: {webRequest.error}\nResponse: {webRequest.downloadHandler.text}");
                        onError?.Invoke(webRequest.error);
                        break;
                    }

                    // Success
                    onSuccess?.Invoke(webRequest.downloadHandler.text);
                    break;
                }
            }
        }
    }
}
