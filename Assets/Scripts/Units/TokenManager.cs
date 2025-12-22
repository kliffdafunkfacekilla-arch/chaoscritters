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
        
        public string CurrentActorId { get; private set; }

        private Dictionary<string, TokenController> _activeTokens = new Dictionary<string, TokenController>();
        
        [System.Serializable]
        public class BattleStartResponse
        {
            public string message;
            public string[] turn_order;
            public string current_turn;
        }

        [System.Serializable]
        public class NarrativeWrapper
        {
            public string narrative;
            public string message;
        }

        [System.Serializable]
        public class AttackResponse
        {
            public ActionResult result;
            public int attacker_ap;
        }

        [System.Serializable]
        public class ActionResult
        {
            public bool success;
            public string message; // Error msg
            public MechanicsResult mechanics;
        }

        [System.Serializable]
        public class MechanicsResult
        {
            public int damage_amount;
            public string damage_type;
        }

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
            Invoke(nameof(InitializeBattle), 1.0f);
        }

        private void InitializeBattle()
        {
            NetworkManager.Instance.Post("/battle/start", "{}", 
                onSuccess: (json) => 
                {
                    BattleStartResponse res = JsonUtility.FromJson<BattleStartResponse>(json);
                    CurrentActorId = res.current_turn;
                    Debug.Log($"Battle Initialized. Current Turn: {CurrentActorId}");
                    RefreshEntities();
                },
                onError: (err) => Debug.LogError($"Failed to start battle: {err}")
            );
        }

        [ContextMenu("Refresh Entities")]
        public void RefreshEntities()
        {
            FetchEntityList();
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
                    Debug.Log($"Syncing Token {data.id}: Pos ({data.x}, {data.y})");
                    _activeTokens[data.id].MoveTo(data.x, data.y);
                    
                    if (data.id == CurrentActorId && UI.HUDController.Instance != null)
                    {
                        UI.HUDController.Instance.UpdatePlayerCard(data);
                    }
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
                    // Check for narrative/messages
                    // Simple manual parse for now
                    if (response.Contains("\"narrative\":"))
                    {
                        var wrapper = JsonUtility.FromJson<NarrativeWrapper>(response);
                        if (!string.IsNullOrEmpty(wrapper.narrative))
                            UI.NarratorController.Instance?.AddLine(wrapper.narrative);
                    }
                    
                    RefreshEntities(); // Re-sync positions
                },
                onError: (err) => 
                {
                    Debug.LogError($"Move Rejected: {err}");
                    UI.NarratorController.Instance?.AddLine($"Move Failed: {err}");
                }
            );
        }

        public void RequestAttack(string actorId, string targetId)
        {
             // Construct JSON manually
            string json = $"{{\"actor_id\": \"{actorId}\", \"target_id\": \"{targetId}\"}}";
            Debug.Log($"Requesting Attack: {json}");
            
            NetworkManager.Instance.Post("/battle/action/attack", json,
                onSuccess: (response) => 
                {
                    Debug.Log($"Attack Response: {response}"); // Debug log to see full JSON
                    
                    // Parse Response
                    AttackResponse res = JsonUtility.FromJson<AttackResponse>(response);
                    
                    if (res != null && res.result != null && res.result.success)
                    {
                        // Show Narrative
                        if (response.Contains("\"narrative\":"))
                        {
                             var wrapper = JsonUtility.FromJson<NarrativeWrapper>(response);
                             if (!string.IsNullOrEmpty(wrapper.narrative))
                                 UI.NarratorController.Instance?.AddLine(wrapper.narrative);
                        }

                        // Show Damage Popup
                        if (_activeTokens.ContainsKey(targetId))
                        {
                            TokenController targetToken = _activeTokens[targetId];
                            int dmg = res.result.mechanics.damage_amount;
                            string type = res.result.mechanics.damage_type;
                            
                            if (dmg > 0)
                            {
                                Color col = (type == "Meat") ? Color.red : Color.yellow;
                                UI.DamagePopup.Create(targetToken.transform.position + Vector3.up, dmg, col);
                            }
                            else
                            {
                                UI.DamagePopup.Create(targetToken.transform.position + Vector3.up, 0, Color.gray);
                            }
                        }
                        
                        RefreshEntities();
                    }
                    else
                    {
                         UI.NarratorController.Instance?.AddLine($"Attack Failed: {(res?.result?.message ?? "Unknown Error")}");
                    }
                },
                onError: (err) => 
                {
                     UI.NarratorController.Instance?.AddLine($"Attack Error: {err}");
                }
            );
        }

        public string GetTokenAt(int x, int y)
        {
            foreach(var kvp in _activeTokens)
            {
                // We access the controller's transform, but better to check data if we had it.
                // Or simply check coords. Transform is float, so we cast.
                Vector3 pos = kvp.Value.transform.position;
                // We check against FloorToInt because we shifted visual pos by +0.5f
                // e.g. (2.5, 3.5) -> Cell (2, 3)
                if (Mathf.FloorToInt(pos.x) == x && Mathf.FloorToInt(pos.y) == y)
                {
                    return kvp.Key;
                }
            }
            return null;
        }
    }
}
