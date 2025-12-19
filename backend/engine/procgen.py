import random
import math
from typing import Dict, Tuple, List
from backend.engine.grid import GridManager, Hex

try:
    from perlin_noise import PerlinNoise
except ImportError:
    # Fallback if perlin-noise is not installed, though we requested it
    class PerlinNoise:
        def __init__(self, octaves=1, seed=1):
            self.seed = seed
        def __call__(self, coords):
            # Very dumb pseudo-random fallback
            return random.random()

class ProcGen:
    def __init__(self, seed: int = None):
        self.seed = seed if seed is not None else random.randint(0, 10000)
        self.noise_elevation = PerlinNoise(octaves=3, seed=self.seed)
        self.noise_moisture = PerlinNoise(octaves=2, seed=self.seed + 1)
        self.noise_difficulty = PerlinNoise(octaves=4, seed=self.seed + 2)

    def generate_terrain(self, grid: GridManager, biome_type: str = "Standard") -> Dict[Tuple[int, int], dict]:
        """
        Populates the grid with terrain based on noise maps.
        Returns a dict of metadata for each cell.
        """
        map_data = {}
        
        # Biome Settings (Elevation Thresholds)
        # Deep Water < Water < Sand < Grass < Forest < Mountain < Peak
        
        for (q, r) in grid.cells.keys():
            # Normalize coords for noise (scale needs to be small)
            # We use a scale factor relative to map radius to keep features reasonable
            scale = 0.15
            
            x = q * scale
            y = r * scale
            
            elev = self.noise_elevation([x, y]) + 0.5 # Normalize roughly to 0-1
            moist = self.noise_moisture([x, y]) + 0.5
            
            tile_type = "Grass"
            movement_cost = 1
            height = 0
            
            # Basic Terrain Logic
            if elev < 0.35:
                tile_type = "Water"
                movement_cost = 99
                height = -1
            elif elev < 0.40:
                tile_type = "Sand"
                movement_cost = 2
                height = 0
            elif elev < 0.65:
                if moist > 0.6:
                    tile_type = "Forest"
                    movement_cost = 2
                else:
                    tile_type = "Grass"
                    movement_cost = 1
                height = 1
            elif elev < 0.8:
                tile_type = "Stone"
                movement_cost = 1
                height = 2
            else:
                tile_type = "Mountain"
                movement_cost = 3
                height = 3
            
            # Biome Overrides
            if biome_type == "Ruins":
                if tile_type in ["Grass", "Forest"]:
                    if random.random() > 0.7:
                        tile_type = "Rubble"
                        movement_cost = 2
                
            grid.cells[(q, r)] = tile_type
            map_data[(q, r)] = {
                "type": tile_type,
                "cost": movement_cost,
                "height": height,
                "elevation": elev,
                "moisture": moist
            }
            
        return map_data

    def visualize_map(self, map_data: Dict[Tuple[int, int], dict], output_path: str = "map_preview.png"):
        """Generates a PNG preview of the map using Matplotlib"""
        try:
            import matplotlib.pyplot as plt
            import matplotlib.patches as patches
            
            fig, ax = plt.subplots(figsize=(10, 10))
            ax.set_aspect('equal')
            
            # Color map
            colors = {
                "Water": "#1f77b4",
                "Sand": "#e3d5ca",
                "Grass": "#2ca02c",
                "Forest": "#006400",
                "Stone": "#7f7f7f",
                "Mountain": "#505050",
                "Rubble": "#8c564b",
                "Void": "#000000"
            }
            
            # Convert hex to pixel for plotting
            hex_size = 1.0
            
            for (q, r), data in map_data.items():
                h = Hex(q, r, -q-r)
                x, y = h.to_pixel(hex_size)
                color = colors.get(data['type'], "pink")
                
                # Draw simple hexagon approximation (circle for speed) or actual hex
                circle = patches.Circle((x, y), radius=hex_size * 0.9, color=color)
                ax.add_patch(circle)
                # ax.text(x, y, f"{q},{r}", fontsize=6, ha='center', va='center')

            # Auto-scale
            ax.autoscale_view()
            plt.axis('off')
            plt.savefig(output_path, bbox_inches='tight')
            plt.close()
            print(f"Map visualization saved to {output_path}")
            
        except ImportError:
            print("Matplotlib not installed, skipping visualization.")
