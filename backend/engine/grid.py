from typing import List, Tuple, Dict
from dataclasses import dataclass
import math

@dataclass(frozen=True)
class Hex:
    q: int
    r: int
    s: int

    def __post_init__(self):
        assert self.q + self.r + self.s == 0, "Hex coordinates must sum to 0"

    def __add__(self, other: 'Hex') -> 'Hex':
        return Hex(self.q + other.q, self.r + other.r, self.s + other.s)

    def __sub__(self, other: 'Hex') -> 'Hex':
        return Hex(self.q - other.q, self.r - other.r, self.s - other.s)

    def scale(self, k: int) -> 'Hex':
        return Hex(self.q * k, self.r * k, self.s * k)

    def length(self) -> int:
        return (abs(self.q) + abs(self.r) + abs(self.s)) // 2

    def distance(self, other: 'Hex') -> int:
        return (self - other).length()
    
    def neighbors(self) -> List['Hex']:
        directions = [
            Hex(1, 0, -1), Hex(1, -1, 0), Hex(0, -1, 1),
            Hex(-1, 0, 1), Hex(-1, 1, 0), Hex(0, 1, -1)
        ]
        return [self + d for d in directions]

    def to_pixel(self, size: float) -> Tuple[float, float]:
        """Converts Hex to Pixel (Pointy Top)"""
        x = size * (math.sqrt(3) * self.q + math.sqrt(3)/2 * self.r)
        y = size * (3./2 * self.r)
        return (x, y)

    @staticmethod
    def from_pixel(x: float, y: float, size: float) -> 'Hex':
        """Converts Pixel to Hex (Pointy Top)"""
        q = (math.sqrt(3)/3 * x - 1./3 * y) / size
        r = (2./3 * y) / size
        return Hex.round(q, r, -q-r)

    @staticmethod
    def round(frac_q: float, frac_r: float, frac_s: float) -> 'Hex':
        q = round(frac_q)
        r = round(frac_r)
        s = round(frac_s)

        q_diff = abs(q - frac_q)
        r_diff = abs(r - frac_r)
        s_diff = abs(s - frac_s)

        if q_diff > r_diff and q_diff > s_diff:
            q = -r - s
        elif r_diff > s_diff:
            r = -q - s
        else:
            s = -q - r
        return Hex(int(q), int(r), int(s))

class GridManager:
    def __init__(self, radius: int = 10):
        self.radius = radius
        self.cells: Dict[Tuple[int, int], str] = {} # (q, r) -> biome/type
    
    def generate_empty_map(self):
        """Generates a hexagonal map of given radius centered at 0,0,0"""
        for q in range(-self.radius, self.radius + 1):
            r1 = max(-self.radius, -q - self.radius)
            r2 = min(self.radius, -q + self.radius)
            for r in range(r1, r2 + 1):
                self.cells[(q, r)] = "Void"

    def get_hex(self, q: int, r: int) -> Hex:
        return Hex(q, r, -q-r)

    def is_in_bounds(self, h: Hex) -> bool:
        return (h.q, h.r) in self.cells
