using UnityEngine;
using UnityEngine.UI;
using ChaosCritters.Data;
using ChaosCritters.Units; // For InteractionController

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
            Setup();
        }

        public void Setup()
        {
            // Clear existing to avoid duplicates if Setup called multiple times
            if (northBtn != null) { northBtn.onClick.RemoveAllListeners(); northBtn.onClick.AddListener(() => OnAbilityClicked("Heavy")); }
            if (southBtn != null) { southBtn.onClick.RemoveAllListeners(); southBtn.onClick.AddListener(() => OnAbilityClicked("EndTurn")); }
            if (westBtn != null) { westBtn.onClick.RemoveAllListeners(); westBtn.onClick.AddListener(() => OnAbilityClicked("Physical")); }
            if (eastBtn != null) { eastBtn.onClick.RemoveAllListeners(); eastBtn.onClick.AddListener(() => OnAbilityClicked("Mental")); }
            
            // Update Label for North Button
            if (northBtn != null)
            {
                 var txt = northBtn.GetComponentInChildren<Text>();
                 if (txt != null) txt.text = "Smash [3]";
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) OnAbilityClicked("EndTurn");
            if (Input.GetKeyDown(KeyCode.Alpha1)) OnAbilityClicked("Physical");
            if (Input.GetKeyDown(KeyCode.Alpha2)) OnAbilityClicked("Mental");
            if (Input.GetKeyDown(KeyCode.Alpha3)) OnAbilityClicked("Heavy");
        }

        private void OnAbilityClicked(string action)
        {
            Debug.Log($"[ABILITY CLICK] Action: {action}");
            NarratorController.Instance?.AddLine($"Selected: {action}");
            
            if (action == "Physical" || action == "Mental" || action == "Heavy")
            {
                 // Trigger Targeting
                 InteractionController.Instance?.StartTargeting(action);
            }
            else if (action == "EndTurn")
            {
                if (HUDController.Instance != null)
                    HUDController.Instance.OnEndTurnClicked();
                else
                    TokenManager.Instance.RequestEndTurn();
            }
            else if (action == "Wait")
            {
                // Fallback for old calls or other inputs
                 if (HUDController.Instance != null) HUDController.Instance.OnEndTurnClicked();
            }
        }


        public void RefreshAbilities(EntityData data)
        {
            // TODO: Populate button text/icons based on EntityData.abilities list
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
