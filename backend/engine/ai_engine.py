from typing import List, Dict, Any, Optional
from backend.engine.turn_manager import TurnManager, EntityState
from backend.engine.actions import ActionResolver
from backend.engine.mechanics import MechanicsEngine
from backend.engine.grid import Point

from backend.engine.abilities import AbilityResolver, DB

class AIEngine:
    def __init__(self, action_resolver: ActionResolver, ability_resolver: AbilityResolver, mechanics: MechanicsEngine):
        self.resolver = action_resolver
        self.ability_resolver = ability_resolver
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
            
        # 2. Check State & Distance
        start = Point(actor.x, actor.y)
        end = Point(target.x, target.y)
        dist = start.distance(end)
        
        # RETREAT LOGIC: If HP < 30%, try to run away
        is_low_health = (actor.hp / actor.max_hp) < 0.3
        
        if is_low_health:
            print(f"[AI] Low Health ({actor.hp}/{actor.max_hp})! Attempting Retreat.")
            move_res = self._attempt_move(actor, start, end, retreat=True, tm=turn_manager)
            if move_res: return move_res
            # If cannot retreat, fight desperately
        
        # 3. Decision Tree (Aggressive)
        
        # 3a. Try to use Best Skill
        best_skill = self._pick_best_skill(actor, target, dist)
        
        if best_skill:
            skill_id, predicted_dmg = best_skill
            print(f"[AI] Using Best Skill: {skill_id} (Est Dmg: {predicted_dmg}) on {target.name}")
            result = self.ability_resolver.resolve_ability(skill_id, actor, target)
            
            if result["success"]:
                 actor.ap -= result["cost"] 
                 
                 # Deduct resources
                 r_cost = result.get("resource_cost", 0)
                 r_type = result.get("resource_type", "")
                 if r_type == "stamina": actor.stamina = max(0, actor.stamina - r_cost)
                 elif r_type == "focus": actor.focus = max(0, actor.focus - r_cost)
                 
                 mech = result["mechanics"]
                 dmg = mech.get("damage_amount", 0)
                 dtype = mech.get("damage_type", "Meat")
                 
                 # Apply Damage to Target directly (Simulation side-effect)
                 if dmg > 0:
                    if dtype == "Meat": target.hp = max(0, target.hp - dmg)
                    else: target.composure = max(0, target.composure - dmg)
                    
                 return {
                    "action": "UseSkill",
                    "skill_id": skill_id,
                    "target": target.id,
                    "damage": dmg,
                    "narrative": result.get("narrative", "")
                 }
        
        # 3b. Fallback to Basic Attack if close
        if dist <= 1:
            print(f"[AI] Basic Attacking!")
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
            # Move towards target (or continue retreat logic if missed above)
            print(f"[AI] Moving to engage!")
            move_res = self._attempt_move(actor, start, end, retreat=False, tm=turn_manager)
            if move_res: return move_res
            
            return {"action": "Wait", "message": "Stuck."}

    def _attempt_move(self, actor, start: Point, end: Point, retreat: bool, tm: TurnManager) -> Optional[Dict]:
        """
        Tries to move one step towards or away from target. 
        Checks collision with other units.
        """
        dx = end.x - start.x
        dy = end.y - start.y
        
        # Potential steps
        options = []
        if retreat:
            # We want to INCREASE distance, so invert signs
            # (dx > 0) -> Target is right -> Run Left (-1)
            step_x = -1 if dx > 0 else 1
            step_y = -1 if dy > 0 else 1
            options = [(step_x, 0), (0, step_y), (-step_x, 0), (0, -step_y)] # Prioritize away
        else:
            # Approach
            step_x = 1 if dx > 0 else -1
            step_y = 1 if dy > 0 else -1
            
            # Prefer closing the wider gap
            if abs(dx) >= abs(dy):
                options = [(step_x, 0), (0, step_y)]
            else:
                options = [(0, step_y), (step_x, 0)]
                
        # Try options
        for sx, sy in options:
            if sx == 0 and sy == 0: continue
            
            nx, ny = actor.x + sx, actor.y + sy
            
            # check collision
            if self._is_occupied(nx, ny, tm):
                continue
                
            # validations handled by resolver mostly, but bounds check implicit in resolver
            result = self.resolver.resolve_move(actor.id, (actor.x, actor.y), (nx, ny), actor.ap)
            
            if result["success"]:
                actor.ap -= result["cost"]
                actor.x = nx
                actor.y = ny
                return {
                    "action": "Move",
                    "from": (start.x, start.y),
                    "to": (nx, ny)
                }
                
        return None

    def _is_occupied(self, x, y, tm: TurnManager) -> bool:
        for entity in tm.entities.values():
            if entity.hp > 0 and entity.x == x and entity.y == y:
                return True
        return False

    def _pick_best_skill(self, actor: EntityState, target: EntityState, dist: int):
        best_skill = None
        max_dmg = 0
        
        for skill_id in actor.known_skills:
            skill = DB.get(skill_id)
            if not skill: continue
            
            # Check Costs
            if actor.ap < skill.costs.ap: continue
            if skill.costs.type == "stamina" and actor.stamina < skill.costs.resource: continue
            if skill.costs.type == "focus" and actor.focus < skill.costs.resource: continue
            
            # Check Range
            if dist > skill.targeting.range: continue
            
            # Estimate damage (Basic heuristic: Base + Scaling)
            # This is duplicate of mechanics logic but needed for estimation
            # Let's just trust Base Damage for now or use a heuristic
            est_dmg = 5 
            # In a real system, we'd query the mechanics engine for "Projected Damage"
            
            if est_dmg > max_dmg:
                max_dmg = est_dmg
                best_skill = (skill_id, est_dmg)
                
        return best_skill

    def _find_nearest_target(self, actor: EntityState, tm: TurnManager) -> Optional[EntityState]:
        # Iterate all entities, find one on different team
        nearest = None
        min_dist = 999
        
        my_pos = Point(actor.x, actor.y)
        
        for eid, entity in tm.entities.items():
            if entity.team != actor.team and entity.hp > 0: # Check alive
                # print(f"[AI] Found hostile: {entity.name} at ({entity.x}, {entity.y})")
                other_pos = Point(entity.x, entity.y)
                d = my_pos.distance(other_pos)
                if d < min_dist:
                    min_dist = d
                    nearest = entity
                    
        return nearest
