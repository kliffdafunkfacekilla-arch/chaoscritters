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
            if (tilemap == null)
                tilemap = FindObjectOfType<Tilemap>();
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
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // 2. Convert World Point to Cell Coordinate (Integer Grid)
            Vector3Int cellPos = tilemap.WorldToCell(worldPos);
            
            // 3. Request Move
            Debug.Log($"Clicked Cell: {cellPos}");
            
            // For now, we assume we control "P1"
            if (TokenManager.Instance != null)
            {
                TokenManager.Instance.RequestMove(myActorId, cellPos.x, cellPos.y);
            }
        }
    }
}
