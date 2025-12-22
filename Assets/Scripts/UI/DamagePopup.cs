using UnityEngine;
using TMPro;

namespace ChaosCritters.UI
{
    public class DamagePopup : MonoBehaviour
    {
        public static void Create(Vector3 position, int amount, Color color)
        {
            GameObject go = new GameObject("DamagePopup");
            go.transform.position = position;
            
            // Text
            TextMeshPro txt = go.AddComponent<TextMeshPro>(); // World Space Text
            txt.text = amount.ToString();
            txt.fontSize = 6;
            txt.color = color;
            txt.alignment = TextAlignmentOptions.Center;
            
            DamagePopup popup = go.AddComponent<DamagePopup>();
        }

        private void Update()
        {
            transform.position += Vector3.up * 2f * Time.deltaTime;
            
            // Fade out?
            var txt = GetComponent<TextMeshPro>();
            var col = txt.color;
            col.a -= Time.deltaTime;
            txt.color = col;

            if (col.a <= 0) Destroy(gameObject);
        }
    }
}
