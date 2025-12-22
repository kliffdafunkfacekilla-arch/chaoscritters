using UnityEngine;
using UnityEngine.Tilemaps;
using ChaosCritters.UI;

namespace ChaosCritters.Units
{
    public class InteractionController : MonoBehaviour
    {
        public static InteractionController Instance { get; private set; }

        public Tilemap tilemap;
        
        // Hardcoded for Sprint 3 - User is always Player 1
        private string myActorId = "P1"; 

        private enum InteractionMode { Normal, Targeting }
        private InteractionMode _currentMode = InteractionMode.Normal;
        private string _pendingAbility;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            Debug.Log($"CONTROLLER STARTED on {gameObject.name}");
            if (tilemap == null)
                tilemap = FindFirstObjectByType<Tilemap>();
        }

        public void StartTargeting(string abilityName)
        {
            _currentMode = InteractionMode.Targeting;
            _pendingAbility = abilityName;
            NarratorController.Instance?.AddLine($"Select Target for {abilityName}...");
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
                    TokenManager.Instance.RequestAttack(myActorId, targetId);
                    // Reset
                    _currentMode = InteractionMode.Normal;
                    NarratorController.Instance?.AddLine("Target Selected.");
                }
                else
                {
                    NarratorController.Instance?.AddLine("No valid target there.");
                    // Optional: Cancel on empty click?
                    _currentMode = InteractionMode.Normal;
                }
            }
            else
            {
                // Normal Move Logic
                 if (TokenManager.Instance != null)
                {
                    TokenManager.Instance.RequestMove(myActorId, cellPos.x, cellPos.y);
                }
                else
                {
                    Debug.LogError("TokenManager is MISSING! Cannot Move.");
                }
            }
        }
    }
}
