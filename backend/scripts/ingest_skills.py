import json
import os
import re

LEGACY_PATH = os.path.join("backend", "assets", "abilities.json")
OUTPUT_DIR = os.path.join("assets", "data", "skills")

def to_snake_case(name):
    s1 = re.sub('(.)([A-Z][a-z]+)', r'\1_\2', name)
    return re.sub('([a-z0-9])([A-Z])', r'\1_\2', s1).lower().replace(" ", "_").replace("(", "").replace(")", "").replace("-", "_")

def ingest():
    if not os.path.exists(LEGACY_PATH):
        print(f"Error: {LEGACY_PATH} not found.")
        return

    try:
        with open(LEGACY_PATH, 'r') as f:
            data = json.load(f)
    except Exception as e:
        print(f"Error processing JSON: {e}")
        return

    if not os.path.exists(OUTPUT_DIR):
        os.makedirs(OUTPUT_DIR)

    total_skills = 0
    
    # Iterate Schools (Force, Bastion...)
    for school_name, school_data in data.items():
        print(f"Processing School: {school_name}...")
        
        # Determine Resource Type mapping
        legacy_res = school_data.get("resource", "Stamina")
        res_type = "stamina"
        if legacy_res in ["Presence", "Stamina", "Endurance"]: res_type = "stamina"
        elif legacy_res in ["Focus", "Mana", "Logic"]: res_type = "focus"
        
        new_skills = []
        
        branches = school_data.get("branches", [])
        for branch in branches:
            tiers = branch.get("tiers", [])
            for tier_data in tiers:
                name = tier_data.get("name", "Unknown")
                tier_label = tier_data.get("tier", "T1")
                tier_val = 1
                if tier_label.startswith("T") and tier_label[1:].isdigit():
                    tier_val = int(tier_label[1:])
                
                # ID Generation
                clean_name = to_snake_case(name)
                # Avoid ID collisions if same name exists in diff branches (unlikely but safe)
                skill_id = clean_name 
                
                # Narrative generation
                desc = tier_data.get("description", "Does something.")
                
                # Transform to New Format
                new_skill = {
                    "id": skill_id,
                    "name": name,
                    "school": school_name,
                    "tier": tier_val,
                    "costs": {
                        "ap": 1,
                        "resource": tier_data.get("cost", {}).get("amount", 0),
                        "type": res_type
                    },
                    "targeting": {
                        "type": "Melee" if tier_data.get("target_type") == "enemy" else "Ranged", # Rough heuristic
                        "range": tier_data.get("range", 1) # Default to 1
                    },
                    "effects": tier_data.get("effects", []),
                    "narrative": desc # Use description as narrative/tooltip for now
                }
                
                # Refine Targeting based on Effects
                # If effect says "range": 5, use that.
                for eff in new_skill["effects"]:
                    if "range" in eff:
                         new_skill["targeting"]["range"] = eff["range"]
                         new_skill["targeting"]["type"] = "Ranged"
                
                new_skills.append(new_skill)
                total_skills += 1

        # Write School File
        out_path = os.path.join(OUTPUT_DIR, f"{school_name.lower()}.json")
        with open(out_path, 'w') as f:
            json.dump(new_skills, f, indent=2)
        print(f"Saved {len(new_skills)} skills to {out_path}")

    print(f"Ingestion Complete. Total Skills: {total_skills}")

if __name__ == "__main__":
    ingest()
