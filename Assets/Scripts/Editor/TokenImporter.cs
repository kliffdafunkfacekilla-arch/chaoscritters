#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace ChaosCritters.EditorTools
{
    public class TokenImporter
    {
        [MenuItem("ChaosCritters/Import Parsed Tokens")]
        public static void ImportTokens()
        {
            string folder = "Assets/Sprites/Parsed";
            if (!Directory.Exists(folder))
            {
                Debug.LogError($"Folder not found: {folder}");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { folder });
            int count = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null)
                {
                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    
                    // Pixels Per Unit - adjust if they look too small/big?
                    // Default 100 is fine for now.
                    
                    if (changed)
                    {
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }
            Debug.Log($"[TokenImporter] Processed {count} tokens in {folder}.");
        }
    }
}
#endif
