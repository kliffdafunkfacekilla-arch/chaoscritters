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
            // 1. Chassis (Body)
            if (visualTags.ContainsKey("chassis"))
            {
                string chassisId = visualTags["chassis"];
                if (_chassisLookup.ContainsKey(chassisId))
                {
                    bodyLayer.sprite = _chassisLookup[chassisId];
                }
                else
                {
                    Debug.LogWarning($"[SpriteAssembler] Chassis '{chassisId}' not found.");
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
                // bodyLayer.color = infusionColor; 
                
                if (vfxSystem != null)
                {
                    var main = vfxSystem.main;
                    main.startColor = infusionColor;
                }
            }
        }
        
        // Debug helper
        [ContextMenu("Test Assemble")]
        public void TestAssemble()
        {
            var tags = new Dictionary<string, string>
            {
                { "chassis", "Bear" },
                { "role", "Warrior" },
                { "infusion", "Gravity" }
            };
            Assemble(tags);
        }
    }
}
