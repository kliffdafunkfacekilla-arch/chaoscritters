using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using ChaosCritters.Network;

namespace ChaosCritters.UI
{
    public class ClashUIController : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panelRoot;
        public Text statusText;
        public Button btnPress;
        public Button btnManeuver;
        public Button btnDisengage;
        public Button btnTactic;
        
        [Header("State")]
        public string currentAttackerId;
        public string currentDefenderId;
        
        private void Start()
        {
            // Auto-wire listeners if buttons are assigned
            if(btnPress) btnPress.onClick.AddListener(() => OnCardSelected("PRESS"));
            if(btnManeuver) btnManeuver.onClick.AddListener(() => OnCardSelected("MANEUVER"));
            if(btnDisengage) btnDisengage.onClick.AddListener(() => OnCardSelected("DISENGAGE"));
            if(btnTactic) btnTactic.onClick.AddListener(() => OnCardSelected("TACTIC"));
            
            if(panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);
        }
        
        public void Show(string attackerId, string defenderId)
        {
            currentAttackerId = attackerId;
            currentDefenderId = defenderId;
            
            statusText.text = "CLASH! Choose your card!";
            panelRoot.SetActive(true);
        }
        
        public void Hide()
        {
            panelRoot.SetActive(false);
        }
        
        public void OnCardSelected(string card)
        {
            Debug.Log($"[Clash] Selected: {card}");
            statusText.text = "Waiting for resolution...";
            
            // For now, since we don't have a backend "wait for opponent" state for PVE, 
            // we assume the AI (if opponent) picks randomly or we send our pick to a resolution endpoint.
            // The current server endpoint /mechanics/clash expects both cards.
            // So we need to simulate the opponent pick here OR have a better backend flow.
            // Plan: Ask server to resolve against a random/AI pick.
            
            // We need to fetch the opponent's card (Simulated for this implementation).
            string opponentCard = PickRandomCard(); // Temporary client-side simulation for PVE
            
            ResolveClash(card, opponentCard);
        }
        
        private string PickRandomCard()
        {
            string[] cards = { "PRESS", "MANEUVER", "DISENGAGE", "TACTIC" };
            return cards[Random.Range(0, cards.Length)];
        }
        
        private void ResolveClash(string myCard, string oppCard)
        {
             // Construct JSON manually
            string json = $"{{\"attacker_card\": \"{myCard}\", \"defender_card\": \"{oppCard}\"}}";
            
            NetworkManager.Instance.Post("/mechanics/clash", json,
                onSuccess: (response) => 
                {
                    Debug.Log($"[Clash] Result: {response}");
                    var res = JsonUtility.FromJson<ClashResult>(response);
                    StartCoroutine(ShowResultRoutine(res));
                },
                onError: (err) => 
                {
                    statusText.text = $"Error: {err}";
                    Debug.LogError(err);
                }
            );
        }
        
        private IEnumerator ShowResultRoutine(ClashResult res)
        {
            statusText.text = res.message;
            yield return new WaitForSeconds(3.0f);
            Hide();
        }
        
        [System.Serializable]
        public class ClashResult
        {
            public string winner;
            public string message;
        }
    }
}
