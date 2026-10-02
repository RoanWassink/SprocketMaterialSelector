# v0.3.1 — Sprocket Material Selector

Adds an armour material dropdown to the plate structure editor, using custom armour technology JSON files from `Sprocket_Data\StreamingAssets\Technology`.

- Updates armour mass, cached component mass, and total vehicle mass through Sprocket's native rebuild route.
- Retains the selected material when saving and loading the vehicle.
- Includes a button to reload the available materials while playing.

Mass updates and saving/loading have been tested. This plugin is vibe coded with AI assistance.

Requires Sprocket 0.2.55.5 and a working Sprocket Mod Loader / BepInEx 6 IL2CPP setup. Quality of Life is optional.

Download `SprocketMaterialSelector.dll` and put it in `Sprocket\BepInEx\plugins\` while the game is closed. The optional materials pack ZIP contains JSON files to copy into `Sprocket_Data\StreamingAssets\Technology`.
