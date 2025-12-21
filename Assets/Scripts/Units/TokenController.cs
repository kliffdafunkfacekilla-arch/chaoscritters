using System.Collections;
using UnityEngine;
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

        public void Initialize(EntityData data)
        {
            entityId = data.id;
            name = data.name;
            
            // Set initial position instantly
            transform.position = new Vector3(data.x, data.y, 0);
            targetPosition = transform.position;
            
            // visual assembly
            var assembler = GetComponent<SpriteAssembler>();
            if (assembler == null) assembler = gameObject.AddComponent<SpriteAssembler>();
            
            if (data.visual_tags != null)
            {
                // Convert struct to dict for the assembler
                var tags = new System.Collections.Generic.Dictionary<string, string>();
                if (!string.IsNullOrEmpty(data.visual_tags.chassis)) tags.Add("chassis", data.visual_tags.chassis);
                if (!string.IsNullOrEmpty(data.visual_tags.role)) tags.Add("role", data.visual_tags.role);
                if (!string.IsNullOrEmpty(data.visual_tags.infusion)) tags.Add("infusion", data.visual_tags.infusion);
                
                assembler.Assemble(tags);
            }
            else
            {
                // Fallback debug colors
                spriteRenderer.color = data.team == "Player" ? Color.green : Color.red; 
            } 
        }

        public void MoveTo(int x, int y)
        {
            targetPosition = new Vector3(x, y, 0);
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
