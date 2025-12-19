import json
import os
from typing import Dict, List, Optional
from datetime import datetime
from pydantic import BaseModel

from backend.engine.turn_manager import EntityState, TurnManager
from backend.engine.grid import GridManager

SAVE_DIR = "saves"

class GameState(BaseModel):
    session_id: str
    timestamp: str
    round: int
    turn_index: int
    entities: Dict[str, EntityState]
    map_data: Dict[str, dict] # Simplified map storage
    history: List[str] = []

class SessionManager:
    def __init__(self):
        if not os.path.exists(SAVE_DIR):
            os.makedirs(SAVE_DIR)
            
    def save_game(self, session_id: str, turn_manager: TurnManager, grid: GridManager, history: List[str]):
        """Saves the current game state to a JSON file."""
        try:
            # Serialize grid
            map_serialized = {}
            for coord, type_str in grid.cells.items():
                key = f"{coord[0]},{coord[1]}"
                map_serialized[key] = {"type": type_str} 
                
            state = GameState(
                session_id=session_id,
                timestamp=datetime.now().isoformat(),
                round=turn_manager.round,
                turn_index=turn_manager.current_index,
                entities=turn_manager.entities,
                map_data=map_serialized,
                history=history
            )
            
            file_path = os.path.join(SAVE_DIR, f"{session_id}.json")
            with open(file_path, 'w') as f:
                json.dump(state.dict(), f, indent=4, default=str)
                
            print(f"Game saved to {file_path}")
            return file_path
        except Exception as e:
            print(f"FAILED TO SAVE GAME: {e}")
            import traceback
            traceback.print_exc()
            raise e

    def load_game(self, session_id: str, turn_manager: TurnManager, grid: GridManager) -> bool:
        """Loads a game state from JSON."""
        file_path = os.path.join(SAVE_DIR, f"{session_id}.json")
        if not os.path.exists(file_path):
            print("Save file not found.")
            return False
            
        try:
            with open(file_path, 'r') as f:
                data = json.load(f)
                
            # Restore Turn Logic
            turn_manager.round = data['round']
            turn_manager.current_index = data['turn_index']
            turn_manager.entities = {k: EntityState(**v) for k,v in data['entities'].items()}
            turn_manager.turn_order = sorted(
                turn_manager.entities.keys(), 
                key=lambda x: turn_manager.entities[x].initiative, 
                reverse=True
            ) 
            
            # Restore Map
            grid.cells = {}
            for k, v in data['map_data'].items():
                parts = k.split(',')
                q, r = int(parts[0]), int(parts[1])
                grid.cells[(q, r)] = v['type']
                
            print(f"Game loaded from {file_path}")
            return True
        except Exception as e:
            print(f"FAILED TO LOAD SAVE: {e}")
            import traceback
            traceback.print_exc()
            return False
