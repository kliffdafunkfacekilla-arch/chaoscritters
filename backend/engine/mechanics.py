import json
import random
from typing import Dict, List, Optional, Tuple, Any
import os

# Constants
STATS_FILE = os.path.join(os.path.dirname(os.path.dirname(__file__)), "assets", "stats_and_skills.json")
TALENTS_FILE = os.path.join(os.path.dirname(os.path.dirname(__file__)), "assets", "talents.json")
ABILITIES_FILE = os.path.join(os.path.dirname(os.path.dirname(__file__)), "assets", "abilities.json")

class MechanicsEngine:
    def __init__(self):
        self.stats = self._load_json(STATS_FILE)
        self.talents = self._load_json(TALENTS_FILE)
        self.abilities = self._load_json(ABILITIES_FILE)
        # Unified data property for backward compatibility or easy dumping
        self.data = {
            "stats": self.stats,
            "talents": self.talents,
            "abilities": self.abilities
        }

    def _load_json(self, file_path: str) -> Dict[str, Any]:
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                return json.load(f)
        except getattr(FileNotFoundError, 'mro', lambda: [FileNotFoundError])()[0]: # robust catch
            print(f"Error: File not found at {file_path}")
            return {}
        except Exception as e:
            print(f"Error reading {file_path}: {e}")
            return {}



    def roll_dice(self, sides: int = 20, count: int = 1) -> int:
        return sum(random.randint(1, sides) for _ in range(count))

    def calculate_modifier(self, value: int) -> int:
        # Standard d20 system modifier: (Score - 10) / 2
        # However, check if the system uses a different modifier rule.
        # The Rules Bible doesn't explicitly state a standard D&D style modifier formula 
        # but implies stats are used directly or heavily influential.
        # "Attack Roll: d20 + Weapon Stat + Skill Mod"
        # If Stat is 12 (Apex), adding 12 + d20 might be the intended mechanic given the high numbers.
        # Let's assume for now it's Stat Value directly added unless clarified.
        return value

    def resolve_attack(self, attack_stat: int, skill_bonus: int, 
                       defense_stat: int, armor_bonus: int, defense_skill_bonus: int = 0) -> Dict[str, Any]:
        """
        Resolves an attack vs defense contest.
        """
        attack_roll = self.roll_dice(20)
        defense_roll = self.roll_dice(20)

        total_attack = attack_roll + attack_stat + skill_bonus
        total_defense = defense_roll + defense_stat + armor_bonus + defense_skill_bonus

        margin = total_attack - total_defense
        
        result = "MISS"
        effect = "No effect."
        damage_type = "None"
        damage_amount = 0

        if margin == 0:
            result = "CLASH"
            effect = "Trigger The Wheel of Pain."
        elif margin < 0:
            result = "MISS"
            effect = "Dodge, Block, or Glancing Blow."
        elif 1 <= margin <= 4:
            result = "GRAZE"
            effect = "Deal Shock Damage to COMPOSURE."
            damage_type = "Shock"
            damage_amount = self.roll_dice(4) # Base graze damage from bible is implied often 1d4 or variable
        elif 5 <= margin <= 9:
            result = "HIT"
            effect = "Deal Meat Damage to HP."
            damage_type = "Meat"
            damage_amount = self.roll_dice(8) # Placeholder weapon damage
        elif margin >= 10:
            result = "CRIT"
            effect = "Double HP Damage + Roll on Injury Table."
            damage_type = "Meat"
            damage_amount = self.roll_dice(8) * 2

        return {
            "attack_roll_base": attack_roll,
            "defense_roll_base": defense_roll,
            "total_attack": total_attack,
            "total_defense": total_defense,
            "margin": margin,
            "result": result,
            "effect": effect,
            "damage_type": damage_type,
            "damage_amount": damage_amount
        }

    def wheel_of_pain(self, attacker_card: str, defender_card: str) -> Dict[str, str]:
        """
        Resolves the Wheel of Pain clash mechanic.
        Choices: PRESS, MANEUVER, DISENGAGE, TACTIC
        """
        attacker_card = attacker_card.upper()
        defender_card = defender_card.upper()
        
        wins = {
            "PRESS": "MANEUVER",
            "MANEUVER": "DISENGAGE",
            "DISENGAGE": "TACTIC",
            "TACTIC": "PRESS"
        }

        if attacker_card == defender_card:
            return {"winner": "TIE", "message": "The clash continues!"}
        
        if wins.get(attacker_card) == defender_card:
            return {"winner": "ATTACKER", "message": f"{attacker_card} beats {defender_card}. Attacker +5 to re-roll."}
        
        return {"winner": "DEFENDER", "message": f"{defender_card} beats {attacker_card}. Defender +5 to re-roll."}

    def calculate_pools(self, stats: Dict[str, int]) -> Dict[str, int]:
        """
        Calculates derived pools from base stats.
        """
        might = stats.get("Might", 10)
        endurance = stats.get("Endurance", 10)
        logic = stats.get("Logic", 10)
        knowledge = stats.get("Knowledge", 10)
        vitality = stats.get("Vitality", 10)
        fortitude = stats.get("Fortitude", 10)
        willpower = stats.get("Willpower", 10)
        charm = stats.get("Charm", 10)

        return {
            "Stamina": (might + endurance) // 2,
            "Focus": (logic + knowledge) // 2,
            "HP": 10 + vitality + fortitude,
            "Composure": 5 + willpower + charm
        }

if __name__ == "__main__":
    # Simple test
    engine = MechanicsEngine()
    print("Testing Attack Resolution:")
    print(engine.resolve_attack(12, 2, 10, 2))
    print("\nTesting Wheel of Pain:")
    print(engine.wheel_of_pain("PRESS", "MANEUVER"))
