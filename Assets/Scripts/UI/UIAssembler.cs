using UnityEngine;
using UnityEngine.UI;
// using TMPro; // Standard text mesh pro (Disabled for reliability)
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
                canvas.sortingOrder = 100; // Ensure on top
                
                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            else
            {
               // Self-Healing
               EnsureComponent<GraphicRaycaster>(canvas.gameObject);
               var scaler = EnsureComponent<CanvasScaler>(canvas.gameObject);
               // Don't override scaler settings aggressively if it exists, maybe user changed them.
               // But ensure Raycaster is there.
               if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
               {
                   canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                   Debug.LogWarning("[UIAssembler] Fixed Canvas RenderMode from WorldSpace (No Camera) to Overlay.");
               }
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
                
                // Player Card - Top Left Corner
                GameObject card = CreatePanel(hudManager.transform, "PlayerCard", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(400, 150), new Vector2(210, -85));
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
                 narratorCtrl.feedText.alignment = TextAnchor.UpperLeft;
                 narratorCtrl.maxLines = 8;
            }

            // 6. Build Action Bar (Replaces AbilityGrid)
            if (gridCtrl.northBtn == null)
            {
                 // Bottom Center Bar
                 GameObject grid = CreatePanel(hudManager.transform, "ActionBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(600, 100), new Vector2(0, 60));
                 
                 // Make panel background darker for readability
                 grid.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
                 
                 // Centered Horizontal Layout
                 // Total items: 4. Widths: 100, 100, 100, 120. Spacing ~20.
                 // Total substantial width: 420.
                 // Start X = -210 + half button width? 
                 // Let's use simple hardcoded offsets from center 0.
                 
                 float yPos = 0;
                 
                 gridCtrl.westBtn = CreateButton(grid.transform, "BtnAttack", "Attack", new Vector2(-180, yPos), new Vector2(100, 50));
                 gridCtrl.eastBtn = CreateButton(grid.transform, "BtnSkill", "Skill", new Vector2(-60, yPos), new Vector2(100, 50));
                 gridCtrl.northBtn = CreateButton(grid.transform, "BtnWait", "Wait", new Vector2(60, yPos), new Vector2(100, 50));
                 
                 gridCtrl.southBtn = CreateButton(grid.transform, "BtnEndTurn", "End Turn", new Vector2(200, yPos), new Vector2(120, 60));
                 
                 // Colors
                 gridCtrl.southBtn.GetComponent<Image>().color = new Color(0.8f, 0.3f, 0.3f); // Reddish
                 gridCtrl.westBtn.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
            }
            
            // 7. Force Setup
            gridCtrl.Setup();
        }

        // ... helpers ...

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
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // High opacity Dark
            
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
                 guids = UnityEditor.AssetDatabase.FindAssets("uipack_rpg_sheet"); 
                 if (guids.Length > 0)
                 {
                     string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                     
                     UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                     if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
                     {
                         importer.textureType = UnityEditor.TextureImporterType.Sprite;
                         importer.SaveAndReimport();
                     }

                     img.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                 }
            }
#endif
            return go;
        }


        private static Text CreateText(Transform parent, string name, string content, float fontSize, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300, 50);
            
            Text txt = go.AddComponent<Text>();
            txt.text = content;
            
            Font font = Font.CreateDynamicFontFromOSFont("Arial", (int)fontSize);
            if (font == null) 
            {
                var fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0) font = fonts[0];
            }
            
            txt.font = font;
            txt.fontSize = (int)fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false; 
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
        
        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.8f, 0.8f, 0.8f); 
            img.raycastTarget = true;
            
#if UNITY_EDITOR
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

        public static void ShowGameOver(bool victory)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Remove existing HUD manager to cleanup? Or just overlay?
            // Overlay is safer.
            
            GameObject pnl = CreatePanel(canvas.transform, "GameOverPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image img = pnl.GetComponent<Image>();
            img.color = new Color(0,0,0, 0.85f);
            
            string msg = victory ? "VICTORY" : "DEFEAT";
            Color col = victory ? Color.yellow : Color.red;
            
            Text txt = CreateText(pnl.transform, "Title", msg, 80, new Vector2(0, 50));
            txt.color = col;
            
            Text sub = CreateText(pnl.transform, "Sub", "Press Alt+F4 to Quit (for now)", 20, new Vector2(0, -50));
        }
    }
}
