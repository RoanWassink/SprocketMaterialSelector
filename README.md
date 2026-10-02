# Sprocket Material Selector

A vibe-coded BepInEx IL2CPP plugin that adds an **Armour material** dropdown to Sprocket's plate structure editor. Pick custom armour materials for your hull, turret, or other plate structures.

**Built with AI assistance.** Mass updates and vehicle saving/loading have been tested and work.

## Requirements

- Sprocket **0.2.55.5** (Unity **6000.3.21f1**).
- A working **Sprocket Mod Loader / BepInEx 6 IL2CPP** setup—the same environment used by Hans21223's *Sprocket Quality of Life*.
- Quality of Life itself is optional.

## Installation

1. Run the game once with the mod loader installed, then close it.
2. Download **SprocketMaterialSelector.dll** from this repository's **Releases** section.
3. Drop the DLL into:

   ```text
   Sprocket\BepInEx\plugins\
   ```

4. Launch the game, select a plate structure, and open **Armour material** in its editor.

No compiling needed. Back up your vehicle saves before experimenting.

## Custom materials

Put custom armour material JSON files in:

```text
Sprocket\Sprocket_Data\StreamingAssets\Technology\
```

Use **Reload armour materials** to pick up new files without restarting the game. The dropdown discovers JSON entries containing `type` and `properties.rhaFactor`.

An optional gameplay-oriented materials pack is included in **Optional Balanced Materials**. Copy its JSON files into the Technology folder. See the pack's README for details; these are gameplay approximations.

## Troubleshooting

Check `Sprocket\BepInEx\LogOutput.log` for messages containing `Sprocket Material Selector`. After changing a material, `After vanilla build` reports the updated armour, component, and vehicle masses.

When reporting an issue, include your game/mod-loader version, what you did, and the relevant log lines.

## Building from source

For contributors: install the .NET SDK and start the game once with the working mod loader so `BepInEx\interop` exists.

```powershell
dotnet build -c Release -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Sprocket"
```

The output is `bin\Release\net6.0\SprocketMaterialSelector.dll`. The included `build-install.ps1` builds and installs it while the game is closed. Game and loader assemblies are referenced from your local installation and are not included here.

## Credits

Created by RoanWassink with AI assistance. The native inspector integration follows the pattern used by Hans21223's *Sprocket Quality of Life*.
