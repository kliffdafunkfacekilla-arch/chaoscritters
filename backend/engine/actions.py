from typing import Dict, Any, Optional
from backend.engine.grid import Point, GridManager

class ActionResolver:
    def __init__(self, grid: GridManager):
        self.grid = grid

    def validate_move(self, start: Point, end: Point, max_distance: int) -> bool:
        # Manhattan distance for 4-way movement
        dist = abs(start.x - end.x) + abs(start.y - end.y)
        # TODO: Check terrain costs and obstacles
        return dist <= max_distance

    def resolve_move(self, actor_id: str, old_pos: tuple, new_pos: tuple, current_ap: int) -> Dict[str, Any]:
        """
        Validates and executes a move action.
        Returns result dict with success/failure and new state.
        """
        # Convert tuples to Points (x, y)
        start = Point(old_pos[0], old_pos[1])
        end = Point(new_pos[0], new_pos[1])

        cost = 1 # Placeholder for movement cost (usually 1 AP for a move action which allows X tiles)
        
        if current_ap < cost:
            return {"success": False, "message": "Not enough AP."}
            
        # Basic distance check (e.g., specific move limits from Agility)
        # For now, let's say 1 Move Action = 5 Tiles
        if start.distance(end) > 5:
             return {"success": False, "message": "Target too far for a single Move action."}

        return {
            "success": True, 
            "cost": cost,
            "new_pos": (end.x, end.y)
        }
             

    def resolve_attack(self, attacker: 'EntityState', target: 'EntityState', engine: 'MechanicsEngine') -> Dict[str, Any]:
        """
        Resolves an attack using the Mechanics Engine.
        """
        cost = 2 # Standard Action Cost
        if attacker.ap < cost:
            return {"success": False, "message": "Not enough AP."}
            
        # Distance check (Placeholder: Range 1 for melee)
        # In a real system, we'd check grid distance between attacker.pos and target.pos
        
        # Derived stats (Simple placeholder logic)
        atk_stat = 12 # attacker.stats['Might']
        def_stat = 10 # target.stats['Agility']
        
        result = engine.resolve_attack(atk_stat, 0, def_stat, 0)
        
        # Apply Damage
        damage = result.get("damage_amount", 0)
        damage_type = result.get("damage_type", "None")
        
        if damage > 0:
            if damage_type == "Meat":
                target.hp = max(0, target.hp - damage)
            elif damage_type == "Shock":
                target.composure = max(0, target.composure - damage)
                
        return {
            "success": True,
            "cost": cost,
            "mechanics": result,
            "target_state": {"hp": target.hp, "composure": target.composure}
        }
