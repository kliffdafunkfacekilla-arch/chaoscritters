from typing import List, Dict, Any, Optional
from backend.engine.turn_manager import TurnManager, EntityState
from backend.engine.actions import ActionResolver
from backend.engine.mechanics import MechanicsEngine
from backend.engine.grid import Point

class AIEngine:
    def __init__(self, action_resolver: ActionResolver, mechanics: MechanicsEngine):
        self.resolver = action_resolver
        self.mechanics = mechanics
    
    def process_turn(self, actor: EntityState, turn_manager: TurnManager) -> Dict[str, Any]:
        """
        Calculates and executes the AI's action for the turn.
        Returns a dict describing what happened (for logging).
        """
        print(f"[AI] Processing turn for {actor.name} ({actor.id})")
        
        # 1. Identify Target (Nearest Player)
        target = self._find_nearest_target(actor, turn_manager)
        if not target:
            return {"action": "Wait", "message": "No targets found so I slept."}
            
        print(f"[AI] Target identified: {target.name}")
            
        # 2. Check Distance
        start = Point(actor.x, actor.y)
        end = Point(target.x, target.y)
        dist = start.distance(end)
        
        # 3. Decision Tree
        if dist <= 1:
            # Attack
            print(f"[AI] Attacking!")
            result = self.resolver.resolve_attack(actor, target, self.mechanics)
            if result["success"]:
                actor.ap -= result["cost"]
                return {
                    "action": "Attack", 
                    "target": target.id, 
                    "target_name": target.name,
                    "damage": result["mechanics"].get("damage_amount", 0),
                    "type": result["mechanics"].get("damage_type", "None")
                }
            else:
                 return {"action": "Wait", "message": "Tried to attack but failed."}
                 
        else:
            # Move towards target
            print(f"[AI] Moving!")
            # Simple Greedy Step: Reduce Largest Difference
            dx = end.x - start.x
            dy = end.y - start.y
            
            # Prefer axis with larger gap
            step_x, step_y = 0, 0
            
            if abs(dx) >= abs(dy):
                step_x = 1 if dx > 0 else -1
            else:
                step_y = 1 if dy > 0 else -1
                
            # Proposal
            new_x = actor.x + step_x
            new_y = actor.y + step_y
            
            # Validate
            # TODO: Should check if occupied by another unit, but backend resolver doesn't check collision yet? 
            # resolver.resolve_move DOES check AP. 
            # Let's try it.
            
            result = self.resolver.resolve_move(actor.id, (actor.x, actor.y), (new_x, new_y), actor.ap)
            
            if result["success"]:
                actor.ap -= result["cost"]
                actor.x = new_x
                actor.y = new_y
                return {
                    "action": "Move",
                    "from": (start.x, start.y),
                    "to": (new_x, new_y)
                }
            else:
                return {"action": "Wait", "message": "Wanted to move but stuck."}

    def _find_nearest_target(self, actor: EntityState, tm: TurnManager) -> Optional[EntityState]:
        # Iterate all entities, find one on different team
        nearest = None
        min_dist = 999
        
        my_pos = Point(actor.x, actor.y)
        
        for eid, entity in tm.entities.items():
            if entity.team != actor.team:
                print(f"[AI] Found hostile: {entity.name} at ({entity.x}, {entity.y})")
                other_pos = Point(entity.x, entity.y)
                d = my_pos.distance(other_pos)
                if d < min_dist:
                    min_dist = d
                    nearest = entity
                    
        return nearest
