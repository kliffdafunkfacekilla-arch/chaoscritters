import json
import os
import random
from typing import Dict, Any, List, Optional
from pydantic import BaseModel

class Effect(BaseModel):
    type: str # Damage, Heal, Status
    dice: Optional[str] = None
    bonus: int = 0
    dmg_type: Optional[str] = None
    status: Optional[str] = None
    duration: int = 0

class AbilityCosts(BaseModel):
    ap: int = 1
    resource: int = 0
    type: str = "stamina"

class AbilityTargeting(BaseModel):
    type: str = "Melee"
    range: int = 1

class Ability(BaseModel):
    id: str
    name: str
    school: str
    tier: int = 1
    costs: AbilityCosts
    targeting: AbilityTargeting
    effects: List[Effect]
    narrative: str

class AbilityDatabase:
    def __init__(self):
        self.skills: Dict[str, Ability] = {}
        self.load_skills()

    def load_skills(self):
        base_path = os.path.join("assets", "data", "skills")
        if not os.path.exists(base_path):
            print(f"[AbilityDB] Warning: {base_path} not found.")
            return

        for filename in os.listdir(base_path):
            if filename.endswith(".json"):
                path = os.path.join(base_path, filename)
                try:
                    with open(path, 'r') as f:
                        data = json.load(f)
                        for item in data:
                            ability = Ability(**item)
                            self.skills[ability.id] = ability
                    print(f"[AbilityDB] Loaded {filename}")
                except Exception as e:
                    print(f"[AbilityDB] Error loading {filename}: {e}")

    def get(self, ability_id: str) -> Optional[Ability]:
        return self.skills.get(ability_id)

# Global DB Instance
DB = AbilityDatabase()

class AbilityResolver:
    def __init__(self, engine):
        self.engine = engine # MechanicsEngine

    def resolve_ability(self, ability_id: str, attacker: Any, target: Any) -> Dict[str, Any]:
        ability = DB.get(ability_id)
        if not ability:
            return {"success": False, "message": f"Unknown Ability: {ability_id}"}
            
        # 1. Check Costs
        if attacker.ap < ability.costs.ap:
            return {"success": False, "message": "Not enough AP"}
            
        if ability.costs.type == "stamina" and attacker.stamina < ability.costs.resource:
             return {"success": False, "message": "Not enough Stamina"}
        elif ability.costs.type == "focus" and attacker.focus < ability.costs.resource:
             return {"success": False, "message": "Not enough Focus"}
             
        # 2. Check Range
        dist = abs(attacker.x - target.x) + abs(attacker.y - target.y)
        if dist > ability.targeting.range:
            return {"success": False, "message": f"Out of Range ({dist} > {ability.targeting.range})"}
            
        # 3. Resolve Effects
        total_damage = 0
        damage_type = "Meat"
        
        # Determine Stats for Rolls
        atk_stat = 12
        if ability.costs.type == "stamina": atk_stat = attacker.stats.get('Might', 12)
        elif ability.costs.type == "focus": atk_stat = attacker.stats.get('Knowledge', 12)
        
        def_stat = target.stats.get('Reflexes', 10)
        
        # Base Mechanics Roll (Hit/Crit)
        mech_result = self.engine.resolve_attack(atk_stat, 0, def_stat, 0)
        
        # Process Effects
        for effect in ability.effects:
            if effect.type == "Damage":
                # Roll Dice?
                # For now, simple logic: Base Mechanics Damage + Bonus
                # If Mechanics says 0 damage (Miss), usually spell misses too?
                # Or Spells are Save-based?
                # Sticking to Attack Roll for now.
                
                base = mech_result.get("damage_amount", 0)
                if base > 0:
                    # Apply Dice Roll Logic if needed, or just flat bonus from JSON?
                    # JSON says "dice": "1d10".
                    # Let's say we roll that.
                    roll = self.roll_dice(effect.dice)
                    total_damage += roll + effect.bonus
                    damage_type = effect.dmg_type or "Meat"
        
        if total_damage > 0:
            mech_result["damage_amount"] = total_damage
            mech_result["damage_type"] = damage_type
            
        return {
            "success": True,
            "cost": ability.costs.ap,
            "resource_cost": ability.costs.resource,
            "resource_type": ability.costs.type,
            "mechanics": mech_result,
            "narrative": f"{attacker.name} {ability.narrative} at {target.name}!"
        }

    def roll_dice(self, dice_str: str) -> int:
        if not dice_str: return 0
        try:
            # "1d10"
            count, sides = map(int, dice_str.lower().split('d'))
            val = 0
            for _ in range(count):
                val += random.randint(1, sides)
            return val
        except:
             return 0
