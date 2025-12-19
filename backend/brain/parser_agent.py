from typing import Dict, Any, List
from backend.brain.llm_client import LLMClient
from backend.engine.turn_manager import EntityState

class ParserAgent:
    def __init__(self, llm: LLMClient):
        self.llm = llm

    def parse_command(self, text: str, actor: EntityState, visible_entities: List[EntityState]) -> Dict[str, Any]:
        """
        Parses text input into a structured Intent.
        """
        system = """
        You are the Game Parser. Your job is to convert player speech into game actions.
        Available Actions:
        - Move(target_pos=[q, r])
        - Attack(target_id="ID")
        - UseAbility(name="AbilityName", target_id="ID")
        - Speak(text="...")
        
        Output format: JSON with keys "action", "params".
        If "action" is "Unknown", provide "reason".
        """
        
        context = f"""
        Actor: {actor.name} (ID: {actor.id})
        Visible Targets: {[{'id': e.id, 'name': e.name} for e in visible_entities if e.id != actor.id]}
        Player Input: "{text}"
        """
        
        return self.llm.generate_json(context, {}, system)
