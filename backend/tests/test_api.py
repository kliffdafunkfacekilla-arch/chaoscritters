from fastapi.testclient import TestClient
import sys
import os

# Add parent dir to path
sys.path.append(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

from backend.server import app
from backend.engine.mechanics import MechanicsEngine

client = TestClient(app)
engine = MechanicsEngine()

def test_root():
    response = client.get("/")
    assert response.status_code == 200
    assert response.json() == {"message": "The Shattered World Backend is Online"}

def test_stats_load():
    response = client.get("/stats")
    assert response.status_code == 200
    data = response.json()
    assert "kingdoms" in data
    assert "Mammal" in data["kingdoms"]

def test_attack_mechanic():
    # Test a guaranteed hit case (High stats vs Low stats)
    # Attack: 20 stat + 10 skill = 30 min
    # Defense: 0 stat + 0 skill = 20 max (d20)
    # Margin should be at least 10 -> CRIT
    payload = {
        "attack_stat": 20,
        "skill_bonus": 10,
        "defense_stat": 0,
        "armor_bonus": 0
    }
    response = client.post("/mechanics/attack", json=payload)
    assert response.status_code == 200
    data = response.json()
    assert data["result"] in ["HIT", "CRIT"]

def test_clash_mechanic():
    payload = {
        "attacker_card": "PRESS",
        "defender_card": "MANEUVER"
    }
    response = client.post("/mechanics/clash", json=payload)
    assert response.status_code == 200
    data = response.json()
    assert data["winner"] == "ATTACKER"

def test_engine_pools():
    stats = {"Might": 12, "Endurance": 10} 
    # Stamina = (12+10)//2 = 11
    pools = engine.calculate_pools(stats)
    assert pools["Stamina"] == 11
