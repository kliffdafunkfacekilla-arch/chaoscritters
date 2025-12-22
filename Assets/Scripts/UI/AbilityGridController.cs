using UnityEngine;
using UnityEngine.UI;
using ChaosCritters.Data;

namespace ChaosCritters.UI
{
    public class AbilityGridController : MonoBehaviour
    {
        [Header("D-Pad Configuration")]
        public Button northBtn; // Move / Interact
        public Button westBtn;  // Physical
        public Button eastBtn;  // Mental
        public Button southBtn; // End Turn

        private void Start()
        {
            // Hardcoded listeners for now until we have real ability data/logic
            if (northBtn != null) northBtn.onClick.AddListener(() => OnAbilityClicked("Move"));
            if (southBtn != null) southBtn.onClick.AddListener(() => OnAbilityClicked("EndTurn"));
            if (westBtn != null) westBtn.onClick.AddListener(() => OnAbilityClicked("Physical"));
            if (eastBtn != null) eastBtn.onClick.AddListener(() => OnAbilityClicked("Mental"));
        }

        private void OnAbilityClicked(string action)
        {
            Debug.Log($"[AbilityGrid] Clicked {action}");
            NarratorController.Instance?.AddLine($"Selected: {action}");
            
            // TODO: In Sprint 3, this will trigger Targeting Mode or Backend Calls
        }

        public void RefreshAbilities(EntityData data)
        {
            // TODO: Populate button text/icons based on EntityData.abilities list
        }
    }
}
