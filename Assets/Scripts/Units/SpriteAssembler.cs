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
                string chassisId = visualTags["chassis"]; // e.g. "Bear"
                
                // Try Lookup first
                if (_chassisLookup.ContainsKey(chassisId) && _chassisLookup[chassisId] != null)
                {
                    bodyLayer.sprite = _chassisLookup[chassisId];
                }
                else
                {
                    // Runtime Resource Load Strategy
                    Sprite loaded = Resources.Load<Sprite>($"Sprites/{chassisId}");
                    if (loaded != null)
                    {
                         bodyLayer.sprite = loaded;
                         // Cache it
                         _chassisLookup[chassisId] = loaded;
                    }
                    else
                    {
                         Debug.LogWarning($"[SpriteAssembler] Chassis '{chassisId}' not found in Resources/Sprites. Using Fallback.");
                         if (bodyLayer.sprite == null) 
                         {
                             bodyLayer.sprite = GetRuntimeFallbackSprite();
                             bodyLayer.color = Color.magenta;   
                         }
                    }
                }
            }
            else
            {
                 // No tag? Use fallback.
                 if (bodyLayer.sprite == null) 
                 {
                     bodyLayer.sprite = GetRuntimeFallbackSprite();
                     bodyLayer.color = Color.gray;   
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
            // Expanded list based on actual file existence
            // Removed "Human", "Insect" as they likely don't exist as specific sprites or are groups
            string[] chassisIds = new string[] { 
                "Bear", "Wolf", "Fox", "Deer", "Eagle", "Falcon", 
                "Owl", "Raven", "Frog", "Ant", "Badger", "Butterfly", "Crab", "Croc", 
                "Lizard", "Mantis", "Mermaid", "Shark", "Spider", "Turtle" 
            }; 
            
            foreach (var id in chassisIds)
            {
                // Try find specific asset in Resources first (Preferred)
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"{id} t:Sprite");
                bool found = false;

                foreach(var guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    // Normalize path and check for Sprites folder case-insensitive
                    string ptr = path.Replace("\\", "/").ToLower();
                    
                    if (ptr.Contains("/sprites/"))
                    {
                        Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (s != null)
                        {
                            chassisDb.Add(new VisualEntry { id = id, sprite = s, color = Color.white });
                            // Debug.Log($"[SpriteAssembler] Found Chassis: {id}");
                            found = true;
                            break;
                        }
                    }
                }
                
                // Silent continue if not found to avoid error spam
            }
            
            // 2. Role Discovery (Warrior, Breaker)
            roleDb = new List<VisualEntry>();
            string[] roleIds = new string[] { "Warrior", "Breaker", "Mage" };
             foreach (var id in roleIds)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"{id} t:Sprite");
                foreach(var guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    // Also filter roles if possible, or just accept best match
                    // Accepting any match for Roles since they might be in UI folders
                    Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (s != null) 
                    {
                        roleDb.Add(new VisualEntry { id = id, sprite = s, color = Color.white });
                        break;
                    }
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
            int baseOrder = 10; // Ensure we are above map (Map is -10) or default (0)

            if (bodyLayer == null)
            {
                bodyLayer = GetComponent<SpriteRenderer>();
                // If still null, add one
                if (bodyLayer == null) bodyLayer = gameObject.AddComponent<SpriteRenderer>();
            }
            bodyLayer.sortingOrder = baseOrder;

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
                }
            }
            armorLayer.sortingOrder = baseOrder + 1;

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
                }
            }
            shadowLayer.sortingOrder = baseOrder - 1;
        }
    }
}
