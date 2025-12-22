using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace ChaosCritters.UI
{
    public class NarratorController : MonoBehaviour
    {
        public static NarratorController Instance { get; private set; }

        [Header("UI References")]
        public TMP_Text feedText;
        public int maxLines = 5;

        private Queue<string> _log = new Queue<string>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void AddLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            _log.Enqueue(text);
            if (_log.Count > maxLines) _log.Dequeue();

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (feedText != null)
            {
                feedText.text = string.Join("\n", _log);
            }
        }
    }
}
