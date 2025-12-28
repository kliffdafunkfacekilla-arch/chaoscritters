using UnityEngine;
using UnityEngine.UI;

using ChaosCritters.Data;

namespace ChaosCritters.UI
{
    public class StatBar : MonoBehaviour
    {
        [Header("UI References")]
        public Image fillImage;
        public UnityEngine.UI.Text valueText;

        [Header("Settings")]
        public Color barColor = Color.white;
        public float animationSpeed = 5f;

        private float _targetFillAmount = 1f;

        private void Awake()
        {
            if (fillImage != null) fillImage.color = barColor;
        }

        private void Update()
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, _targetFillAmount, Time.deltaTime * animationSpeed);
            }
        }

        public void SetValues(int current, int max)
        {
            float fill = (max > 0) ? (float)current / max : 0f;
            _targetFillAmount = Mathf.Clamp01(fill);

            if (valueText != null)
            {
                valueText.gameObject.SetActive(true);
                valueText.text = $"{current} / {max}";
            }
        }
    }
}
