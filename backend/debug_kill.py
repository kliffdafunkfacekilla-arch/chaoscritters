
import requests
import json

BASE_URL = "http://localhost:8000"

def test_kill():
    print("--- Starting Kill Test ---")
    requests.post(f"{BASE_URL}/battle/start")
    
    # Get initial state
    res = requests.get(f"{BASE_URL}/entities").json()
    e1 = next((e for e in res["entities"] if e["id"] == "E1"), None)
    p1 = next((e for e in res["entities"] if e["id"] == "P1"), None)
    
    if not e1: 
        print("Enemy not found at start!")
        return

    print(f"Enemy HP: {e1['hp']}")
    print(f"Player AP: {p1['ap']}")
    
    # 1. Move into range
    # P1 at (2,2), E1 at (5,5). Range 1 needed.
    # Move to (4, 5) is 3 steps. Cost 3 AP. Leaving 2 AP. 
    # 1 Attack possible.
    
    print("Moving into range (4,5)...")
    requests.post(f"{BASE_URL}/battle/action/move", json={"actor_id": "P1", "target_pos": [4, 5]})
    
    # Attack loop
    
    payload = {"actor_id": "P1", "target_id": "E1"}
    
    for i in range(5):
        print(f"\nAttack {i+1}")
        resp = requests.post(f"{BASE_URL}/battle/action/attack", json=payload).json()
        
        if resp['result']['success']:
            dmg = resp['result']['mechanics']['damage_amount']
            print(f"Dealt {dmg} damage.")
            
            # Check Entity State
            res = requests.get(f"{BASE_URL}/entities").json()
            e1_check = next((e for e in res["entities"] if e["id"] == "E1"), None)
            
            if e1_check:
                print(f"Enemy HP Now: {e1_check['hp']}")
            else:
                print("Enemy is DEAD (Not found in entities list). SUCCESS.")
                return
        else:
            print(f"Attack Failed: {resp['result']['message']}")
            # Refresh AP?
            requests.post(f"{BASE_URL}/battle/turn/end")
            requests.post(f"{BASE_URL}/battle/turn/end") # AI turn
            # P1 again?
            
if __name__ == "__main__":
    test_kill()
