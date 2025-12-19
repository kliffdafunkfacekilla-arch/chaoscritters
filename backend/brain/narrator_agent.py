from backend.brain.llm_client import LLMClient

class NarratorAgent:
    def __init__(self, llm: LLMClient):
        self.llm = llm

    def narrate_event(self, event_log: list) -> str:
        """
        Describes a sequence of game events effectively.
        """
        system = "You are the Dungeon Master Narrator. Describe the combat action vividly but briefly based on the log provided. Keep it under 2 sentences."
        prompt = f"Events: {event_log}"
        return self.llm.generate(prompt, system)
