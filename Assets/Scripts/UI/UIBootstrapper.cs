using UnityEngine;

namespace ChaosCritters.UI
{
    public static class UIBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureUIExists()
        {
            var hud = Object.FindFirstObjectByType<HUDController>();
            if (hud == null)
            {
                // This call does EXACTLY what the user would have done manually:
                // 1. Creates "HUD_Manager"
                // 2. Adds "HUDController"
                // 3. Builds the Canvas
                UIAssembler.BuildDefaultHUD();
                Debug.Log("[UIBootstrapper] Auto-Generated HUD Hierarchy.");
            }
        }
    }
}
