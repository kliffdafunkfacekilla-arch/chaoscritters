from PIL import Image
import os

def slice_tokens():
    source_path = r"c:\Users\krazy\chaoscritters\Assets\Sprites\tokens.png"
    output_dir = r"c:\Users\krazy\chaoscritters\Assets\Sprites\Parsed"
    
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)
        
    try:
        img = Image.open(source_path)
    except Exception as e:
        print(f"Error opening image: {e}")
        return

    width, height = img.size
    cols = 7
    rows = 4
    
    cell_w = width // cols
    cell_h = height // rows
    
    print(f"Image Size: {width}x{height}. Cell Size: {cell_w}x{cell_h}")
    
    # Name Mapping (Cleaned)
    names = [
        "Bear", "Mermaid", "Eagle", "Lizard", "Tree", "Tree2", "Owl",
        "Owl2", "Wolf", "Owl3", "Fox", "Turtle", "Ant", "Mushroom",
        "Mushroom2", "Falcon", "Shark", "Flower", "Badger", "Spider", "Croc",
        "Mantis", "Tree3", "Crab", "Raven", "Frog", "Butterfly", "Deer"
    ]
    
    idx = 0
    for r in range(rows):
        for c in range(cols):
            if idx >= len(names):
                break
                
            name = names[idx]
            left = c * cell_w
            top = r * cell_h
            right = left + cell_w
            bottom = top + cell_h
            
            crop = img.crop((left, top, right, bottom))
            out_path = os.path.join(output_dir, f"{name}.png")
            crop.save(out_path)
            print(f"Saved {out_path}")
            idx += 1

if __name__ == "__main__":
    slice_tokens()
