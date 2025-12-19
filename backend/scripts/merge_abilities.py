import json
import os

main_file = 'backend/assets/abilities.json'
missing_file = 'backend/assets/missing_abilities.json'

try:
    with open(main_file, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    with open(missing_file, 'r', encoding='utf-8') as f:
        new_data = json.load(f)
    
    if "PLACEHOLDER" in data:
        print("Removing PLACEHOLDER...")
        del data["PLACEHOLDER"]
        
    print(f"Merging {len(new_data)} new disciplines...")
    data.update(new_data)
    
    with open(main_file, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=4)
        
    print(f"Success! Final Keys: {list(data.keys())}")
except Exception as e:
    print(f"Error: {e}")
