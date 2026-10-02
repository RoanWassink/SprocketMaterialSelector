# v0.4.0 — Runtime cost balancing and expanded materials pack

- Automatic minimum material price based on protection per millimetre and per kilogram, relative to vanilla RHA.
- Third-party requested prices can exceed the floor, but cannot undercut it. Source Technology JSON files remain unchanged.
- Balancing covers material selection, technology synchronization and loaded vehicle builds.
- Invalid or numerically unsupported materials are excluded from the dropdown; loaded structures using them fall back to RHA with a warning.
- Inspector shows weight efficiency, effective price and any balance correction.
- Optional pack: 11 distinct gameplay presets, with an equal-protection comparison in its README. Includes a new magnesium-alloy option.

Mass, costs and saving/loading have been checked in-game by the author. This plugin is vibe coded with AI assistance.

Requires Sprocket 0.2.55.5 and a working Sprocket Mod Loader / BepInEx 6 IL2CPP setup. Quality of Life is optional.

Close Sprocket, replace SprocketMaterialSelector.dll in Sprocket\BepInEx\plugins\, then copy the optional pack's JSON files into Sprocket_Data\StreamingAssets\Technology, replacing the previous pack files. Vanilla RHA and SheetMetal are not included or overwritten.

Updated material presets also affect existing vehicles saved with those material IDs. Back up saves before upgrading. The preset values and dates are gameplay choices; filler/liner names do not imply full real-world reactive-armour or spall-catching simulation.
