from fastapi import FastAPI, HTTPException
from fastapi.staticfiles import StaticFiles
from fastapi.responses import HTMLResponse
from pydantic import BaseModel
from typing import Optional, Dict
import os
import sys

# Add the parent directory to sys.path to import engine modules
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from backend.engine.mechanics import MechanicsEngine

app = FastAPI(title="The Shattered World Backend")

engine = MechanicsEngine()
from backend.engine.grid import GridManager

grid_manager = GridManager(radius=5)
grid_manager.generate_empty_map()

# --- Models ---
class MapRequest(BaseModel):
    radius: int = 5
    biome: str = "Standard"
class RollRequest(BaseModel):
    sides: int = 20
    count: int = 1

class AttackRequest(BaseModel):
    attack_stat: int
    skill_bonus: int = 0
    defense_stat: int
    armor_bonus: int = 0
    defense_skill_bonus: int = 0

class ClashRequest(BaseModel):
    attacker_card: str
    defender_card: str

# --- Endpoints ---

@app.get("/")
async def root():
    return {"message": "The Shattered World Backend is Online"}

@app.get("/data/stats")
async def get_stats():
    return engine.stats

@app.get("/data/talents")
async def get_talents():
    return engine.talents

@app.get("/data/abilities")
async def get_abilities():
    return engine.abilities

@app.get("/data/all")
async def get_all_data():
    return engine.data

@app.post("/roll")
async def roll_dice(req: RollRequest):
    result = engine.roll_dice(req.sides, req.count)
    return {"sides": req.sides, "count": req.count, "total": result}

@app.post("/mechanics/attack")
async def resolve_attack(req: AttackRequest):
    result = engine.resolve_attack(
        req.attack_stat, req.skill_bonus,
        req.defense_stat, req.armor_bonus, req.defense_skill_bonus
    )
    return result


# --- Character Validation Models ---
class Attributes(BaseModel):
    Might: int = 10
    Endurance: int = 10
    Agility: int = 10
    Perception: int = 10
    Logic: int = 10
    Knowledge: int = 10
    Willpower: int = 10
    Charm: int = 10

class CharacterSheet(BaseModel):
    name: str
    lineage: str
    heritage: str
    background: str
    stats: Attributes
    skills: Dict[str, int] = {}
    talents: list[str] = []
    abilities: list[str] = []
    
@app.post("/validate/character")
async def validate_character(sheet: CharacterSheet):
    # Basic logic validation
    issues = []
    
    # Check if stats are within range (example 0-20)
    for stat, val in sheet.stats.dict().items():
        if not (0 <= val <= 20):
            issues.append(f"Stat '{stat}' value {val} is out of range (0-20).")
            
    # Check if lineage exists
    if sheet.lineage not in engine.talents.get("Lineages", {}):
        issues.append(f"Lineage '{sheet.lineage}' not recognized.")

    if issues:
        return {"valid": False, "issues": issues}
    return {"valid": True, "message": "Character Sheet is valid."}

@app.post("/map/generate")
async def generate_map(req: MapRequest):
    grid_manager.radius = req.radius
    grid_manager.generate_empty_map()
    # Simple placeholder: Fill with random types for now
    import random
    types = ["Grass", "Dirt", "Stone", "Water"]
    count = 0
    tile_data = []
    
    for (q, r), _ in grid_manager.cells.items():
        terrain = random.choice(types)
        grid_manager.cells[(q, r)] = terrain
        tile_data.append({"q": q, "r": r, "terrain": terrain})
        count += 1
        
    return {
        "radius": req.radius,
        "biome": req.biome,
        "tile_count": count,
        "tiles": tile_data
    }

@app.post("/mechanics/clash")
async def resolve_clash(req: ClashRequest):
    result = engine.wheel_of_pain(req.attacker_card, req.defender_card)
    return result

# Serve static files for simple testing UI if needed
# app.mount("/static", StaticFiles(directory="static"), name="static")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
