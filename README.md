# Sprocket Material Selector

A vibe-coded BepInEx IL2CPP plugin that adds an **Armour material** dropdown to Sprocket's plate structure editor. Pick custom armour materials for your hull, turret, or other plate structures.

**Built with AI assistance.** Mass updates, vehicle saving/loading and runtime cost balancing have been tested in-game.

<img width="666" height="447" alt="Armour material selector in Sprocket" src="https://github.com/user-attachments/assets/28fd5705-8f84-4877-b51a-c8dbd489e943" />

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

Version 0.4.0 applies an automatic runtime cost floor based on protection per
millimetre and protection per kilogram. Authors can request a higher price;
lower prices are raised to the calculated minimum. Original JSON files are
unchanged. Invalid or numerically unsupported materials are excluded from the
dropdown; loaded structures using them fall back to RHA with a warning.

The inspector displays weight efficiency, the effective runtime multiplier and,
when the floor applies, the requested multiplier. Vanilla RHA remains the reference:
1.0 RHA factor, 7850 kg/m³ and 2.0 cost multiplier.

The v0.4.0 optional pack has 11 presets: budget structural/cast steel, high-hardness
and premium armour steel, aluminium, magnesium, titanium, ceramic, heavy-alloy
inserts, elastomer filler and aramid liner. Its README compares thickness, mass and
material cost at equal protection. These are gameplay presets, not real engineering data.

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

Run the independent balance regression checks with:

```powershell
dotnet run --project tests/MaterialBalance.Tests.csproj -c Release
```

## Credits

Created by RoanWassink with AI assistance. The native inspector integration follows the pattern used by Hans21223's *Sprocket Quality of Life*.

## Donations
For ChatGPT budget. Helps me reverse engineer sprocket to add cool mods. https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6
