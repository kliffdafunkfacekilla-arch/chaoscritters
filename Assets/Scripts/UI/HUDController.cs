using UnityEngine;
using ChaosCritters.Data;
using System.Collections.Generic;

namespace ChaosCritters.UI
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("Active Player Card")]
        public Transform playerCardPanel;
        public StatBar healthBar;
        public StatBar staminaBar;
        public StatBar focusBar;
        public TMPro.TMP_Text nameText;
        public TMPro.TMP_Text classText;

        [Header("Status Icons")]
        public Transform iconContainer;
        public GameObject iconPrefab; // Image with sprite

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            // Auto-Build Infrastructure if missing elements
            if (playerCardPanel == null || healthBar == null)
            {
                UIAssembler.VerifyHUD();
            }
            
            // Default state
            if (playerCardPanel != null) playerCardPanel.gameObject.SetActive(false);
        }

        public void UpdatePlayerCard(EntityData data)
        {
            if (data == null)
            {
                if (playerCardPanel != null) playerCardPanel.gameObject.SetActive(false);
                return;
            }

            if (playerCardPanel != null) playerCardPanel.gameObject.SetActive(true);

            // Text
            if (nameText != null) nameText.text = data.name;
            if (classText != null && data.visual_tags != null) 
                classText.text = $"{data.visual_tags.role} / {data.visual_tags.infusion}";

            // Bars
            if (healthBar != null) healthBar.SetValues(data.hp, data.max_hp);
            // Assuming default AP is mixed Stamina/Focus for now, or just mapping AP to Stamina
            if (staminaBar != null) staminaBar.SetValues(data.ap, 20); // Hardcoded max for now
            if (focusBar != null) focusBar.SetValues(data.composure, data.max_composure);

            // Status (TODO: Add status list to EntityData)
        }
    }
}
