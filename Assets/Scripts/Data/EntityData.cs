using System;

namespace ChaosCritters.Data
{
    [Serializable]
    public class EntityData
    {
        public string id;
        public string name;
        public int x;
        public int y;
        public string team; // "Player" or "Enemy"
        public int hp;
        public int max_hp;
        public int ap;
        public int composure;
        public int max_composure;
        public int stamina;
        public int max_stamina;
        public int focus;
        public int max_focus;
        public string image_id; // For sprite lookup
        public string lineage;
        public string heritage;
        public string background;

        public VisualTags visual_tags; // Deprecated, kept for safety until full cleanup
        public string[] known_skills;
        public string[] status_effects;
    }

    [Serializable]
    public class EntityListResponse
    {
        public EntityData[] entities;
    }
}
