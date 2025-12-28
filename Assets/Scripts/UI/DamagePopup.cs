using UnityEngine;

namespace ChaosCritters.UI
{
    public class DamagePopup : MonoBehaviour
    {
        public static void Create(Vector3 position, int amount, Color color, string overrideText = null)
        {
            GameObject go = new GameObject("DamagePopup");
            go.transform.position = position;
            
            // Text (Legacy TextMesh for World Space without Canvas)
            TextMesh txt = go.AddComponent<TextMesh>(); 
            txt.text = (overrideText != null) ? overrideText : amount.ToString();
            txt.fontSize = 60; // Higher res
            txt.characterSize = 0.05f; // Scale down
            txt.color = color;
            txt.anchor = TextAnchor.MiddleCenter;
            txt.alignment = TextAlignment.Center;
            
            DamagePopup popup = go.AddComponent<DamagePopup>();
        }

        private void Update()
        {
            transform.position += Vector3.up * 2f * Time.deltaTime;
            
            // Fade out
            var txt = GetComponent<TextMesh>();
            if (txt != null)
            {
                var col = txt.color;
                col.a -= Time.deltaTime;
                txt.color = col;

                if (col.a <= 0) Destroy(gameObject);
            }
        }
    }
}
