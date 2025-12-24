using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChaosCritters.Units
{
    public class SpriteAssembler : MonoBehaviour
    {
        [Header("Layers")]
        public SpriteRenderer shadowLayer; // Order 0
        public SpriteRenderer bodyLayer;   // Order 1
        public SpriteRenderer armorLayer;  // Order 2
        
        [Header("Effects")]
        public ParticleSystem vfxSystem;   // Order 3
        // public Light2D auraLight;       // Overlay (Requires URP, commented out for now)

        [Header("Databases")]
        // In a real implementation, these would be ScriptableObjects or Addressables
        // For now, we manually link sprite arrays in the inspector
        public List<VisualEntry> chassisDb;
        public List<VisualEntry> roleDb; // Could be armor/weapons
        public List<VisualEntry> infusionDb; // Could be VFX colors

        [System.Serializable]
        public struct VisualEntry
        {
            public string id; // e.g. "Insect", "Bear"
            public Sprite sprite;
            public Color color;
        }

        private Dictionary<string, Sprite> _chassisLookup;
        private Dictionary<string, Sprite> _roleLookup;

        private void Awake()
        {
            if (chassisDb == null) chassisDb = new List<VisualEntry>();
            if (roleDb == null) roleDb = new List<VisualEntry>();
            if (infusionDb == null) infusionDb = new List<VisualEntry>();

#if UNITY_EDITOR
            // Auto-populate for runtime testing if empty
            if (chassisDb.Count == 0) AutoDiscoverSchemas();
#endif
            InitializeLookups();
        }

        private void InitializeLookups()
        {
            _chassisLookup = new Dictionary<string, Sprite>();
            foreach(var entry in chassisDb) if(!_chassisLookup.ContainsKey(entry.id)) _chassisLookup.Add(entry.id, entry.sprite);

            _roleLookup = new Dictionary<string, Sprite>();
            foreach(var entry in roleDb) if(!_roleLookup.ContainsKey(entry.id)) _roleLookup.Add(entry.id, entry.sprite);
        }

        public void Assemble(Dictionary<string, string> visualTags)
        {
            EnsureLayers();

            // 1. Chassis (Body)
            // 1. Chassis (Body)
            if (visualTags.ContainsKey("chassis"))
            {
                string chassisId = visualTags["chassis"];
                if (_chassisLookup.ContainsKey(chassisId) && _chassisLookup[chassisId] != null)
                {
                    bodyLayer.sprite = _chassisLookup[chassisId];
                }
                else
                {
                    Debug.LogWarning($"[SpriteAssembler] Chassis '{chassisId}' not found. Using Fallback.");
                    if (bodyLayer.sprite == null) bodyLayer.sprite = GetRuntimeFallbackSprite();
                }
            }
            else
            {
                 // No tag? Use fallback.
                 if (bodyLayer.sprite == null) 
                 {
                     bodyLayer.sprite = GetRuntimeFallbackSprite();
                     bodyLayer.color = Color.magenta;   
                 }
            }

            // 2. Role (Armor/Weapon overlay)
            if (visualTags.ContainsKey("role"))
            {
                string roleId = visualTags["role"];
                if (_roleLookup.ContainsKey(roleId))
                {
                    armorLayer.sprite = _roleLookup[roleId];
                    armorLayer.enabled = true;
                }
                else
                {
                    armorLayer.enabled = false;
                }
            }

            // 3. Infusion (Color/VFX)
            if (visualTags.ContainsKey("infusion"))
            {
                string infusion = visualTags["infusion"];
                // Simple color mapping for now
                Color infusionColor = Color.white;
                switch(infusion)
                {
                    case "Gravity": infusionColor = new Color(0.5f, 0f, 0.5f); break; // Purple
                    case "Fire": infusionColor = Color.red; break;
                    case "Nature": infusionColor = Color.green; break;
                }
                
                // Tint the body slightly? Or just the aura.
                bodyLayer.color = infusionColor; 
                
                if (vfxSystem != null)
                {
                    var main = vfxSystem.main;
                    main.startColor = infusionColor;
                }
            }
        }

        private Sprite _runtimeFallback;
        private Sprite GetRuntimeFallbackSprite()
        {
            if (_runtimeFallback == null)
            {
                Texture2D tex = new Texture2D(32, 32);
                Color[] pixels = new Color[32*32];
                for(int i=0; i<pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                _runtimeFallback = Sprite.Create(tex, new Rect(0,0,32,32), new Vector2(0.5f, 0.5f));
            }
            return _runtimeFallback;
        }
        
        // Debug helper
        [ContextMenu("Test Assemble")]
        [ContextMenu("Auto-Discover Schemas")]
        public void AutoDiscoverSchemas()
        {
#if UNITY_EDITOR
            // 1. Chassis Discovery
            _chassisLookup = new Dictionary<string, Sprite>();
            chassisDb = new List<VisualEntry>();
            string[] chassisIds = new string[] { "Bear", "Insect", "Human" }; // Visual Types from Backend
            
            foreach (var id in chassisIds)
            {
                // Try find specific asset
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"{id} t:Sprite");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (s != null)
                    {
                        chassisDb.Add(new VisualEntry { id = id, sprite = s, color = Color.white });
                        Debug.Log($"[SpriteAssembler] Found Chassis: {id} -> {path}");
                    }
                }
                else
                {
                    // Fallback 1: Specific placeholder
                    string[] fallback = UnityEditor.AssetDatabase.FindAssets("roguelikeDungeon_transparent t:Sprite");
                    if (fallback.Length == 0)
                    {
                        // Fallback 2: ABSOLUTELY ANY SPRITE
                        fallback = UnityEditor.AssetDatabase.FindAssets("t:Sprite");
                    }

                    if (fallback.Length > 0)
                    {
                         string path = UnityEditor.AssetDatabase.GUIDToAssetPath(fallback[0]);
                         Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                         if (s != null)
                         {
                             chassisDb.Add(new VisualEntry { id = id, sprite = s, color = Color.gray }); 
                             Debug.Log($"[SpriteAssembler] Fallback Chassis: {id} -> {path}");
                         }
                    }
                }
            }
            
            // 2. Role Discovery (Warrior, Breaker)
            roleDb = new List<VisualEntry>();
            string[] roleIds = new string[] { "Warrior", "Breaker", "Mage" };
             foreach (var id in roleIds)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"{id} t:Sprite");
                 if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (s != null) roleDb.Add(new VisualEntry { id = id, sprite = s, color = Color.white });
                }
            }

            // Refresh Lookups
            InitializeLookups();
