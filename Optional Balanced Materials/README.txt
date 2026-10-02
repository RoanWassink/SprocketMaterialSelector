Sprocket Optional Balanced Materials - v0.4.0

Gameplay-oriented choices, balanced against vanilla RHA (R=1, density=7850, cost=2).
These are fictionalized gameplay presets, not measured engineering data. Dates are gameplay availability settings.

INSTALL: close Sprocket, copy these JSON files into Sprocket_Data\StreamingAssets\Technology,
replacing the matching files from the previous pack. Start the game with Material Selector v0.4.0.
Do not overwrite vanilla RHA or SheetMetal; this pack does not contain them.
Editing existing material IDs changes vehicles already saved with those IDs.

Comparison at EQUAL protection (material costs only; welding/cutting excluded):
Material | RHA/mm | density | cost/kg multiplier | floor | thickness vs RHA | mass vs RHA | cost vs RHA
Vanilla RHA | 1.00 | 7850 | 2.00 | 2.00 | 1.00x | 1.00x | 1.00x
StructuralSteel | 0.70 | 7850 | 0.60 | 0.560 | 1.43x | 1.43x | 0.43x
CastArmour | 0.90 | 7850 | 1.30 | 1.181 | 1.11x | 1.11x | 0.72x
HighHardnessSteel | 1.16 | 7850 | 5.00 | 4.201 | 0.86x | 0.86x | 2.16x
AdvancedArmourSteel | 1.30 | 7850 | 8.00 | 7.426 | 0.77x | 0.77x | 3.08x
AluminiumArmour | 0.42 | 2700 | 6.00 | 5.430 | 2.38x | 0.82x | 2.46x
MagnesiumAlloy | 0.30 | 1800 | 8.00 | 7.667 | 3.33x | 0.76x | 3.06x
TitaniumArmour | 0.72 | 4430 | 9.00 | 6.761 | 1.39x | 0.78x | 3.53x
CeramicArray | 0.65 | 3600 | 14.00 | 11.440 | 1.54x | 0.71x | 4.94x
HeavyAlloyInsert | 1.50 | 17500 | 7.00 | 6.813 | 0.67x | 1.49x | 5.20x
ElastomerNERA | 0.07 | 1200 | 1.80 | 0.366 | 14.29x | 2.18x | 1.97x
AramidSpallLiner | 0.04 | 1440 | 3.00 | 0.174 | 25.00x | 4.59x | 6.88x

Roles:
StructuralSteel: Budget plate: cheap, thick and heavy for equal protection.
CastArmour: Economy armour: lower price with a modest thickness and mass penalty.
HighHardnessSteel: Compact steel: thinner and lighter for equal protection, higher price and spall.
AdvancedArmourSteel: Premium steel: greater thickness efficiency at a substantial price.
AluminiumArmour: Light alloy: saves weight, needs much more thickness.
MagnesiumAlloy: Very light, very bulky: a small weight advantage over aluminium, with worse thickness efficiency.
TitaniumArmour: Premium light alloy: less bulky than aluminium at a higher price.
CeramicArray: High weight efficiency, high cost and spall. Intended for layered experiments.
HeavyAlloyInsert: Dense insert: best thickness efficiency here, but very heavy for equal protection.
ElastomerNERA: Low-protection filler. The name does not imply simulated reactive-armour effects.
AramidSpallLiner: Low-protection liner with zero configured spall; not a replacement for armour.

The cost floor uses the stronger of thickness efficiency^4 and weight efficiency^4.
All requested prices in this pack exceed their calculated floors. Third-party cheaper
requests are still raised automatically by the plugin; its balancing does not modify JSON.

Layered experiments: combine structural armour with filler/liner layers as desired.
Sprocket does not simulate all real composite, KE/CE, fracture, fire or multi-hit behaviour.
A zero spall value only configures that material: it does not guarantee that a liner
will catch fragments from another layer. This pack makes no reactive-armour guarantees.
