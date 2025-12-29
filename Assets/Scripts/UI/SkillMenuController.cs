using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ChaosCritters.Data;
using ChaosCritters.Units;

namespace ChaosCritters.UI
{
    public class SkillMenuController : MonoBehaviour
    {
        public static SkillMenuController Instance { get; private set; }
        
        public GameObject menuPanel; // Assigned by Assembler
        public Transform contentRoot; // output content
        
        // Runtime
        private EntityData _currentData;
        private bool _isOpen = false;
        
        // Simple Style
        private Font _defaultFont;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_defaultFont == null) _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            
            // Start Closed
            if (menuPanel != null) menuPanel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            if (menuPanel != null) menuPanel.SetActive(_isOpen);
            
            if (_isOpen && _currentData != null)
            {
                Refresh(_currentData);
            }
        }
        
        public void LoadData(EntityData data)
        {
            _currentData = data;
            if (_isOpen) Refresh(data);
        }

        private void Refresh(EntityData data)
        {
            // Clear Old
            foreach (Transform child in contentRoot)
            {
                Destroy(child.gameObject);
            }
            
            if (data.known_skills == null) return;
            
            // Populate
            foreach (string skillId in data.known_skills)
            {
                CreateSkillRow(skillId);
            }
        }
        
        private void CreateSkillRow(string skillId)
        {
            // Row Container
            GameObject row = new GameObject($"Row_{skillId}");
            row.transform.SetParent(contentRoot, false);
            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 10;
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.minHeight = 30;
            le.preferredHeight = 40;
            
            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row.transform, false);
            Image ic = iconObj.AddComponent<Image>();
            ic.color = Color.white;
            if (SkillDatabase.Instance != null)
                ic.sprite = SkillDatabase.Instance.GetIcon(skillId);
                
            LayoutElement leIcon = iconObj.AddComponent<LayoutElement>();
            leIcon.minWidth = 30;
            leIcon.minHeight = 30;
            leIcon.preferredWidth = 30;
            leIcon.preferredHeight = 30;
            
            // Name Text
            GameObject txtObj = new GameObject("Name");
            txtObj.transform.SetParent(row.transform, false);
            Text t = txtObj.AddComponent<Text>();
            t.font = _defaultFont;
            t.text = Prettify(skillId);
            t.color = Color.black;
            t.alignment = TextAnchor.MiddleLeft;
            t.resizeTextForBestFit = true;
            LayoutElement leTxt = txtObj.AddComponent<LayoutElement>();
            leTxt.minWidth = 120; // Reduce slightly to fit icon
            
            // Cast Button
            CreateButton(row, "Cast", () => 
            {
                InteractionController.Instance?.StartTargeting(skillId);
                Toggle(); // Close on cast?
            });

            // Bind 1
            CreateButton(row, "1", () => Bind(0, skillId));
            // Bind 2
            CreateButton(row, "2", () => Bind(1, skillId));
            // Bind 3
            CreateButton(row, "3", () => Bind(2, skillId));
        }
        
        private void CreateButton(GameObject parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent.transform, false);
            Image img = btnObj.AddComponent<Image>();
            img.color = Color.white;
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(action);
            
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.minWidth = (label.Length > 1) ? 60 : 30; // "Cast" vs "1"
            
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text t = txtObj.AddComponent<Text>();
            t.font = _defaultFont;
            t.text = label;
            t.color = Color.black;
            t.alignment = TextAnchor.MiddleCenter;
            t.resizeTextForBestFit = true;
            
            // Stretch Text to Button
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void Bind(int slot, string skillId)
        {
            var grid = FindFirstObjectByType<AbilityGridController>();
            if (grid != null)
            {
                grid.AssignSlot(slot, skillId);
                NarratorController.Instance?.AddLine($"Bound {Prettify(skillId)} to Key {slot + 1}");
            }
        }

        private string Prettify(string s)
        {
            return s.Replace("__", " ").Replace("_", " ");
        }
    }
}
