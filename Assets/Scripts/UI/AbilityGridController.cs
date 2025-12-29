using UnityEngine;
using UnityEngine.UI;
using ChaosCritters.Data;
using ChaosCritters.Units; // For InteractionController

namespace ChaosCritters.UI
{
    public class AbilityGridController : MonoBehaviour
    {
        [Header("D-Pad Configuration")]
        public Button westBtn;  // Slot 1
        public Button eastBtn;  // Slot 2
        public Button northBtn; // Slot 3
        public Button southBtn; // End Turn
        
        // Hotbar State
        private string[] hotbarSkills = new string[3]; // Map to 1, 2, 3
        
        private void Start()
        {
            Setup();
        }

        public void Setup()
        {
            // Reset Listeners
            if (westBtn != null) { westBtn.onClick.RemoveAllListeners(); westBtn.onClick.AddListener(() => OnHotbarClicked(0)); }
            if (eastBtn != null) { eastBtn.onClick.RemoveAllListeners(); eastBtn.onClick.AddListener(() => OnHotbarClicked(1)); }
            if (northBtn != null) { northBtn.onClick.RemoveAllListeners(); northBtn.onClick.AddListener(() => OnHotbarClicked(2)); }
            if (southBtn != null) { southBtn.onClick.RemoveAllListeners(); southBtn.onClick.AddListener(() => OnEndTurnClicked()); }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) OnEndTurnClicked();
            if (Input.GetKeyDown(KeyCode.Alpha1)) OnHotbarClicked(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) OnHotbarClicked(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) OnHotbarClicked(2);
        }

        // Called when a hotbar button (or key) is pressed
        private void OnHotbarClicked(int index)
        {
            if (index < 0 || index >= hotbarSkills.Length) return;
            
            string skillId = hotbarSkills[index];
            if (string.IsNullOrEmpty(skillId))
            {
                Debug.Log($"[Hotbar] Slot {index+1} is empty.");
                return;
            }

            Debug.Log($"[Hotbar] Selected Slot {index+1}: {skillId}");
            
            // Send Skill ID directly to InteractionController
            // InteractionController will need to handle Raw Skill IDs now, not "Physical" tags.
            InteractionController.Instance?.StartTargeting(skillId);
        }
        
        private void OnEndTurnClicked()
        {
             if (HUDController.Instance != null)
                 HUDController.Instance.OnEndTurnClicked();
             else
                 TokenManager.Instance.RequestEndTurn();
        }

        public void AssignSlot(int index, string skillId)
        {
            if (index < 0 || index >= hotbarSkills.Length) return;
            hotbarSkills[index] = skillId;
            UpdateButtonText(index, skillId);
            Debug.Log($"[Hotbar] Assigned {skillId} to Slot {index+1}");
        }

        public void RefreshAbilities(EntityData data)
        {
            if (data.known_skills == null) return;
            
            // Auto-Fill ONLY empty slots
            // This preserves manual assignments if Refresh is called multiple times
            
            for (int i = 0; i < data.known_skills.Length; i++)
            {
                string skillId = data.known_skills[i];
                
                // Try to place in first available slot? 
                // Or map 1:1?
                // M mapping 1:1 is dangerous if we have 20 skills.
                
                // Strategy: 
                // If hotbar is completely empty (first load), fill 1,2,3.
                // Else, do nothing (let User manage via Menu).
                
                // Implementation: Just map known_skills[0->2] to Slot 0->2 IF slot is null/empty
                if (i < hotbarSkills.Length)
                {
                    if (string.IsNullOrEmpty(hotbarSkills[i]))
                    {
                        AssignSlot(i, skillId);
                    }
                }
            }
            
            if (string.IsNullOrEmpty(hotbarSkills[0]))
            {
                 // Fallback if truly nothing known?
            }
        }
        
        private void UpdateButtonText(int index, string skillId)
        {
            Button btn = null;
            if (index == 0) btn = westBtn;
            if (index == 1) btn = eastBtn;
            if (index == 2) btn = northBtn;
            
            if (btn != null)
            {
                // Icon Logic
                if (SkillDatabase.Instance != null && !string.IsNullOrEmpty(skillId))
                {
                    Sprite icon = SkillDatabase.Instance.GetIcon(skillId);
                    if (icon != null)
                    {
                        btn.image.sprite = icon;
                        btn.image.color = Color.white; // Reset color in case it was grayed
                    }
                }

                // Text Logic (Overlay)
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    // Minimal Text: Just the Hotkey + Name?
                    // "Fireball [1]" -> "1" if Icon exists? 
                    // Let's keep Name for now but make it smaller or outline?
                    // Actually, let's keep it as is for clarity until the icons are good.
                    
                    string display = skillId.Replace("__", " ").Replace("_", " ");
                    txt.text = $"{display}\n[{index+1}]"; // Multiline
                    txt.resizeTextForBestFit = true;
                    
                    // Make text visually distinct over the icon (Shadow/Outline is expensive to add via script reliably without TMPro)
                    // Just set color to white with black outline? 
                    // Use standard Text component:
                    txt.color = Color.white;
                    if (txt.GetComponent<Outline>() == null)
                        txt.gameObject.AddComponent<Outline>().effectColor = Color.black;
                }
            }
        }
        
        public void SetCombatMode(bool inCombat)
        {
            if (southBtn != null)
            {
                southBtn.interactable = inCombat;
                var txt = southBtn.GetComponentInChildren<Text>();
                if (txt != null) txt.color = inCombat ? Color.black : Color.gray;
            }
        }
    }
}
