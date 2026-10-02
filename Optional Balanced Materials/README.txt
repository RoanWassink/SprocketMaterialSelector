Sprocket Balanced Materials Pack

Basis: vanilla RolledHomogeneousArmour.json uses rhaFactor 1.0, density 7850 kg/m3,
spallFactor 0.0005, costMultiplier 2.0.

Design intent:
- Cast Armour: slightly worse than RHA, cheap WW2-style option.
- High Hardness Steel: modest 12% protection gain at same mass, higher cost/spall.
- Advanced Armour Steel: modest modern improvement, expensive.
- Aluminium Armour: much lighter but requires substantially more thickness.
- Titanium Armour: lighter and reasonably efficient, but expensive.
- Ceramic Array: intended only as a layer in composite arrays. Good mass efficiency,
  but high spall factor simulates brittleness. Do not use as a complete monolithic hull.
- Elastomer NERA: lightweight filler layer with almost no direct protection and no spall.
- Aramid Spall Liner: almost no direct armour value; intended as a rear liner.
- Heavy Alloy Insert: compact protection but extremely heavy and expensive.
- Structural Steel: cheap non-armour steel, significantly weaker than RHA.

Suggested composite stack:
10 mm High Hardness Steel
25-50 mm Ceramic Array
10-20 mm Elastomer NERA
25-50 mm Ceramic Array
20-40 mm RHA / Advanced Armour Steel
5-10 mm Aramid Spall Liner

These are gameplay-oriented approximations, not engineering-grade material models.
Sprocket does not reproduce the full KE/CE, fracture and multi-hit behaviour of real armour materials.
