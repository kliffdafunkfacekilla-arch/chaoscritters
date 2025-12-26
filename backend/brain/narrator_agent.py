from backend.brain.llm_client import LLMClient

class NarratorAgent:
    def __init__(self, llm: LLMClient):
        self.llm = llm

    def narrate_event(self, event_log: list) -> str:
        """
        Describes a sequence of game events effectively.
        """
        if not event_log:
            return "Nothing happened."
            
        system = "You are the Dungeon Master Narrator. Describe the combat action vividly but briefly based on the log provided. Keep it under 2 sentences."
        prompt = f"Events: {event_log}"
        
        # Fallback for speed/reliability:
        try:
            # Check if LLM is actually connected? Or just try/except
            # return self.llm.generate(prompt, system)
            # For debugging, let's just return the raw log prettified
            return f"Log: {'; '.join([str(e) for e in event_log])}"
        except:
             return "The chaos shifts..."
