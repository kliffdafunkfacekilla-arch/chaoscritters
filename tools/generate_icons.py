import os
from PIL import Image, ImageDraw, ImageFont

def create_icon(name, color, text):
    os.makedirs("Assets/Resources/Icons", exist_ok=True)
    img = Image.new('RGB', (128, 128), color=color)
    d = ImageDraw.Draw(img)
    
    # Draw simple shape or text
    # Just text for now
    d.text((10, 50), text, fill=(255, 255, 255))
    
    path = f"Assets/Resources/Icons/{name}.png"
    img.save(path)
    print(f"Created {path}")

# Skill Mappings
icons = [
    ("default_icon", (100, 100, 100), "?"),
    ("fireball", (200, 50, 0), "FIRE"),
    ("hammer_drop", (50, 50, 200), "SLAM"),
    ("shove", (100, 200, 50), "PUSH"),
    ("boot_stomp", (150, 100, 50), "LEAP"),
]

if __name__ == "__main__":
    for name, col, txt in icons:
        create_icon(name, col, txt)
