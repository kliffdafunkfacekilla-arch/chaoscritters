using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ChaosCritters.Data;

namespace ChaosCritters.Units
{
    public class TokenController : MonoBehaviour
    {
        public string entityId;
        public SpriteRenderer spriteRenderer;
        
        // Configuration
        public float moveSpeed = 5f;

        private Vector3 targetPosition;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // UI
        private Transform healthBarFill;
        private Vector3 healthBarScale;

        public void Initialize(EntityData data)
        {
            entityId = data.id;
            name = data.name;
            
            // Set initial position instantly with 0.5f offset for cell center
            transform.position = new Vector3(data.x + 0.5f, data.y + 0.5f, 0);
            targetPosition = transform.position;
            
            // visual assembly
            var assembler = GetComponent<SpriteAssembler>();
            if (assembler == null) assembler = gameObject.AddComponent<SpriteAssembler>();
            
            // Convert struct to dict for the assembler
            var tags = new System.Collections.Generic.Dictionary<string, string>();
            if (data.visual_tags != null)
            {
                if (!string.IsNullOrEmpty(data.visual_tags.chassis)) tags.Add("chassis", data.visual_tags.chassis);
                if (!string.IsNullOrEmpty(data.visual_tags.role)) tags.Add("role", data.visual_tags.role);
                if (!string.IsNullOrEmpty(data.visual_tags.infusion)) tags.Add("infusion", data.visual_tags.infusion);
            }
            
            // Always assemble! This ensures layers are setup and fallbacks are used if tags are missing
            assembler.Assemble(tags);
            
            // Override color if no visual tags were present only?
            // Actually Assembler handles fallback colors.
            // But if we want team colors for basic shapes:
            if (tags.Count == 0 && assembler.bodyLayer != null)
            {
                 assembler.bodyLayer.color = data.team == "Player" ? Color.green : Color.red;
            }
            
            CreateHealthBar();
            UpdateHealth(data.hp, data.max_hp);
        }

        private void CreateHealthBar()
        {
            // Create Canvas
            GameObject canvasGO = new GameObject("WorldCanvas");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = new Vector3(0, 0.75f, -0.1f); // Slightly forward
            
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100; // Force on top of sprites (usually 0)
            
            // Scale logic: 
            // We want the bar to be ~0.8 units wide.
            // If Text sizes are pixels, we need a balance.
            // Let's use 0.01 scale and 80 width.
            RectTransform rt = canvasGO.GetComponent<RectTransform>();
            rt.localScale = new Vector3(0.01f, 0.01f, 1f);
            rt.sizeDelta = new Vector2(80, 15); 
            
            // Background (White Border)
            GameObject border = new GameObject("Border");
            border.transform.SetParent(canvasGO.transform, false);
            Image borderImg = border.AddComponent<Image>();
            borderImg.color = Color.white;
            border.GetComponent<RectTransform>().sizeDelta = new Vector2(82, 17); // Slightly larger
            
            // Dark Background
            GameObject bg = new GameObject("Bg");
            bg.transform.SetParent(canvasGO.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = Color.black;
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 15);
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(canvasGO.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = Color.red;
            
            RectTransform fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fillRT.pivot = new Vector2(0, 0.5f); // Pivots left
            
            healthBarFill = fill.transform;
            healthBarScale = Vector3.one;
        }

        public void UpdateHealth(int current, int max)
        {
            if (healthBarFill != null && max > 0)
            {
                float pct = Mathf.Clamp01((float)current / max);
                healthBarFill.localScale = new Vector3(pct, 1, 1);
            }
        }

        public void MoveTo(int x, int y)
        {
            // Add offset here too
            targetPosition = new Vector3(x + 0.5f, y + 0.5f, 0);
            StopAllCoroutines();
            StartCoroutine(SmooothMove());
        }

        private IEnumerator SmooothMove()
        {
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPosition;
        }
    }
}
