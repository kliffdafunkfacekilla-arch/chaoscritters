#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using ChaosCritters.Map;
using ChaosCritters.Units;
using ChaosCritters.Network;

namespace ChaosCritters.Editor
{
    public class SceneBootstrapper : EditorWindow
    {
        [MenuItem("ChaosCritters/Setup Scene")]
        public static void SetupScene()
        {
            // 1. Create Grid
            GameObject grid = GameObject.Find("Grid");
            if (grid == null)
            {
                grid = new GameObject("Grid");
                grid.AddComponent<Grid>();
                Debug.Log("Created Grid.");
            }

            // 2. Create Tilemap
            Tilemap tilemap = grid.GetComponentInChildren<Tilemap>();
            if (tilemap == null)
            {
                GameObject tm = new GameObject("Tilemap");
                tm.transform.SetParent(grid.transform);
                tm.AddComponent<Tilemap>();
                tm.AddComponent<TilemapRenderer>();
                Debug.Log("Created Tilemap.");
            }

            // 3. Add MapBuilder
            MapBuilder builder = grid.GetComponent<MapBuilder>();
            if (builder == null)
            {
                builder = grid.AddComponent<MapBuilder>();
                builder.mapRadius = 20; // Set user preference
                Debug.Log("Added MapBuilder.");
            }

            // 4. Create TokenManager
            GameObject tmObj = GameObject.Find("TokenManager");
            if (tmObj == null)
            {
                tmObj = new GameObject("TokenManager");
                tmObj.AddComponent<TokenManager>();
                Debug.Log("Created TokenManager.");
            }

            // 5. Create CameraController
            GameObject cam = GameObject.Find("Main Camera");
            if (cam != null && cam.GetComponent<ChaosCritters.Core.CameraController>() == null)
            {
                cam.AddComponent<ChaosCritters.Core.CameraController>();
                cam.GetComponent<Camera>().orthographic = true;
                cam.GetComponent<Camera>().orthographicSize = 10;
                
                // Add Click Handler
                if (cam.GetComponent<InteractionController>() == null)
                {
                    cam.AddComponent<InteractionController>();
                    Debug.Log("Added InteractionController to Camera.");
                }
                
                Debug.Log("Setup Camera.");
            }

            Debug.Log("<color=green>Scene Setup Complete!</color>");
        }
    }
}
#endif
