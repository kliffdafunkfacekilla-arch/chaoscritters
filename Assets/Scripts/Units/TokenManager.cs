using System.Collections.Generic;
using UnityEngine;
using ChaosCritters.Network;
using ChaosCritters.Data;

namespace ChaosCritters.Units
{
    public class TokenManager : MonoBehaviour
    {
        public static TokenManager Instance { get; private set; }

        [Header("Configuration")]
        public GameObject tokenPrefab; // User drags "Token" prefab here

        private Dictionary<string, TokenController> _activeTokens = new Dictionary<string, TokenController>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (tokenPrefab == null)
            {
                Debug.LogError("TokenManager: No Token Prefab assigned!");
                return;
            }
            // Add a small delay to ensure backend is ready/connected
            Invoke(nameof(RefreshEntities), 1.0f);
        }

        [ContextMenu("Refresh Entities")]
        public void RefreshEntities()
        {
            // First, ensure we start a battle so entities actually exist on the server
            // (In a real game, this would happen at a specific trigger, but for now we force it)
            NetworkManager.Instance.Post("/battle/start", "{}", 
                onSuccess: (json) => FetchEntityList(),
                onError: (err) => Debug.LogError($"Failed to start battle: {err}")
            );
        }

        private void FetchEntityList()
        {
            NetworkManager.Instance.Get("/entities", 
                onSuccess: (json) => 
                {
                    EntityListResponse response = JsonUtility.FromJson<EntityListResponse>(json);
                    SyncTokens(response.entities);
                },
                onError: (err) => Debug.LogError($"Failed to fetch entities: {err}")
            );
        }

        private void SyncTokens(EntityData[] entities)
        {
            // 1. Mark all for potential deletion
            HashSet<string> seenIds = new HashSet<string>();

            foreach (var data in entities)
            {
                seenIds.Add(data.id);

                if (_activeTokens.ContainsKey(data.id))
                {
                    // Update existing
                    _activeTokens[data.id].MoveTo(data.x, data.y);
                }
                else
                {
                    // Create new
                    GameObject go = Instantiate(tokenPrefab);
                    TokenController controller = go.GetComponent<TokenController>();
                    if (controller == null) controller = go.AddComponent<TokenController>();
                    
                    controller.Initialize(data);
                    _activeTokens.Add(data.id, controller);
                }
            }

            // 2. Remove tokens that don't exist anymore
            List<string> toRemove = new List<string>();
            foreach (var kvp in _activeTokens)
            {
                if (!seenIds.Contains(kvp.Key))
                {
                    Destroy(kvp.Value.gameObject);
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var id in toRemove) _activeTokens.Remove(id);
        }

        public void RequestMove(string actorId, int targetX, int targetY)
        {
            // Construct JSON manually to avoid creating another class file just for this
            string json = $"{{\"actor_id\": \"{actorId}\", \"target_pos\": [{targetX}, {targetY}]}}";
            
            Debug.Log($"Requesting Move: {json}");

            NetworkManager.Instance.Post("/battle/action/move", json,
                onSuccess: (response) => 
                {
                    Debug.Log("Move Approved by Backend.");
                    RefreshEntities(); // Re-sync positions
                },
                onError: (err) => Debug.LogError($"Move Rejected: {err}")
            );
        }
    }
}
