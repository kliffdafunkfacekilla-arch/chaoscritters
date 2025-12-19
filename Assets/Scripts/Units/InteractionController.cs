using UnityEngine;
using UnityEngine.Tilemaps;

namespace ChaosCritters.Units
{
    public class InteractionController : MonoBehaviour
    {
        public Tilemap tilemap;
        
        // Hardcoded for Sprint 3 - User is always Player 1
        private string myActorId = "P1"; 

        private void Start()
        {
            Debug.Log($"CONTROLLER STARTED on {gameObject.name}");
            if (tilemap == null)
                tilemap = FindObjectOfType<Tilemap>();
        }

        private void OnGUI()
        {
            // Visual Proof that script is running
            GUI.color = Color.green;
            GUI.Label(new Rect(20, 20, 300, 50), $"CONTROLLER ACTIVE. CLICK TO MOVE.\nMouse: {Input.mousePosition}");
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
            if (Camera.main == null)
            {
                Debug.LogError("No Main Camera found!");
                return;
            }
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // 2. Convert World Point to Cell Coordinate (Integer Grid)
            if (tilemap == null)
            {
                 Debug.LogError("InteractionController: No Tilemap assigned!");
                 return;
            }
            Vector3Int cellPos = tilemap.WorldToCell(worldPos);
            
            // 3. Request Move
            Debug.Log($"CLICK DETECTED at Cell: {cellPos}. Requesting Move...");
            
            // For now, we assume we control "P1"
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
