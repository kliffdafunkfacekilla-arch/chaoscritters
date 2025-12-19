import requests
import json

base_url = "http://127.0.0.1:8000"

def test_map():
    print("Testing /map/generate...")
    try:
        r = requests.post(f"{base_url}/map/generate", json={"radius": 5, "biome": "Standard"})
        print(f"Status: {r.status_code}")
        if r.status_code == 200:
            print("Response keys:", r.json().keys())
            print("Tile count:", len(r.json().get("tiles", [])))
        else:
            print("Error:", r.text)
    except Exception as e:
        print(f"Exception: {e}")

def test_battle():
    print("\nTesting /battle/start...")
    try:
        r = requests.post(f"{base_url}/battle/start")
        print(f"Status: {r.status_code}")
        print("Response:", r.text)
        return r.json()
    except Exception as e:
        print(f"Exception: {e}")
        return {}

def test_move(actor_id):
    print(f"\nTesting /battle/action/move for {actor_id}...")
    try:
        # Move back to origin or somewhere close
        r = requests.post(f"{base_url}/battle/action/move", json={"actor_id": actor_id, "target_pos": [0, 0]})
        print(f"Status: {r.status_code}")
        print("Response:", r.text)
    except Exception as e:
        print(f"Exception: {e}")

if __name__ == "__main__":
    test_map()
    data = test_battle()
    if data and "current_turn" in data:
        test_move(data["current_turn"])
