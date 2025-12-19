import random
import math
from typing import Dict, Tuple, List
from backend.engine.grid import GridManager

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
        
        for (gx, gy) in grid.cells.keys():
            # Normalize coords for noise
            scale = 0.15
            
            # Simple offset to noise space
            x = gx * scale
            y = gy * scale
            
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
                
            grid.cells[(gx, gy)] = tile_type
            map_data[(gx, gy)] = {
                "type": tile_type,
                "cost": movement_cost,
                "height": height,
                "elevation": elev,
                "moisture": moist
            }
            
        return map_data

    # Visualization removed to simplify dependencies

