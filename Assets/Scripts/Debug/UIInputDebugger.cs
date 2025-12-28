using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ChaosCritters.DebugTools
{
    public class UIInputDebugger : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            GameObject go = new GameObject("UI_Input_Debugger");
            go.AddComponent<UIInputDebugger>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                if (results.Count > 0)
                {
                    Debug.Log($"<color=cyan>[UI Debug] Clicked at {Input.mousePosition}. Hit {results.Count} objects:</color>");
                    foreach (var result in results)
                    {
                        Debug.Log($" - <b>{result.gameObject.name}</b> (Depth: {result.depth}, Sort: {result.sortingOrder})");
                    }
                }
                else
                {
                    Debug.Log($"<color=orange>[UI Debug] Clicked at {Input.mousePosition} but hit NOTHING UI-wise.</color>");
                }
            }
        }
    }
}
