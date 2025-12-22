#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChaosCritters.EditorTools
{
    public class stylesFixer : MonoBehaviour
    {
        [MenuItem("ChaosCritters/Fix UI Sprites")]
        public static void FixUISprites()
        {
            string[] guids = AssetDatabase.FindAssets("uipack_rpg_sheet");
            
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
                    
                    if (importer.spriteImportMode != SpriteImportMode.Multiple)
                    {
                        importer.spriteImportMode = SpriteImportMode.Multiple;
                        changed = true;
                    }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                        Debug.Log($"[FixUISprites] Converted {path} to Sprite Mode.");
                    }
                    else
                    {
                        Debug.Log($"[FixUISprites] {path} is already correct.");
                    }
                }
            }
        }
    }
}
#endif
