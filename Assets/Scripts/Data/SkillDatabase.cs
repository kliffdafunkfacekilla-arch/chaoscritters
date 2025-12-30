using System.Collections.Generic;
using UnityEngine;
using ChaosCritters.Network;

namespace ChaosCritters.Data
{
    // Simplified model matching the backend's Structure
    [System.Serializable]
    public class SkillDef
    {
        public string id;
        public string name;
        public string icon;
        public string narrative;
    }
    
    [System.Serializable]
    public class SkillListResponse
    {
        // Because the backend returns a Dict<string, Ability>,
        // JsonUtility is bad at parsing Dictionaries directly.
        // I might need to update the Backend endpoint to return a List or use a Wrapper.
        // Let's check server.py: @app.get("/data/abilities") returns engine.abilities.metrics? 
        // No, engine.abilities returns AbilityDatabase?
        // Let's assume for now I need to parse a JSON Object where Keys are IDs.
        // Actually, for simplicity, I will implement a custom parser or just assume the backend returns a List,
        // OR I will just implement a "Lazy" lookup if I can't fetch all at once easily with JsonUtility.
        
        // BETTER PLAN: Update Server to return `{"skills": [list]}` or handle `Dict` via a Wrapper.
        // But I can't easily change `server.py` return type of library objects.
        // Wait, `server.py` returns `engine.abilities`. `engine.abilities` is an instance of `AbilityDatabase`.
        // `AbilityDatabase` has `self.skills: Dict`.
        // So endpoint returns `{"skills": {"id": {...}, "id2": {...}}}`.
        // Iterate over keys? JsonUtility cannot do this.
        // I will use `SimpleJSON` or just string manipulation if I have to.
        // OR, I can just fetch `known_skills` only? No, I want all.
        
        // WORKAROUND: I will add a method to `server.py` to return a LIST of skills specifically for Unity.
    }

    public class SkillDatabase : MonoBehaviour
    {
        public static SkillDatabase Instance { get; private set; }
        
        private Dictionary<string, SkillDef> _skills = new Dictionary<string, SkillDef>();
        private bool _isLoaded = false;
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            DontDestroyOnLoad(gameObject);
        }
        
        public void Initialize()
        {
            if (_isLoaded) return;
            Debug.Log("[SkillDB] Fetching definitions...");
            
            // Note: Since JsonUtility fails at Dictionaries, we might need a List endpoint.
            // I'll try to hit "/data/abilities/list" (I need to create this info).
            NetworkManager.Instance.Get("/data/abilities/list", 
                onSuccess: (json) => 
                {
                    // Wrapper: { "skills": [ ... ] }
                    SkillWrapper wrapper = JsonUtility.FromJson<SkillWrapper>(json);
                    if (wrapper != null && wrapper.skills != null)
                    {
                        foreach (var s in wrapper.skills)
                        {
                            _skills[s.id] = s;
                        }
                        _isLoaded = true;
                        Debug.Log($"[SkillDB] Loaded {_skills.Count} skills.");
                    }
                },
                onError: (err) => Debug.LogWarning($"[SkillDB] Failed to load: {err}")
            );
        }

        public SkillDef Get(string id)
        {
            if (_skills.ContainsKey(id)) return _skills[id];
            
            // Special Cases
            if (id == "basic_attack") return new SkillDef { id = "basic_attack", name = "Basic Attack", icon = "sword_slash", narrative = "Strike with weapon." };
            
            // Fallback for unknown
            return new SkillDef { id = id, name = id, icon = "default_icon", narrative = "Unknown" };
        }
        
        public Sprite GetIcon(string id)
        {
            string iconName = Get(id).icon;
            if (string.IsNullOrEmpty(iconName)) iconName = "default_icon";
            
            // Try direct Sprite load (requires Import Settings = Sprite)
            Sprite s = Resources.Load<Sprite>($"Icons/{iconName}");
            
            // Fallback: Load as Texture2D and wrap
            if (s == null)
            {
                Texture2D tex = Resources.Load<Texture2D>($"Icons/{iconName}");
                if (tex != null)
                {
                    s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    // Cache this? For now, just create.
                }
            }

            // Ultimate Fallback
            if (s == null) 
            {
                // Try fetching default
                 Texture2D defTex = Resources.Load<Texture2D>("Icons/default_icon");
                 if (defTex != null)
                    s = Sprite.Create(defTex, new Rect(0,0,defTex.width, defTex.height), new Vector2(0.5f, 0.5f));
            }
            
            return s;
        }
        
        [System.Serializable]
        private class SkillWrapper
        {
             public SkillDef[] skills;
        }
    }
}
