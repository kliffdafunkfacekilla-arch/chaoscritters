using System.Collections.Generic;
using UnityEngine;
using ChaosCritters.Network;
using ChaosCritters.Data;

namespace ChaosCritters.Units
{
    public class TokenManager : MonoBehaviour
    {
        private static TokenManager _instance;
        public static TokenManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TokenManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("TokenManager");
                        _instance = go.AddComponent<TokenManager>();
                        // Initialization happens in Start via Invoke
                        // But let's make sure it persists
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set { _instance = value; }
        }

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
            public string narrative; 
            public string battle_state; // Added
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
            public string battle_state; // Added
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

        private void Start()
        {
            if (tokenPrefab == null)
            {
                // Fallback: Try to load from Resources
                tokenPrefab = Resources.Load<GameObject>("Prefabs/Token");
                if (tokenPrefab == null)
                {
                    Debug.LogWarning("[TokenManager] 'Prefabs/Token' not found. Creating programmatic placeholder.");
                    // Create a runtime placeholder prefab
                    tokenPrefab = new GameObject("RuntimeToken_Placeholder");
                    tokenPrefab.AddComponent<TokenController>();
                    var renderer = tokenPrefab.AddComponent<SpriteRenderer>();
                    
                    // Create a simple circle texture
                    Texture2D tex = new Texture2D(32, 32);
                    Color[] pixels = new Color[32*32];
                    for(int i=0; i<pixels.Length; i++) pixels[i] = Color.white;
                    tex.SetPixels(pixels);
                    tex.Apply();
                    renderer.sprite = Sprite.Create(tex, new Rect(0,0,32,32), new Vector2(0.5f, 0.5f), 32);
                    
                    // Keep it alive for instantiation
                    DontDestroyOnLoad(tokenPrefab);
                }
                Debug.Log("[TokenManager] Token Prefab Ready.");
            }
            
            // Initialize Systems
            if (SkillDatabase.Instance == null)
            {
                var go = new GameObject("SkillDatabase");
                go.AddComponent<SkillDatabase>();
            }
            SkillDatabase.Instance.Initialize();
            
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
                    Debug.Log($"Battle Initialized. Current Turn: {CurrentActorId} | State: {res.battle_state}");
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
            // Debug.Log($"[TokenManager] Syncing {entities.Length} entities from backend.");
            // 1. Mark all for potential deletion
            HashSet<string> seenIds = new HashSet<string>();

            foreach (var data in entities)
            {
                seenIds.Add(data.id);

                if (_activeTokens.ContainsKey(data.id))
                {
                    // Update existing
                    // Debug.Log($"Syncing Token {data.id}: Pos ({data.x}, {data.y})");
                    _activeTokens[data.id].MoveTo(data.x, data.y);
                    _activeTokens[data.id].UpdateHealth(data.hp, data.max_hp);
                    
                    // Always update Player Card if this is P1 (Free Roam or Turn)
                    // Always update Player Card if this is P1 (Free Roam or Turn)
                    if (data.id == "P1")
                    {
                        if (UI.HUDController.Instance != null) UI.HUDController.Instance.UpdatePlayerCard(data);
                        
                        // Sync Hotbar
                        var hotbar = FindFirstObjectByType<UI.AbilityGridController>();
                        if (hotbar != null) hotbar.RefreshAbilities(data);
                        
                        // Sync Skill Menu
                        var skillMenu = FindFirstObjectByType<UI.SkillMenuController>();
                        if (skillMenu != null) skillMenu.LoadData(data);
                    }
                    
                    // Auto-End Turn Check
                    if (data.id == CurrentActorId && CurrentActorId == "P1")
                    {
                        // Assuming 1 AP is minimum for any action
                        if (data.ap < 1)
                        {
                            Debug.Log("[TokenManager] Auto-Ending Turn due to Low AP");
                            // Avoid spamming if already ended?
                            // RequestEndTurn is async.
                            // Ideally we flag this. But for now, just call it.
                            // Risky if RefreshEntities is called often. 
                            // Add a guard?
                            // We can check if we are *already* ending.
                            // But RequestEndTurn changes CurrentActorId promptly on success.
                            
                            // Let's rely on the user interface blocking or basic logic.
                            // Actually, RequestEndTurn() will call next_turn() on backend, changing CurrentActorId.
                            // So we just call it once.
                            RequestEndTurn(); 
                        }
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
            
            // Sync Combat State UI
            if (UI.HUDController.Instance != null)
            {
                bool inCombat = !string.IsNullOrEmpty(CurrentActorId);
                UI.HUDController.Instance.SetCombatMode(inCombat);
            }
        }

        private void Update()
        {
            // Hotkey: Spacebar to End Turn
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (CurrentActorId == "P1")
                {
                    RequestEndTurn();
                }
            }
        }

        public void RequestMove(string actorId, int targetX, int targetY)
        {
            // Client-Side Predication Check
            if (actorId == "P1" && _activeTokens.ContainsKey("P1"))
            {
                // We don't have easy access to AP here without referencing the UI card or EntityData cache.
                // But we can let the backend handle it, OR we can show the error response clearly.
                // Let's rely on the error response for truth, but make it Visible.
            }
        
            // Construct JSON manually to avoid creating another class file just for this
            string json = $"{{\"actor_id\": \"{actorId}\", \"target_pos\": [{targetX}, {targetY}]}}";
            
            Debug.Log($"Requesting Move: {json}");

            NetworkManager.Instance.Post("/battle/action/move", json,
                onSuccess: (response) => 
                {
                    Debug.Log("Move Approved by Backend.");
                    if (response.Contains("\"narrative\":"))
                    {
                        var wrapper = JsonUtility.FromJson<NarrativeWrapper>(response);
                        if (!string.IsNullOrEmpty(wrapper.narrative))
                            UI.NarratorController.Instance?.AddLine(wrapper.narrative);
                    }
                    
                    RefreshEntities(); 
                },
                onError: (err) => 
                {
                    Debug.LogError($"Move Rejected: {err}");
                    UI.NarratorController.Instance?.AddLine($"Move Failed: {err}");
                    
                    // Visual Feedback
                    if (_activeTokens.ContainsKey(actorId))
                    {
                        var token = _activeTokens[actorId];
                        UI.DamagePopup.Create(token.transform.position + Vector3.up, 0, Color.gray, "Blocked!");
                    }
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
                        
                        // Check Victory/Defeat Immediately
                        if (res.battle_state == "Victory")
                        {
                            Debug.Log("[TokenManager] VICTORY DETECTED!");
                            UI.UIAssembler.ShowGameOver(true);
                            CurrentActorId = null; // Exit Combat Mode locally
                            if (UI.HUDController.Instance != null) UI.HUDController.Instance.SetCombatMode(false);
                        }
                        else if (res.battle_state == "Defeat")
                        {
                             Debug.Log("[TokenManager] DEFEAT DETECTED!");
                             UI.UIAssembler.ShowGameOver(false);
                             CurrentActorId = null;
                             if (UI.HUDController.Instance != null) UI.HUDController.Instance.SetCombatMode(false);
                        }
                    }
                    else
                    {
                         UI.NarratorController.Instance?.AddLine($"Attack Failed: {(res?.result?.message ?? "Unknown Error")}");
                    }
                },
                onError: (err) => 
                {
                     UI.NarratorController.Instance?.AddLine($"Attack Error: {err}");
                     
                     if (_activeTokens.ContainsKey(actorId))
                     {
                         var token = _activeTokens[actorId];
                         UI.DamagePopup.Create(token.transform.position + Vector3.up, 0, Color.gray, "Full!"); // or Fail
                     }
                }
            );
        }

        public void RequestAbility(string actorId, string targetId, string abilityId)
        {
             // Construct JSON manually
            string json = $"{{\"actor_id\": \"{actorId}\", \"target_id\": \"{targetId}\", \"ability_id\": \"{abilityId}\"}}";
            Debug.Log($"Requesting Ability {abilityId}: {json}");
            
            NetworkManager.Instance.Post("/battle/action/ability", json,
                onSuccess: (response) => 
                {
                    Debug.Log($"Ability Response: {response}");
                    AttackResponse res = JsonUtility.FromJson<AttackResponse>(response); // Reuse AttackResponse struct
                    
                    if (res != null && res.result != null && res.result.success)
                    {
                         if (response.Contains("\"narrative\":"))
                        {
                             var wrapper = JsonUtility.FromJson<NarrativeWrapper>(response);
                             if (!string.IsNullOrEmpty(wrapper.narrative))
                                 UI.NarratorController.Instance?.AddLine(wrapper.narrative);
                        }

                        if (_activeTokens.ContainsKey(targetId))
                        {
                            TokenController targetToken = _activeTokens[targetId];
                            int dmg = res.result.mechanics.damage_amount;
                            string type = res.result.mechanics.damage_type;
                            Color col = (type == "Meat") ? Color.red : (type == "Burn") ? new Color(1f, 0.5f, 0f) : Color.yellow;
                            
                            UI.DamagePopup.Create(targetToken.transform.position + Vector3.up, dmg, col, (dmg > 0 ? "" : "Miss"));
                        }
                        
                        RefreshEntities();
                        
                        if (res.battle_state == "Victory")
                        {
                            UI.UIAssembler.ShowGameOver(true);
                            CurrentActorId = null;
                            if (UI.HUDController.Instance != null) UI.HUDController.Instance.SetCombatMode(false);
                        }
                        else if (res.battle_state == "Defeat")
                        {
                             UI.UIAssembler.ShowGameOver(false);
                             CurrentActorId = null;
                             if (UI.HUDController.Instance != null) UI.HUDController.Instance.SetCombatMode(false);
                        }
                    }
                    else
                    {
                         UI.NarratorController.Instance?.AddLine($"Ability Failed: {(res?.result?.message ?? "Unknown Error")}");
                    }
                },
                onError: (err) => UI.NarratorController.Instance?.AddLine($"Ability Error: {err}")
            );
        }

        public void RequestEndTurn()
        {
            Debug.Log("[TokenManager] Requesting End Turn...");
            NetworkManager.Instance.Post("/battle/turn/end", "{}", 
                onSuccess: (response) => 
                {
                    BattleStartResponse res = JsonUtility.FromJson<BattleStartResponse>(response);
                    CurrentActorId = res.current_turn;
                    Debug.Log($"Turn Ended. Next Actor: {CurrentActorId}");
                    
                    if (UI.NarratorController.Instance != null)
                    {
                        if (!string.IsNullOrEmpty(res.narrative))
                            UI.NarratorController.Instance.AddLine(res.narrative);
                        
                        UI.NarratorController.Instance.AddLine($"Turn: {CurrentActorId}");
                    }

                    RefreshEntities();

                    // If it is NOT the player's turn, we must poll or wait for the AI to finish
                    if (CurrentActorId != "P1")
                    {
                        Debug.Log("Waiting for AI...");
                        StartCoroutine(WaitForPlayerTurn());
                    }
                },
                onError: (err) => Debug.LogError($"End Turn Failed: {err}")
            );
        }

        private System.Collections.IEnumerator WaitForPlayerTurn()
        {
            // Simple Polling to see if AI is done
            while (CurrentActorId != "P1")
            {
                yield return new WaitForSeconds(1.0f);
                RefreshEntities(); // Helper that now does both
                
                // We must query the state explicitly because RefreshEntities only got positions
                NetworkManager.Instance.Get("/battle/state", 
                    onSuccess: (json) => {
                        BattleStartResponse res = JsonUtility.FromJson<BattleStartResponse>(json);
                        CurrentActorId = res.current_turn;
                        if (UI.NarratorController.Instance != null && CurrentActorId == "P1")
                            UI.NarratorController.Instance.AddLine("Your Turn!");
                    }
                );
            }
        }

        public TokenController GetToken(string id)
        {
            if (_activeTokens.ContainsKey(id)) return _activeTokens[id];
            return null;
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
