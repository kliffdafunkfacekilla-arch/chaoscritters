using UnityEngine;
using UnityEngine.Tilemaps;
using ChaosCritters.UI;

namespace ChaosCritters.Units
{
    public class InteractionController : MonoBehaviour
    {
        public static InteractionController Instance 
        { 
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<InteractionController>();
                    if (_instance == null)
                    {
                        if (Camera.main != null)
                        {
                            _instance = Camera.main.gameObject.AddComponent<InteractionController>();
                            Debug.Log("[InteractionController] Auto-Attached to Main Camera.");
                        }
                        else
                        {
                            GameObject go = new GameObject("InteractionController");
                            _instance = go.AddComponent<InteractionController>();
                            Debug.Log("[InteractionController] Auto-Created singleton object.");
                        }
                    }
                }
                return _instance;
            }
        }
        private static InteractionController _instance;

        public Tilemap tilemap;
        
        // Hardcoded for Sprint 3 - User is always Player 1
        private string myActorId = "P1"; 

        private enum InteractionMode { Normal, Targeting }
        private InteractionMode _currentMode = InteractionMode.Normal;
        private string _pendingAbility;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else if (_instance != this) Destroy(this);
        }

        private void Start()
        {
            // Robust Tilemap Finding
            if (tilemap == null)
            {
                tilemap = FindFirstObjectByType<Tilemap>();
                if (tilemap == null)
                {
                     // Last ditch: try to find the Grid and get component
                     var grid = FindFirstObjectByType<Grid>();
                     if (grid != null) tilemap = grid.GetComponentInChildren<Tilemap>();
                }
            }
            
            Debug.Log($"[InteractionController] Ready. Tilemap found: {(tilemap != null)}");
        }

        public void StartTargeting(string abilityName)
        {
            _currentMode = InteractionMode.Targeting;
            _pendingAbility = abilityName;
            ChaosCritters.UI.NarratorController.Instance?.AddLine($"Select Target for {abilityName}...");
        }

        private void OnGUI()
        {
            // Visual Proof that script is running
            GUI.color = Color.green;
            GUI.Label(new Rect(20, 20, 300, 50), $"MODE: {_currentMode}\nMouse: {Input.mousePosition}");
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) // Left Click
            {
                // Prevent click-through on UI
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    Debug.Log("[InteractionController] Click blocked by uGUI.");
                    return;
                }
                
                // Prevent click-through on IMGUI (Emergency UI)
                // GUIUtility.hotControl is 0 if nothing captured mouse. 
                // But for a simple button click, it might only be set during the frame of interaction.
                // A better check for IMGUI is hard without rect checks.
                // However, blocking uGUI is the most important step for the real UI.
                
                HandleClick();
            }
        }

        private void HandleClick()
        {
            // 1. Convert Screen Click to World Point
            if (Camera.main == null) return;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // 2. Convert World Point to Cell Coordinate (Integer Grid)
            if (tilemap == null) return;
            Vector3Int cellPos = tilemap.WorldToCell(worldPos);
            
            Debug.Log($"CLICK DETECTED at Cell: {cellPos}. Mode: {_currentMode}");

            if (_currentMode == InteractionMode.Targeting)
            {
                // Attack / Ability Logic
                string targetId = TokenManager.Instance.GetTokenAt(cellPos.x, cellPos.y);
                if (!string.IsNullOrEmpty(targetId))
                {
                    // Generic Handling
                    if (_pendingAbility == "Attack" || _pendingAbility == "Basic Attack" || _pendingAbility == "Physical" || _pendingAbility == "basic_attack")
                    {
                         // Default Basic Attack
                        TokenManager.Instance.RequestAttack(myActorId, targetId);
                    }
                    else
                    {
                        // Assume it is a Skill ID (e.g. "focused__blast")
                        TokenManager.Instance.RequestAbility(myActorId, targetId, _pendingAbility);
                    }
                    
                    // Reset
                    _currentMode = InteractionMode.Normal;
                    ChaosCritters.UI.NarratorController.Instance?.AddLine("Action Sent.");
                }
                else
                {
                    ChaosCritters.UI.NarratorController.Instance?.AddLine("No valid target there.");
                    // Optional: Cancel on empty click?
                    _currentMode = InteractionMode.Normal;
                }
            }
            else
            {
                // Normal Move Logic
                if (TokenManager.Instance != null)
                {
                    // Allow move if Turn is P1 OR if it's Free Roam (null/empty)
                    string currentBtn = TokenManager.Instance.CurrentActorId;
                    if (currentBtn == myActorId || string.IsNullOrEmpty(currentBtn))
                    {
                        TokenManager.Instance.RequestMove(myActorId, cellPos.x, cellPos.y);
                    }
                    else
                    {
                        ChaosCritters.UI.NarratorController.Instance?.AddLine($"Not your turn! Current: {currentBtn}");
                    }
                }
                else
                {
                    Debug.LogError("TokenManager is MISSING! Cannot Move.");
                }
            }
        }
    }
}
