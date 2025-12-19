using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps; // Native Unity Tilemap support
using ChaosCritters.Network;
using ChaosCritters.Data;

namespace ChaosCritters.Map
{
    public class MapBuilder : MonoBehaviour
    {
        [Header("Configuration")]
        public int mapRadius = 5;
        public string biome = "Forest";

        [Header("References")]
        public Tilemap tilemap; // Drag your Grid -> Tilemap here
        public TileBase defaultTile; // Drag a Tile asset here
        
        // Lookup for specific terrain types (e.g. "Water" -> Water Tile)
        public List<TerrainTile> terrainTiles;

        [System.Serializable]
        public struct TerrainTile
        {
            public string terrainName;
            public TileBase tile;
        }

        private Dictionary<string, TileBase> _tileLookup;

        private void Awake()
        {
            // 1. Auto-Find Tilemap (So you don't have to drag it)
            if (tilemap == null)
                tilemap = GetComponentInChildren<Tilemap>();
            
            // 2. Auto-Create a Default Tile (So you don't have to make an asset)
            if (defaultTile == null)
            {
                Tile t = ScriptableObject.CreateInstance<Tile>();
                // Create a 1x1 white texture for the sprite
                Texture2D texture = new Texture2D(16, 16);
                for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) texture.SetPixel(x, y, Color.white);
                texture.Apply();
                t.sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
                defaultTile = t;
            }

            _tileLookup = new Dictionary<string, TileBase>();
            foreach (var tt in terrainTiles)
            {
                if (!_tileLookup.ContainsKey(tt.terrainName) && tt.tile != null)
                    _tileLookup.Add(tt.terrainName, tt.tile);
            }
        }

        private void Start()
        {
            GenerateMap();
        }

        [ContextMenu("Clear Map")]
        public void ClearMap()
        {
            if (tilemap != null) tilemap.ClearAllTiles();
        }

        [ContextMenu("Generate Map")]
        public void GenerateMap()
        {
            ClearMap();
            
            string payload = $"{{\"radius\": {mapRadius}, \"biome\": \"{biome}\"}}";
            Debug.Log($"Requesting Map: {payload}");

            NetworkManager.Instance.Post("/map/generate", payload, 
                onSuccess: (json) => 
                {
                    Debug.Log("Map Data Received. Painting Tilemap...");
                    MapDataResponse mapData = JsonUtility.FromJson<MapDataResponse>(json);
                    PaintMap(mapData);
                },
                onError: (err) => Debug.LogError($"Map Generation Failed: {err}")
            );
        }

        private void PaintMap(MapDataResponse data)
        {
            if (tilemap == null)
            {
                Debug.LogError("No Tilemap assigned!");
                return;
            }

            foreach (var tile in data.tiles)
            {
                // Direct Square Grid Mapping
                Vector3Int pos = new Vector3Int(tile.x, tile.y, 0);

                TileBase tileToUse = defaultTile;
                if (_tileLookup.ContainsKey(tile.terrain))
                {
                    tileToUse = _tileLookup[tile.terrain];
                }

                if (tileToUse != null)
                {
                    tilemap.SetTile(pos, tileToUse);
                }
            }
            
            // Recalculate bounds to ensure camera centers correctly later
            tilemap.CompressBounds();
        }
    }
}
