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
            
        # Validate Destination
        if not self.grid.is_in_bounds(end):
            return {"success": False, "message": f"Target {end} is out of bounds."}

        # Basic distance check (e.g., specific move limits from Agility)
        # For now, let's say 1 Move Action = 5 Tiles
        dist = start.distance(end)
        if dist > 5:
             return {"success": False, "message": f"Target too far ({dist} > 5)."}

        return {
            "success": True, 
            "cost": cost,
            "new_pos": (end.x, end.y)
        }
             

    def resolve_attack(self, attacker: 'EntityState', target: 'EntityState', engine: 'MechanicsEngine') -> Dict[str, Any]:
        """
        Resolves an attack using the Mechanics Engine.
        """
        cost = 1 # Standard Action Cost (AP)
        stamina_cost = 0 # Basic Attack is Free (Stamina-wise)
        
        # 1. Check AP
        if attacker.ap < cost:
            return {"success": False, "message": "Not enough AP."}
            
        # 2. Check Stamina (Fuel)
        if attacker.stamina < stamina_cost:
            return {"success": False, "message": "Not enough Stamina!"}
            
        # Distance check (Range 1 for melee)
        start = Point(attacker.x, attacker.y)
        end = Point(target.x, target.y)
        
        # Manhattan distance for grid
        dist = abs(start.x - end.x) + abs(start.y - end.y)
        
        attack_range = 1 # Hardcoded for now (Melee)
        
        if dist > attack_range:
             return {"success": False, "message": f"Target out of range ({dist} > {attack_range})."}
        
        # Derived stats (Simple placeholder logic)
        # Try to use stats from entity if they exist, else defaults
        atk_stat = attacker.stats.get('Might', 12)
        def_stat = target.stats.get('Reflexes', 10) # Reflex vs Might for Defense usually? Or Parrying?
        
        result = engine.resolve_attack(atk_stat, 0, def_stat, 0)
        
        # Apply Damage
        damage = result.get("damage_amount", 0)
        damage_type = result.get("damage_type", "None")
        
        # FORCE MIN DAMAGE FOR TESTING
        if damage <= 0:
            damage = 1
            damage_type = "Meat" # Ensure it hurts HP if mechanics fail
            
        # Update Dictionary so Frontend sees it Correctly
        result["damage_amount"] = damage
        result["damage_type"] = damage_type
        
        if damage > 0:
            if damage_type == "Meat":
                target.hp = max(0, target.hp - damage)
            elif damage_type == "Shock":
                target.composure = max(0, target.composure - damage)
                
        return {
            "success": True,
            "cost": cost,
            "resource_cost": stamina_cost,
            "resource_type": "stamina", 
            "mechanics": result,
            "target_state": {"hp": target.hp, "composure": target.composure}
        }
