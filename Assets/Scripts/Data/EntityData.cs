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
        public string image_id; // For sprite lookup
    }

    [Serializable]
    public class EntityListResponse
    {
        public EntityData[] entities;
    }
}
