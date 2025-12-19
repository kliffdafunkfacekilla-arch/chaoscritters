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
from backend.engine.procgen import ProcGen

from backend.engine.turn_manager import TurnManager, EntityState
from backend.engine.actions import ActionResolver

from backend.engine.session import SessionManager

grid_manager = GridManager(radius=5)
# grid_manager.generate_empty_map() # Procgen will handle this
proc_gen = ProcGen()
turn_manager = TurnManager()
action_resolver = ActionResolver(grid_manager)
session_manager = SessionManager()

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
    
    # Use Procedural Generation
    map_data = proc_gen.generate_terrain(grid_manager, req.biome)
    
    tile_data = []
    for (q, r), data in map_data.items():
        tile_data.append({
            "q": q, "r": r, 
            "terrain": data["type"],
            "cost": data["cost"],
            "height": data["height"]
        })
        
    return {
        "radius": req.radius,
        "biome": req.biome,
        "tile_count": len(tile_data),
        "tiles": tile_data
    }

@app.post("/battle/start")
async def start_battle():
    # Setup dummy entities for testing
    p1 = EntityState(id="P1", name="Ursine Warrior", hp=30, max_hp=30, composure=15, max_composure=15)
    e1 = EntityState(id="E1", name="Gravity Bear", hp=40, max_hp=40, composure=10, max_composure=10, team="Enemy")
    
    turn_manager.entities = {}
    turn_manager.add_entity(p1)
    turn_manager.add_entity(e1)
    
    turn_manager.roll_initiative()
    current = turn_manager.get_current_actor()
    
    return {
        "message": "Battle Started",
        "turn_order": turn_manager.turn_order,
        "current_turn": current.id
    }


# --- Action Models ---
class MoveRequest(BaseModel):
    actor_id: str
    target_pos: list  # [q, r]
    
@app.post("/battle/action/move")
async def execute_move(req: MoveRequest):
    actor = turn_manager.entities.get(req.actor_id)
    if not actor:
        raise HTTPException(status_code=404, detail="Actor not found")
        
    # Placeholder for current position mechanism (would be in EntityState eventually)
    current_pos = (0, 0) 
    
    result = action_resolver.resolve_move(
        req.actor_id, 
        current_pos, 
        tuple(req.target_pos), 
        actor.ap
    )
    
    if result["success"]:
        actor.ap -= result["cost"]
        
    return {"result": result, "remaining_ap": actor.ap}




# --- Action Models ---
class BattleAttackRequest(BaseModel):
    actor_id: str
    target_id: str

@app.post("/battle/action/attack")
async def execute_attack(req: BattleAttackRequest):
    attacker = turn_manager.entities.get(req.actor_id)
    target = turn_manager.entities.get(req.target_id)
    
    if not attacker or not target:
        raise HTTPException(status_code=404, detail="Entity not found")
        
    result = action_resolver.resolve_attack(attacker, target, engine)
    
    if result["success"]:
        attacker.ap -= result["cost"]
        
    return {"result": result, "attacker_ap": attacker.ap}


# --- Session Models ---
class SessionRequest(BaseModel):
    session_id: str

@app.post("/session/save")
async def save_session(req: SessionRequest):
    # Placeholder history
    history = ["Battle started", "P1 moved", "P1 attacked E1"] 
    path = session_manager.save_game(req.session_id, turn_manager, grid_manager, history)
    return {"message": "Game Saved", "path": path}

@app.post("/session/load")
async def load_session(req: SessionRequest):
    success = session_manager.load_game(req.session_id, turn_manager, grid_manager)
    if not success:
        raise HTTPException(status_code=404, detail="Save file not found")
        
    current = turn_manager.get_current_actor()
    return {
        "message": "Game Loaded",
        "round": turn_manager.round,
        "current_turn": current.id if current else "None",
        "map_tiles": len(grid_manager.cells)
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
