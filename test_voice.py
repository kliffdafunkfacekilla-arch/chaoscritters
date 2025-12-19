import requests
import subprocess
import sys
import time

# Start server
proc = subprocess.Popen([sys.executable, "-m", "uvicorn", "backend.server:app", "--port", "8006", "--no-access-log"], cwd=".")
print("Starting server on port 8006...")
time.sleep(10) # Heavy model load takes time

try:
    # We need a dummy audio file.
    # Creating a fake WAV header + 1 second of silence just to test file upload mechanism
    # Whisper won't transcribe silence, but it shouldn't crash.
    with open("silence.wav", "wb") as f:
        # Minimal WAV header
        f.write(b'RIFF$\x00\x00\x00WAVEfmt \x10\x00\x00\x00\x01\x00\x01\x00D\xac\x00\x00D\xac\x00\x00\x01\x00\x08\x00data\x00\x00\x00\x00')
    
    print("Testing STT Endpoint...")
    with open("silence.wav", "rb") as f:
        files = {'file': f}
        r = requests.post("http://127.0.0.1:8006/interface/stt", files=files)
        
    print(f"STT Status: {r.status_code}")
    print(r.json())
    
finally:
    proc.terminate()
    print("Server stopped.")
