using UnityEngine;

namespace ChaosCritters.Helpers
{
    public static class GridHelper
    {
        // Must match Python grid.py size logic if any
        // Python: x = size * (sqrt(3) * q + sqrt(3)/2 * r)
        // Python: y = size * (3/2 * r)
        // Unity uses float, typically Y is up in 3D, or Y is up in 2D. 
        // We will assume 3D world where we map to (X, 0, Z) or 2D (X, Y). 
        // Let's assume 2D for tilemaps first (X, Y).
        
        public static Vector3 HexToPixel(int q, int r, float size = 1.0f)
        {
            float x = size * (Mathf.Sqrt(3) * q + Mathf.Sqrt(3) / 2 * r);
            float y = size * (3f / 2 * r);
            return new Vector3(x, y, 0); // Z=0 for 2D
        }

        public static Vector2Int PixelToHex(float x, float y, float size = 1.0f)
        {
            float q = (Mathf.Sqrt(3) / 3 * x - 1f / 3 * y) / size;
            float r = (2f / 3 * y) / size;
            return HexRound(q, r);
        }

        private static Vector2Int HexRound(float fracQ, float fracR)
        {
            float fracS = -fracQ - fracR;
            int q = Mathf.RoundToInt(fracQ);
            int r = Mathf.RoundToInt(fracR);
            int s = Mathf.RoundToInt(fracS);

            float q_diff = Mathf.Abs(q - fracQ);
            float r_diff = Mathf.Abs(r - fracR);
            float s_diff = Mathf.Abs(s - fracS);

            if (q_diff > r_diff && q_diff > s_diff)
                q = -r - s;
            else if (r_diff > s_diff)
                r = -q - s;
            
            return new Vector2Int(q, r);
        }
    }
}
