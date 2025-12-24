using UnityEngine;
using ChaosCritters.Units;
using ChaosCritters.UI;

public class EmergencyUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        GameObject go = new GameObject("EmergencyUI");
        go.AddComponent<EmergencyUI>();
        DontDestroyOnLoad(go);
    }

    private void OnGUI()
    {
        // Big scalable font
        GUI.skin.label.fontSize = 20;
        GUI.skin.button.fontSize = 20;

        GUI.Box(new Rect(10, 10, 400, 600), "EMERGENCY DEBUG IO");

        // 1. Backend Status
        if (TokenManager.Instance != null)
        {
            GUI.Label(new Rect(30, 40, 350, 30), $"Actor: {TokenManager.Instance.CurrentActorId}");
            
            // Hacky reflection or just generic info? 
            // We can't easily access the private dictionary in TokenManager without changing it.
            // But we can check if we can find objects.
            var tokens = FindObjectsByType<TokenController>(FindObjectsSortMode.None);
            GUI.Label(new Rect(30, 70, 350, 30), $"Tokens Found: {tokens.Length}");
            
            int y = 100;
            foreach(var t in tokens)
            {
                GUI.Label(new Rect(30, y, 350, 30), $"{t.name} @ {t.transform.position}");
                y += 30;
            }
        }
        else
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(30, 40, 350, 30), "TOKEN MANAGER MISSING!");
            GUI.color = Color.white;
        }

        // 2. Manual Controls
        if (GUI.Button(new Rect(30, 400, 100, 50), "RELOAD UI"))
        {
            UIAssembler.VerifyHUD();
        }
        
        if (GUI.Button(new Rect(140, 400, 100, 50), "ATTACK"))
        {
            InteractionController.Instance.StartTargeting("Physical");
        }

        if (GUI.Button(new Rect(250, 400, 100, 50), "WAIT"))
        {
            // Debug Log
            Debug.Log("Wait Clicked via Emergency UI");
        }
    }
}
