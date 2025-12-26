from fastapi import FastAPI, HTTPException
from fastapi.staticfiles import StaticFiles
from fastapi.responses import HTMLResponse
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional, Dict
import os
import sys

# Add the parent directory to sys.path to import engine modules
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from backend.engine.mechanics import MechanicsEngine

app = FastAPI(title="The Shattered World Backend")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

engine = MechanicsEngine()
from backend.engine.grid import GridManager
from backend.engine.procgen import ProcGen

from backend.engine.turn_manager import TurnManager, EntityState
from backend.engine.actions import ActionResolver

from backend.engine.session import SessionManager
from backend.interface.voice import VoiceInterface
from backend.brain.llm_client import LLMClient
from backend.brain.parser_agent import ParserAgent
from backend.brain.narrator_agent import NarratorAgent

grid_manager = GridManager(radius=5)
grid_manager.generate_empty_map() # Initialize default map
proc_gen = ProcGen()
turn_manager = TurnManager()
action_resolver = ActionResolver(grid_manager)
session_manager = SessionManager()
voice_interface = VoiceInterface()

# Initialize Brain
llm_client = LLMClient() 
# llm_client.check_connection() # Optional: check on startup
parser_agent = ParserAgent(llm_client)
narrator_agent = NarratorAgent(llm_client)

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
async def generate_map(request: MapRequest):
    # Use Global GridManager
    global grid_manager 
    grid_manager.radius = request.radius
    grid_manager.generate_empty_map()
    
    proc_gen = ProcGen()
    map_data = proc_gen.generate_terrain(grid_manager, biome_type=request.biome)
    
    # Serialize for Frontend
    tiles = []
    for (x, y), data in map_data.items():
        tiles.append({
            "x": x, 
            "y": y, 
            "terrain": data["type"],
            "cost": data["cost"],
            "height": data["height"]
        })
        
    return {
        "radius": request.radius,
        "biome": request.biome,
        "tile_count": len(tiles),
        "tiles": tiles
    }

@app.post("/battle/start")
async def start_battle():
    # Setup dummy entities for testing
    p1 = EntityState(
        id="P1", name="Ursine Warrior", 
        hp=30, max_hp=30, composure=15, max_composure=15, 
        x=2, y=2, image_id="Warrior", initiative=100,
        visual_tags={"chassis": "Bear", "role": "Warrior", "infusion": "Nature"}
    )
    e1 = EntityState(
        id="E1", name="Gravity Bear", 
        hp=40, max_hp=40, composure=10, max_composure=10, team="Enemy", 
        x=5, y=5, image_id="Bear",
        visual_tags={"chassis": "Bear", "role": "Breaker", "infusion": "Gravity"}
    )
    
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

@app.get("/entities")
async def get_entities():
    return {"entities": list(turn_manager.entities.values())}