#endif
        }

        private void OnValidate()
        {
            if (chassisDb == null || chassisDb.Count == 0)
            {
                AutoDiscoverSchemas();
            }
        }
        private void EnsureLayers()
        {
            if (bodyLayer == null)
            {
                bodyLayer = GetComponent<SpriteRenderer>();
                // If still null, add one
                if (bodyLayer == null) bodyLayer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (armorLayer == null)
            {
                // Try to find existing child first
                var child = transform.Find("ArmorLayer");
                if (child != null)
                {
                    armorLayer = child.GetComponent<SpriteRenderer>();
                }
                else
                {
                    var go = new GameObject("ArmorLayer");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = Vector3.zero;
                    armorLayer = go.AddComponent<SpriteRenderer>();
                    armorLayer.sortingOrder = bodyLayer.sortingOrder + 1;
                }
            }

            if (shadowLayer == null)
            {
                var child = transform.Find("ShadowLayer");
                 if (child != null)
                {
                    shadowLayer = child.GetComponent<SpriteRenderer>();
                }
                else
                {
                    var go = new GameObject("ShadowLayer");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = Vector3.zero;
                    shadowLayer = go.AddComponent<SpriteRenderer>();
                    shadowLayer.sortingOrder = bodyLayer.sortingOrder - 1;
                }
            }
        }
    }
}
