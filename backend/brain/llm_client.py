import ollama
from typing import List, Dict, Any, Optional
import json

class LLMClient:
    def __init__(self, model="llama3"):
        self.model = model
        self.host = "http://localhost:11434"
        self.client = ollama.Client(host=self.host)
        
    def check_connection(self) -> bool:
        try:
            self.client.list()
            print(f"Connected to Ollama at {self.host}")
            return True
        except Exception as e:
            print(f"Ollama Connection Failed: {e}")
            return False

    def generate(self, prompt: str, system: str = "") -> str:
        try:
            response = self.client.chat(model=self.model, messages=[
                {'role': 'system', 'content': system},
                {'role': 'user', 'content': prompt},
            ])
            return response['message']['content']
        except Exception as e:
            print(f"LLM Generate Error: {e}")
            return ""

    def generate_json(self, prompt: str, schema: Dict[str, Any], system: str = "") -> Dict[str, Any]:
        """Forces JSON output conforming to schema (if possible) or just parsing JSON."""
        system_prompt = f"{system}\nYou MUST output valid JSON only. No markdown formatting."
        
        response = self.generate(prompt, system_prompt)
        
        # Cleanup
        clean_response = response.replace("```json", "").replace("```", "").strip()
        
        try:
            return json.loads(clean_response)
        except json.JSONDecodeError:
            print(f"JSON Parse Error. Raw: {clean_response}")
            return {}
