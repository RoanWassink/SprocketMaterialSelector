# Sprocket Material Selector

Pick custom armour materials from a dropdown in the plate editor. Materials affect protection, weight, spall and the game's vehicle cost. **v0.4.8 — beta.**

Download [SprocketMaterialSelector-v0.4.8.zip](https://github.com/RoanWassink/SprocketMaterialSelector/releases/tag/v0.4.8). You only need the download; no compiling required. GitHub's **Code > Download ZIP** gives you the source instead.

## Requirements

- Sprocket **0.2.55.5**, Windows x64 / Unity **6000.3.21f1**.
- A working [Hans21223 Sprocket Mod Loader](https://github.com/Hans21223/Sprocket-Mod-Loader) / **BepInEx 6 IL2CPP (6.0.0-be.788)** environment. Follow the loader's own installation instructions and start the game once before adding this mod.
- Quality of Life is optional. The loader, game files and other plugins are not included.
- For extra HEAT/APFSDS armour reactions, use **Shell Selector 0.12.5** and a matching, enabled response catalogue. Material Selector alone provides selection and passive armour properties. Keybinds is not required.

## Install

1. Close Sprocket. In Steam, open **Manage > Browse local files** for the game.
2. Put `SprocketMaterialSelector.dll` in `BepInEx/plugins`. Keep only one copy of the plugin.
3. Copy the five included Technology JSON files to `Sprocket_Data/StreamingAssets/Technology`. If a matching file already exists, back it up and keep your edits.
4. If you do not already have `BepInEx/config/sprocket.armour.responses.json`, copy the included starter there. It starts with extra reactions **disabled**. For those reactions, install matching Shell Selector and set `enabled` to `true`; restart after editing.
5. Start the game, select a plate structure and choose its **Armour material**. Hover **Material** or **Protection** for strengths, thickness requirements and limitations.

## Updating and settings

Back up the affected mod files, custom materials, settings and vehicle saves first. Replace the DLL; merge missing assets individually. **Do not extract over customized Technology or response files.** Your existing response catalogue is not automatically merged, replaced or enabled. The full five-material default catalogue is also available in `examples` in the source download for comparison.

The plugin now uses `sprocket.materialselector.cfg`. On the first start, it copies `nl.roan.sprocket.materialselector.cfg` only if the new file does not exist. The old file stays as a backup, and an existing new file always wins. The `[UI]` setting `Armour material section open` defaults to `true`.

Saved material IDs and the response filename remain unchanged. Material availability follows the game's Technology/date rules, including your edited Technology dates. The included five presets retain their configured date of 3 September 1945; this is a gameplay default, not a universal historical cutoff. See [armour setup](ARMOUR-RESPONSES.md) for customization.

The separate **optional-balanced-materials.zip** supplies eleven additional passive gameplay presets. They are optional and do not replace vanilla RHA or sheet metal.

## Troubleshooting and rollback

Check `BepInEx/LogOutput.log` if the dropdown is missing. Confirm the loader works, remove duplicate copies of this plugin, and check material JSON formatting. A material can be unavailable for the vehicle's current technology context. Extra protection requires the enabled catalogue, matching material properties and Shell Selector.

Restore your backed-up DLL and matching settings/assets to roll back. Before uninstalling, change vehicles using custom materials back to stock materials and save them. Keep your vehicle backups.

Yep, this is **vibe coded**: built with AI assistance. Weight updates and vehicle saving/loading have been tested; this remains a beta. The mod code is MIT licensed. No game or loader binaries are bundled.

[Support my ChatGPT budget and help me reverse engineer Sprocket](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6).
