from typing import List, Tuple, Dict
from dataclasses import dataclass
import math

@dataclass(frozen=True)
class Point:
    x: int
    y: int

    def __add__(self, other: 'Point') -> 'Point':
        return Point(self.x + other.x, self.y + other.y)

    def __sub__(self, other: 'Point') -> 'Point':
        return Point(self.x - other.x, self.y - other.y)

    def distance(self, other: 'Point') -> int:
        # Manhattan Distance for 4-way movement
        return abs(self.x - other.x) + abs(self.y - other.y)
    
    def neighbors(self) -> List['Point']:
        # 4-Way neighbors (No diagonals for simple grid)
        directions = [
            Point(0, 1), Point(1, 0), Point(0, -1), Point(-1, 0)
        ]
        return [self + d for d in directions]

class GridManager:
    def __init__(self, radius: int = 10):
        self.radius = radius
        self.cells: Dict[Tuple[int, int], str] = {} # (x, y) -> biome/type
    
    def generate_empty_map(self):
        """Generates a square map of given radius centered at 0,0"""
        # Generates from -radius to +radius
        for x in range(-self.radius, self.radius + 1):
            for y in range(-self.radius, self.radius + 1):
                self.cells[(x, y)] = "Void"

    def is_in_bounds(self, p: Point) -> bool:
        return (p.x, p.y) in self.cells
