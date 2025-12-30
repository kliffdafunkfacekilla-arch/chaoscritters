using UnityEngine;
using ChaosCritters.Data;
using System.Collections.Generic;

namespace ChaosCritters.UI
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("Active Player Card")]
        public Transform playerCardPanel;
        public StatBar healthBar;
        public StatBar staminaBar;
        public StatBar focusBar;
        public UnityEngine.UI.Text nameText;
        public UnityEngine.UI.Text classText;

        [Header("Status Icons")]
        public Transform iconContainer;
        public GameObject iconPrefab; // Image with sprite
        
        // Clash UI
        // Clash UI
        public GameObject clashPanel;
        public ClashUIController clashController;

        public void OnClashCardClicked(string card)
        {
            if (clashController != null) clashController.OnCardSelected(card);
        }

        public void ShowClashUI(string attackerId, string defenderId)
        {
            if (clashController != null)
            {
                clashController.Show(attackerId, defenderId);
            }
            else
            {
                Debug.LogWarning("[HUD] Clash UI Controller not assigned!");
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            // Auto-Build Infrastructure if missing elements
            if (playerCardPanel == null || healthBar == null)
            {
                UIAssembler.VerifyHUD();
            }
            
            // Default state
            if (playerCardPanel != null) playerCardPanel.gameObject.SetActive(false);
        }

        public void UpdatePlayerCard(EntityData data)
        {
            if (data == null)
            {
                if (playerCardPanel != null) playerCardPanel.gameObject.SetActive(false);
                return;
            }

            if (playerCardPanel != null) playerCardPanel.gameObject.SetActive(true);

            // Text
            if (nameText != null) nameText.text = data.name;
            if (classText != null) 
            {
                if (!string.IsNullOrEmpty(data.lineage) && !string.IsNullOrEmpty(data.background))
                    classText.text = $"{data.lineage} / {data.background}";
                else if (data.visual_tags != null && !string.IsNullOrEmpty(data.visual_tags.role))
                     // Fallback to legacy if new data missing
                    classText.text = $"{data.visual_tags.role} / {data.visual_tags.infusion}";
                else
                    classText.text = "Wanderer";
            }

            // Bars
            if (healthBar != null) healthBar.SetValues(data.hp, data.max_hp);
            if (staminaBar != null) staminaBar.SetValues(data.stamina, data.max_stamina);
            if (focusBar != null) focusBar.SetValues(data.focus, data.max_focus);

            // Status
            RefreshStatusEffects(data.status_effects);
        }

        private void RefreshStatusEffects(string[] effects)
        {
            if (iconContainer == null) return;

            // Clear existing
            foreach (Transform child in iconContainer)
            {
                Destroy(child.gameObject);
            }

            if (effects == null || effects.Length == 0) return;

            // Create new
            foreach (string effectId in effects)
            {
                if (string.IsNullOrEmpty(effectId)) continue;
                
                GameObject iconObj = null;
                if (iconPrefab != null) iconObj = Instantiate(iconPrefab, iconContainer);
                else 
                {
                    // Fallback creation
                    iconObj = new GameObject("StatusIcon");
                    iconObj.transform.SetParent(iconContainer, false);
                    var t = iconObj.AddComponent<UnityEngine.UI.Text>();
                    t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    t.color = Color.white;
                    t.alignment = TextAnchor.MiddleCenter;
                    t.resizeTextForBestFit = true;
                }
                
                // Set Text to Emoji
                var txt = iconObj.GetComponent<UnityEngine.UI.Text>();
                if (txt == null) txt = iconObj.GetComponentInChildren<UnityEngine.UI.Text>();
                
                if (txt != null)
                {
                    txt.text = GetStatusEmoji(effectId);
                }
            }
        }

        private string GetStatusEmoji(string statusId)
        {
            string key = statusId.ToLower().Replace(" ", "_");
            
            if (key.Contains("burn")) return "🔥";
            if (key.Contains("freeze") || key.Contains("chill")) return "❄️";
            if (key.Contains("stun")) return "💫";
            if (key.Contains("poison") || key.Contains("sick")) return "🤢";
            if (key.Contains("haste") || key.Contains("speed")) return "⚡";
            if (key.Contains("slow") || key.Contains("hinder")) return "🐌";
            if (key.Contains("prone")) return "⬇️";
            if (key.Contains("armor")) return "🛡️";
            if (key.Contains("weak")) return "💔";
            
            // Default
            return "❓"; 
        }
        public void SetCombatMode(bool inCombat)
        {
            var grid = GetComponent<AbilityGridController>();
            if (grid != null) grid.SetCombatMode(inCombat);
        }

        public void OnEndTurnClicked()
        {
            UnityEngine.UI.Button btn = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<UnityEngine.UI.Button>();
            if(btn != null) btn.interactable = false;

            Debug.Log("[HUD] Ending Turn...");
            ChaosCritters.Network.NetworkManager.Instance.Post("/battle/turn/end", "{}", 
                (json) => {
                    Debug.Log($"[HUD] Turn Ended Response: {json}");
                    // Process response logic
                    StartCoroutine(ProcessTurnResponse(json, btn));
                },
                (error) => {
                    Debug.LogError($"[HUD] End Turn Failed: {error}");
                    if(btn != null) btn.interactable = true;
                }
            );
        }

        [System.Serializable]
        private class TurnResponse
        {
            public string message;
            public string current_turn;
            public string narrative;
            public string battle_state; // Added to match JSON
            public List<AIActionData> ai_actions;
        }

        [System.Serializable]
        private class AIActionData
        {
            public string actor_id;
            public string action;
            // Common params
            public string target; 
            public int damage;
            public float[] from; // JSON [x, y]
            public float[] to;
        }

        private System.Collections.IEnumerator ProcessTurnResponse(string json, UnityEngine.UI.Button btn)
        {
            // Note: JsonUtility has limited support for nested lists/arrays. 
            // If AIActionData has arrays, we might need a wrapper or manual parsing.
            // Using a simpler approach or specific Structs helps.
            // Unity's JsonUtility often fails with top-level lists or complex nesting without wrappers.
            // Assuming the structure matches exactly.
            
            TurnResponse response = JsonUtility.FromJson<TurnResponse>(json);
            
            // 1. Narrate if needed
            if (!string.IsNullOrEmpty(response.narrative) && NarratorController.Instance != null)
            {
               NarratorController.Instance.AddLine(response.narrative);
            }

            // 2. Animate AI Actions
            if (response.ai_actions != null)
            {
                foreach(var action in response.ai_actions)
                {
                    // Find Actor
                    var token = ChaosCritters.Units.TokenManager.Instance.GetToken(action.actor_id);
                    if (token != null)
                    {
                        if (action.action == "Move")
                        {
                            // "to" is float array? JsonUtility might fail on float[]
                            // Let's assume standard MoveRequest format: to=(x,y)
                            // Actually, backend sends "to": (x, y) tuple which is [x, y] in JSON
                            
                            if (action.to != null && action.to.Length >= 2)
                            {
                                int tx = (int)action.to[0];
                                int ty = (int)action.to[1];
                                token.MoveTo(tx, ty);
                                yield return new WaitForSeconds(1.0f); // Wait for move
                            }
                        }
                        else if (action.action == "Attack")
                        {
                             // Trigger Attack Animation
                             Debug.Log($"[HUD] Action Attack: {action.actor_id} -> {action.target}");
                             if (action.actor_id == action.target)
                             {
                                 Debug.LogError($"[HUD] SELF ATTACK DETECTED! Actor {action.actor_id} matches Target {action.target}!");
                             }

                              yield return new WaitForSeconds(0.5f);
                              // Show Damage on Target
                              // Ideally we find the Target Token, not the Actor token?
                              // The original code used 'token' (Actor) for position, which is OK for popup if MELEE
                              // But if proper target, we should find it.
                              
                              Transform targetT = token.transform; // Default to actor if target not found (fallback)
                              if (!string.IsNullOrEmpty(action.target))
                              {
                                    var tTarget = ChaosCritters.Units.TokenManager.Instance.GetToken(action.target);
                                    if(tTarget != null) targetT = tTarget.transform;
                              }
                              
                              DamagePopup.Create(targetT.position, action.damage, Color.red);
                              yield return new WaitForSeconds(0.5f);
                        }
                        else if (action.action == "Wait")
                        {
                            Debug.Log($"[HUD] {action.actor_id} is waiting.");
                            // Maybe show a "Zzz" popup?
                            DamagePopup.Create(token.transform.position + Vector3.up, 0, Color.blue); // Blue 0 for wait/skip
                            yield return new WaitForSeconds(0.5f);
                        }
                        else
                        {
                            Debug.LogWarning($"[HUD] Unknown AI Action: {action.action}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[HUD] Helper: Actor {action.actor_id} not found for action {action.action}");
                    }
                }
            }

            // 3. Update Turn Indicator
            Debug.Log($"[HUD] New Turn: {response.current_turn} | State: {response.battle_state}");
            
            // Re-enable button
            if(btn != null) btn.interactable = true;
            
            // Check Game Over
            if (response.battle_state == "Victory")
            {
                UIAssembler.ShowGameOver(true);
                if(btn != null) btn.interactable = false;
                // Disable controls via InteractionController?
                yield break;
            }
            else if (response.battle_state == "Defeat")
            {
                UIAssembler.ShowGameOver(false);
                if(btn != null) btn.interactable = false;
                yield break;
            }

            // Refresh Map Entities to sync positions/stats finally
            ChaosCritters.Units.TokenManager.Instance.RefreshEntities();
            Debug.Log("[HUD] Response Processed.");
        }
    }
}
