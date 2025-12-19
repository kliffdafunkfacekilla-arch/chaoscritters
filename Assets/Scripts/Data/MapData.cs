using System;
using System.Collections.Generic;

namespace ChaosCritters.Data
{
    [Serializable]
    public class MapDataResponse
    {
        public int radius;
        public string biome;
        public int tile_count;
        public List<TileData> tiles;
    }

    [Serializable]
    public class TileData
    {
        public int x; // Was q
        public int y; // Was r
        public string terrain; // "Grass", "Water", etc.
        public int cost;
        public float height;
    }
}
