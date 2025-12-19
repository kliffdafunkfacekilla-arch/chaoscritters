import os
try:
    from faster_whisper import WhisperModel
except ImportError:
    print("Warning: faster_whisper not installed.")
    WhisperModel = None

class VoiceInterface:
    def __init__(self, model_size="tiny"):
        self.model = None
        self.model_size = model_size
        self.enabled = False
        
        if WhisperModel:
            try:
                # Run on CPU by default to be safe, or 'cuda' if available
                # 'int8' quantization for speed on CPU
                self.model = WhisperModel(model_size, device="cpu", compute_type="int8")
                self.enabled = True
                print(f"Faster Whisper ({model_size}) initialized on CPU.")
            except Exception as e:
                print(f"Failed to load Whisper: {e}")
        else:
            print("Voice Interface disabled (missing dependencies).")

    def transcribe(self, audio_path: str) -> str:
        """Transcribes audio file to text."""
        if not self.enabled or not os.path.exists(audio_path):
            return ""
            
        try:
            segments, info = self.model.transcribe(audio_path, beam_size=5)
            text = " ".join([segment.text for segment in segments])
            return text.strip()
        except Exception as e:
            print(f"Transcription error: {e}")
            return ""


    # TTS Implementation using pyttsx3
    def speak(self, text: str, output_path: str = "output.wav"):
        """Synthesizes speech from text."""
        try:
            import pyttsx3
            engine = pyttsx3.init()
            # Saving to file is safer for async contexts than runAndWait blocking the loop
            engine.save_to_file(text, output_path)
            engine.runAndWait() 
            print(f"[TTS] Saved audio to {output_path}")
            return output_path
        except Exception as e:
            print(f"TTS Error: {e}")
            return None
