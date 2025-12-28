from typing import List, Dict, Optional
import random
from pydantic import BaseModel

class EntityState(BaseModel):
    id: str
    name: str
    initiative: int = 0
    ap: int = 5 # Standard AP
    hp: int
    max_hp: int
    composure: int
    max_composure: int
    team: str = "Player" # Player vs Enemy
    x: int = 0
    y: int = 0
    image_id: str = "default.png"
    visual_tags: Dict[str, str] = {} 

class TurnManager:
    def __init__(self):
        self.entities: Dict[str, EntityState] = {}
        self.turn_order: List[str] = []
        self.current_index: int = 0
        self.round: int = 1
        
    def add_entity(self, entity: EntityState):
        self.entities[entity.id] = entity
        
    def roll_initiative(self):
        """Rolls initiative for all entities and sorts the turn order."""
        for eid, entity in self.entities.items():
            if entity.initiative < 90:
                roll = random.randint(1, 20)
                entity.initiative = roll
            
        self.turn_order = sorted(
            self.entities.keys(), 
            key=lambda x: self.entities[x].initiative, 
            reverse=True
        )
        self.current_index = 0
        self.round = 1
        print(f"Initiative Rolled: {self.turn_order}")

    def get_current_actor(self) -> Optional[EntityState]:
        if not self.turn_order:
            return None
        return self.entities[self.turn_order[self.current_index]]
        
    def next_turn(self):
        """Advances to the next living actor."""
        # Loop until we find a living actor or exhaust list
        attempts = 0
        while attempts < len(self.turn_order):
            self.current_index += 1
            if self.current_index >= len(self.turn_order):
                self.current_index = 0
                self.round += 1
                print(f"--- Round {self.round} Start ---")
            
            actor = self.get_current_actor()
            if actor and actor.hp > 0:
                self._start_turn_logic(actor)
                return actor
            
            attempts += 1
            
        print("All entities dead?")
        return None
        
    def _start_turn_logic(self, actor: EntityState):
        # Reset AP
        actor.ap = 5 
        print(f"Start Turn: {actor.name} (AP: {actor.ap})")

    def check_victory_condition(self) -> str:
        """Returns 'Ongoing', 'Victory' (Player Win), or 'Defeat' (Player Loss)"""
        players_alive = any(e.hp > 0 and e.team == "Player" for e in self.entities.values())
        enemies_alive = any(e.hp > 0 and e.team == "Enemy" for e in self.entities.values())
        
        if not players_alive:
            return "Defeat"
        if not enemies_alive:
            return "Victory"
            
        return "Ongoing"