@app.get("/battle/state")
async def get_battle_state():
    current = turn_manager.get_current_actor()
    return {
        "turn_order": turn_manager.turn_order,
        "current_turn": current.id if current else None,
        "round": turn_manager.round
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
    current_pos = (actor.x, actor.y)
    print(f"[Move] Request for {req.actor_id}: {current_pos} -> {req.target_pos}")
    
    result = action_resolver.resolve_move(
        req.actor_id, 
        current_pos, 
        tuple(req.target_pos), 
        actor.ap
    )
    
    if result["success"]:
        actor.ap -= result["cost"]
        new_pos = result.get("new_pos")
        if new_pos:
            actor.x = new_pos[0]
            actor.y = new_pos[1]
            print(f"[Move] Success. New Pos: ({actor.x}, {actor.y}). AP: {actor.ap}")
    else:
        print(f"[Move] Failed: {result.get('message')}")
        
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



from backend.engine.ai_engine import AIEngine
ai_engine = AIEngine(action_resolver, engine)

@app.post("/battle/turn/end")
async def end_turn():
    # 1. Advance to next actor initially
    current = turn_manager.next_turn()
    log_events = []
    ai_actions = [] 
    
    # 2. Loop while it is an Enemy's turn
    # Safety break: max 10 steps to prevent infinite loops if everyone is AI
    steps = 0
    while current.team == "Enemy" and steps < 10:
        steps += 1
        
        try:
            # Calculate Action
            print(f"[Loop] Processing AI Turn for {current.id} ({current.name})")
            action_log = ai_engine.process_turn(current, turn_manager)
            
            # Enrich with actor_id for frontend animation
            action_log["actor_id"] = current.id
            ai_actions.append(action_log)
            
            log_events.append(f"{current.name}: {action_log}")
            
            # IMPORTANT: ai_engine.process_turn MUST update backend state (AP, Position, HP)
            # We assume it does.
            
        except Exception as e:
            print(f"[Loop] CRITICAL AI ERROR: {e}")
            log_events.append(f"{current.name}: ERROR {e}")

        # Advance Turn
        current = turn_manager.next_turn()
        print(f"[Loop] Advanced to {current.id}. Team: {current.team}")
        
    # AI Hook: If next actor is Enemy/AI, we should trigger AI logic here or return a flag
    # For now, just return the state
    
    # Narrator needs to speak these events?
    narrative = ""
    if log_events:
        narrative = narrator_agent.narrate_event(log_events) # Just feed raw logs for now

    return {
        "message": "Turn Ended",
        "current_turn": current.id,
        "round": turn_manager.round,
        "narrative": narrative,
        "ai_actions": ai_actions
    }

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

from fastapi import File, UploadFile
import shutil

@app.post("/interface/stt")
async def speech_to_text(file: UploadFile = File(...)):
    temp_file = f"temp_{file.filename}"
    with open(temp_file, "wb") as buffer:
        shutil.copyfileobj(file.file, buffer)
        
    text = voice_interface.transcribe(temp_file)
    
    # Cleanup
    if os.path.exists(temp_file):
        os.remove(temp_file)
        
    return {"text": text}

class TTSRequest(BaseModel):
    text: str

from fastapi.responses import FileResponse

@app.post("/interface/tts")
async def text_to_speech(req: TTSRequest):
    output_file = "tts_output.wav"
    path = voice_interface.speak(req.text, output_file)
    if path and os.path.exists(path):
        return FileResponse(path, media_type="audio/wav", filename="speech.wav")
    raise HTTPException(status_code=500, detail="TTS generation failed")


class CommandRequest(BaseModel):
    text: str
    actor_id: str

@app.post("/brain/command")
async def process_command(req: CommandRequest):
    actor = turn_manager.entities.get(req.actor_id)
    if not actor:
        raise HTTPException(status_code=404, detail="Actor not found")
        
    # Get visible entities (for now, all)
    visible = list(turn_manager.entities.values())
    
    # Parse
    intent = parser_agent.parse_command(req.text, actor, visible)
    
    # Execute if valid (Simplified)
    execution_result = {}
    if intent.get("action") == "Move":
        params = intent.get("params", {})
        target = params.get("target_pos")
        if target:
            # We assume LLM returns correct coords, or we validate
            current_pos = (0, 0)
            execution_result = action_resolver.resolve_move(actor.id, current_pos, tuple(target), actor.ap)
            if execution_result["success"]:
                actor.ap -= execution_result["cost"]
                
    elif intent.get("action") == "Attack":
        params = intent.get("params", {})
        target_id = params.get("target_id")
        target_entity = turn_manager.entities.get(target_id)
        if target_entity:
            execution_result = action_resolver.resolve_attack(actor, target_entity, engine)
            if execution_result["success"]:
                actor.ap -= execution_result["cost"]

    # Narrate
    narrative = ""
    if execution_result.get("success"):
        narrative = narrator_agent.narrate_event([f"{actor.name} performed {intent.get('action')}", str(execution_result)])

    return {
        "intent": intent,
        "execution": execution_result,
        "narrative": narrative
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
