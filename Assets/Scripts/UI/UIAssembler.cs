using UnityEngine;
using UnityEngine.UI;
using TMPro; // Standard text mesh pro
using ChaosCritters.Data;

namespace ChaosCritters.UI
{
    public class UIAssembler : MonoBehaviour
    {
        // Changed from BuildDefaultHUD to VerifyHUD to allow re-entry
        public static void VerifyHUD()
        {
            // 1. Find or Create Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("MainCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // 1.5 Ensure EventSystem
            UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject esGO = new GameObject("EventSystem");
                eventSystem = esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 2. Create HUD Manager
            GameObject hudManager = GameObject.Find("HUD_Manager");
            if (hudManager == null)
            {
                hudManager = new GameObject("HUD_Manager");
                hudManager.transform.SetParent(canvas.transform, false);
                
                RectTransform rt = hudManager.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // 3. Ensure Controllers exist
            var hudCtrl = EnsureComponent<HUDController>(hudManager);
            var gridCtrl = EnsureComponent<AbilityGridController>(hudManager);
            var narratorCtrl = EnsureComponent<NarratorController>(hudManager);

            // 4. Build Player Card
            if (hudCtrl.playerCardPanel == null)
            {
                // ... (Existing implementation preserved via logic, but recreated here for safety if partial blocks not allowed)
                // ACTUALLY, for tool efficiency, I will just patch the BuildDefaultHUD method logic.
                // But I need to change the method name and the text raycast logic.
                // Assuming I am replacing the WHOLE method based on lines 10-86 in original.
                
                GameObject card = CreatePanel(hudManager.transform, "PlayerCard", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(400, 150), new Vector2(0, 150/2 + 20));
                hudCtrl.playerCardPanel = card.transform;
                
                Image bg = card.GetComponent<Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

                hudCtrl.nameText = CreateText(card.transform, "NameText", "Hero Name", 24, new Vector2(0, 50));
                hudCtrl.classText = CreateText(card.transform, "ClassText", "Class / Role", 16, new Vector2(0, 25));

                hudCtrl.healthBar = CreateBar(card.transform, "HealthBar", Color.green, new Vector2(0, -10));
                hudCtrl.staminaBar = CreateBar(card.transform, "StaminaBar", Color.red, new Vector2(-100, -50));
                hudCtrl.focusBar = CreateBar(card.transform, "FocusBar", Color.blue, new Vector2(100, -50));
            }

            // 5. Build Narrator
            if (narratorCtrl.feedText == null)
            {
                 GameObject feed = CreatePanel(hudManager.transform, "NarratorFeed", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(300, 200), new Vector2(-160, -110));
                 Image bg = feed.GetComponent<Image>();
                 bg.color = new Color(0, 0, 0, 0.5f);
                 
                 narratorCtrl.feedText = CreateText(feed.transform, "FeedText", "Narrator Log...", 14, Vector2.zero);
                 narratorCtrl.feedText.alignment = TextAlignmentOptions.TopLeft;
                 narratorCtrl.maxLines = 8;
            }

            // 6. Build Ability Grid
            if (gridCtrl.northBtn == null)
            {
                 GameObject grid = CreatePanel(hudManager.transform, "AbilityGrid", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(200, 200), new Vector2(-150, 150));
                 
                 gridCtrl.northBtn = CreateButton(grid.transform, "North", "Wait", new Vector2(0, 60));
                 gridCtrl.southBtn = CreateButton(grid.transform, "South", "End", new Vector2(0, -60));
                 gridCtrl.westBtn = CreateButton(grid.transform, "West", "Phys", new Vector2(-60, 0));
                 gridCtrl.eastBtn = CreateButton(grid.transform, "East", "Ment", new Vector2(60, 0));
            }
            
            // 7. Force Setup (Fixes race conditions if Start() ran before Buttons existed)
            gridCtrl.Setup();
        }



        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            
            // Visuals
            Image img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.9f);
            
#if UNITY_EDITOR
            // Try to find a nice panel sprite
            string[] guids = UnityEditor.AssetDatabase.FindAssets("panel t:Sprite");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                img.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                img.type = Image.Type.Sliced; // Assume 9-slice capable
            }
            else
            {
                 // Fallback to the sheet
                 guids = UnityEditor.AssetDatabase.FindAssets("uipack_rpg_sheet"); // Removed "t:Sprite" filter to find raw texture
                 if (guids.Length > 0)
                 {
                     string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                     
                     // Force fix if not a sprite
                     UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                     if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
                     {
                         importer.textureType = UnityEditor.TextureImporterType.Sprite;
                         importer.SaveAndReimport();
                         Debug.Log("[UIAssembler] Auto-Fixed UI Sheet Import Settings.");
                     }

                     img.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                 }
            }
#endif
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, float fontSize, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300, 50);
            
            TMP_Text txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false; // Fix: Text was blocking buttons
            return txt;
        }

        private static StatBar CreateBar(Transform parent, string name, Color color, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(100, 20); // Small bar
            
            // Background
            Image bg = go.AddComponent<Image>();
            bg.color = Color.gray;

            // Fill
            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(go.transform, false);
            RectTransform fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            
            Image fillImg = fillGO.AddComponent<Image>();
            fillImg.color = color;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            StatBar bar = go.AddComponent<StatBar>();
            bar.fillImage = fillImg;
            bar.barColor = color;
            
            // Text
            bar.valueText = CreateText(go.transform, "Val", "0/0", 12, Vector2.zero);

            return bar;
        }
        
        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(50, 50);
            
            Image img = go.AddComponent<Image>();
            img.color = Color.white;
            
#if UNITY_EDITOR
            // Try to find a nice button sprite
            string[] guids = UnityEditor.AssetDatabase.FindAssets("button t:Sprite");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                img.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                img.type = Image.Type.Sliced;
            }
#endif
            
            Button btn = go.AddComponent<Button>();
            
            CreateText(go.transform, "Label", label, 10, Vector2.zero).color = Color.black;
            
            return btn;
        }
    }
}
