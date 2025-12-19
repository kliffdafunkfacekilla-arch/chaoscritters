import sys
import os

# Add current directory to path
sys.path.append(os.getcwd())

print("Attempting to import GridManager...")
try:
    from backend.engine.grid import GridManager
    print("GridManager Imported Successfully.")
except Exception as e:
    print(f"Failed to import GridManager: {e}")

print("Attempting to import ProcGen...")
try:
    from backend.engine.procgen import ProcGen
    print("ProcGen Imported Successfully.")
except Exception as e:
    print(f"Failed to import ProcGen: {e}")

print("Attempting to import Server...")
try:
    import backend.server
    print("Server Module Imported Successfully.")
except Exception as e:
    print(f"Failed to import Server: {e}")
