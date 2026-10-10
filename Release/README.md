# Sprocket Material Selector 0.5.0

**New: placeable ERA modules and an in-game material editor.** Add Kontakt-1, Kontakt-5, Relikt, Nizh or Duplet armour, including turret and hull variants. The large Duplet hull module has three independently spent zones. ERA parts follow your vehicle's paint and camouflage.

Edit material properties and armour responses from **Edit materials** in the plate editor. Save refreshes the current vehicle's material data and matching Shell Selector responses. Custom armour still uses Sprocket's weight and vehicle-cost system.

## Requirements

- Sprocket 0.2.55.5, Windows x64, Unity 6000.3.21f1.
- A working Hans21223 Sprocket Mod Loader / BepInEx 6 IL2CPP environment.
- **Shell Selector 0.13.0** and **Sprocket Json Editor 0.1.0**. Both are required by this version. Use the matching full pack for the easiest setup.

## Install or update

Download the release ZIP and extract it anywhere. No compiling needed. Close Sprocket, then run:

```powershell
.\Install.ps1 -GameDirectory 'C:\Program Files (x86)\Steam\steamapps\common\Sprocket'
```

Use your actual Steam game folder if different. The installer updates this plugin, adds missing materials, parts, recipes and bindings, and backs up changed files. Your existing response settings and recipes win. Unrecognized customized assets are kept and reported; conflicting ERA bindings stop installation before writes. You may need to reconcile those files yourself.

Fresh standalone installs start with extra armour responses **disabled**. With the required plugins installed, enable them in the material editor or set the top-level `enabled` in `BepInEx/config/sprocket.armour.responses.json` to `true`. Updates preserve your existing value. The full pack supplies its own starter setting.

Select a plate to choose its armour material or open **Edit materials**. Place ERA parts through the game's applique armour menu; use normal move, rotate, mirror and duplicate controls. They have fixed dimensions.

## Limits

ERA protection depends on the projectile, thickness, angle and enabled matching recipe. Each ordinary module is one spent cell; the three-zone hull variant has three. Spent cassettes hide in combat while mounts remain, and return on entering Edit. Passive collision and mass remain after spending. No chain reaction or full tandem-warhead simulation is provided. Dimensions and protection are gameplay estimates, not exact historical specifications. Premium material pricing uses native part/vehicle costs.

The older one-zone Duplet hull part and its saved identity remain supported. Back up vehicles before updating. Before uninstalling, return custom armour to stock materials and remove custom ERA parts, then save.

This is a **vibe-coded beta**, built with AI assistance. Mod source and original generated model/icon assets use the included MIT license. Game and loader binaries are not included. Quality of Life is optional.

For native dates, custom materials, matching recipe fingerprints and protection conditions, see [Armour setup and customization](ARMOUR-RESPONSES.md).

[Support my ChatGPT budget and help me reverse engineer Sprocket](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6).
