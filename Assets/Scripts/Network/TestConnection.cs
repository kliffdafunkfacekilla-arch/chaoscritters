using UnityEngine;
using ChaosCritters.Network;

public class TestConnection : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("TestConnection: Attempting to contact backend...");
        
        NetworkManager.Instance.Get("/", 
            onSuccess: (response) => 
            {
                Debug.Log($"<color=green>SUCCESS:</color> Backend says: {response}");
            },
            onError: (error) => 
            {
                Debug.LogError($"<color=red>FAILURE:</color> Could not reach backend. Error: {error}");
            }
        );
    }
}
