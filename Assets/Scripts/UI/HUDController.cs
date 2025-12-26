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
            if (classText != null && data.visual_tags != null) 
                classText.text = $"{data.visual_tags.role} / {data.visual_tags.infusion}";

            // Bars
            if (healthBar != null) healthBar.SetValues(data.hp, data.max_hp);
            // Assuming default AP is mixed Stamina/Focus for now, or just mapping AP to Stamina
            if (staminaBar != null) staminaBar.SetValues(data.ap, 20); // Hardcoded max for now
            if (focusBar != null) focusBar.SetValues(data.composure, data.max_composure);

            // Status (TODO: Add status list to EntityData)
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
                            // Unity JsonUtility DOES NOT support float[] directly in some versions or list inside list.
                            // Better to parse manually or use a helper if this fails.
                            // For prototype, we'll try simplistic parsing or string checks if JsonUtility fails.
                            // BUT, let's assume it works for List<AIActionData> if properly typed.
                            // actually float[] array is supported in newer Unity versions.
                            
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
                            // token.Attack(target);
                             yield return new WaitForSeconds(0.5f);
                             // Show Damage on Target
                             // Use 'action.target' name to find entity? Ideally we'd have target_id.
                             // For now, simpler.
                             DamagePopup.Create(token.transform.position, action.damage, Color.red);
                             yield return new WaitForSeconds(0.5f);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[HUD] Helper: Actor {action.actor_id} not found for action {action.action}");
                    }
                }
            }

            // 3. Update Turn Indicator
            Debug.Log($"[HUD] New Turn: {response.current_turn}");
            
            // Re-enable button
            if(btn != null) btn.interactable = true;
            
            // Refresh Map Entities to sync positions/stats finally
            // ChaosCritters.Units.TokenManager.Instance.RefreshEntities();
        }
    }
}
