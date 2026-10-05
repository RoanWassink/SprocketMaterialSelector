# Sprocket MaterialSelector

Choose armour materials with different protection, density, spall and cost characteristics.

**v0.4.5 — beta.** Modern armour materials now use the vehicle design date from 3 September 1945 instead of requiring an era named Coldwar. Valid custom postwar and future eras are supported. Native technology availability still applies; material protection, mass and costs are unchanged.

## Requirements

- Sprocket **0.2.55.5**, Windows x64, Unity 6000.3.21f1.
- A working **Sprocket Mod Loader / BepInEx 6 IL2CPP (6.0.0-be.788)** setup with its runtime and generated interop. Loader installation is separate. Stock BepInEx alone is not claimed equivalent to the tested Sprocket-specific setup.
- Quality of Life is not required or included. Other game versions have not been verified.

## Install and update

1. Install a working Sprocket Mod Loader / BepInEx 6 IL2CPP setup, run Sprocket once, then close it. The loader is a separate prerequisite and is not included.
2. Download **SprocketMaterialSelector-v0.4.5.zip** from [this release](https://github.com/RoanWassink/SprocketMaterialSelector/releases/tag/v0.4.5).
3. In Steam, use Sprocket > Manage > Browse local files. Copy the ZIP's folders into the folder containing Sprocket.exe. Merge folders; keep the internal structure intact.
4. Keep one copy of each plugin. Back up matching mod files and vehicle saves before updating. Never replace the whole BepInEx folder.
5. Preserve existing BepInEx/config files, customized thermal-models.json and sound overrides. Install required dependencies separately. Restart the game.

## Usage, controls and settings

Select an armour plate's material in its inspector. Density changes weight, RHA factor changes passive resistance, spall factor changes fragments, and price is constrained by a native balance floor. Material Selector alone does not produce active ERA/NERA benefits: use matching Shell Selector and an enabled response catalogue, plus the Cold War core for era-dependent recipes. New material definitions and the complete additive heavy ERA entry are provided under examples. See [armour setup and customization](ARMOUR-RESPONSES.md). Existing material/Technology edits must be backed up and merged rather than silently replaced.

## Troubleshooting, saves and rollback

If the mod is absent, check BepInEx/LogOutput.log for the mod name, missing dependencies, duplicate plugin versions or invalid configuration. When this mod requires Keybinds, missing/incompatible Keybinds causes the mod to be skipped; old direct-key CFG entries do not replace that requirement. Preserve a malformed file for inspection instead of overwriting all your settings. Restart after repairs.

Restore your backed-up mod files and settings together for rollback. Do not delete an entire shared folder. Custom parts/materials may be referenced by vehicle saves: return affected vehicles to stock parts/materials and save before uninstalling. Keep save backups; installed mods and release archives do not back up every vehicle automatically.

## Credits and support

Made with AI assistance. Mod code is MIT licensed; native Sprocket meshes/icons are resolved from your installed game and are not bundled. Donation: [Support my ChatGPT budget and help me reverse engineer Sprocket to make more mods](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6).

## Where to get the separate loader

Use [Hans21223's Sprocket Mod Loader](https://github.com/Hans21223/Sprocket-Mod-Loader) and follow its [manual installation guide](https://github.com/Hans21223/Sprocket-Mod-Loader/blob/main/package/MANUAL-INSTALL.md) or its documented manager installation. That upstream project targets the tested Sprocket version and supplies the Sprocket-specific patch. These mod downloads do not install the loader. Follow one upstream loader method and its update/backup instructions; the creator's supplied ModManager archive is not redistributed here.

## Custom eras and this update

Modern armour materials now use the vehicle design date from 3 September 1945 instead of requiring an era named Coldwar. Valid custom postwar and future eras are supported. Native technology availability still applies; material protection, mass and costs are unchanged. The cutoff is inclusive. Availability follows the owning design and a valid registered era timeline, not the displayed era label. Missing or malformed dates/timelines fail closed. The game's last-era date sentinel is resolved from the actual final era start; saved dates are not rewritten. This is a pack availability policy, not a claim that every included technology existed in 1945.

Use **Shell Selector 0.12.4** for the repaired impact reactions. Material Selector handles selection and passive properties; it does not apply reactive effects alone. Keep your existing custom Technology files and response catalogue. No new preset values are required.
