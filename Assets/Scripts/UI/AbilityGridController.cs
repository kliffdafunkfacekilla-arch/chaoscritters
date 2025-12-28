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
            if (northBtn != null) { northBtn.onClick.RemoveAllListeners(); northBtn.onClick.AddListener(() => OnAbilityClicked("Wait")); }
            if (southBtn != null) { southBtn.onClick.RemoveAllListeners(); southBtn.onClick.AddListener(() => OnAbilityClicked("EndTurn")); }
            if (westBtn != null) { westBtn.onClick.RemoveAllListeners(); westBtn.onClick.AddListener(() => OnAbilityClicked("Physical")); }
            if (eastBtn != null) { eastBtn.onClick.RemoveAllListeners(); eastBtn.onClick.AddListener(() => OnAbilityClicked("Mental")); }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) OnAbilityClicked("EndTurn");
            if (Input.GetKeyDown(KeyCode.Alpha1)) OnAbilityClicked("Physical");
            if (Input.GetKeyDown(KeyCode.Alpha2)) OnAbilityClicked("Mental");
        }

        private void OnAbilityClicked(string action)
        {
            Debug.Log($"[ABILITY CLICK] Action: {action}");
            if (action == "Wait")
            {
                 // Placeholder for skip turn
                 Debug.Log("Waiting...");
                 
                 // Visual Feedback
                 if (TokenManager.Instance != null && !string.IsNullOrEmpty(TokenManager.Instance.CurrentActorId))
                 {
                     var token = TokenManager.Instance.GetToken(TokenManager.Instance.CurrentActorId);
                     if (token != null)
                     {
                         // Show "WAIT" or "0" blue popup
                         DamagePopup.Create(token.transform.position + Vector3.up, 0, Color.blue);
                     }
                 }

                 if (HUDController.Instance != null) HUDController.Instance.OnEndTurnClicked();
            }
            NarratorController.Instance?.AddLine($"Selected: {action}");
            
            if (action == "Physical" || action == "Mental")
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
        }

        public void RefreshAbilities(EntityData data)
        {
            // TODO: Populate button text/icons based on EntityData.abilities list
        }
    }
}
