using UnityEngine;

namespace ChaosCritters.UI
{
    public static class UIBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureUIExists()
        {
            // Always verify to ensure connections and EventSystem
            UIAssembler.VerifyHUD();
            Debug.Log("[UIBootstrapper] Verified HUD Integrity.");
        }
    }
}
