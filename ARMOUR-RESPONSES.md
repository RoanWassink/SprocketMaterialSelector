# Armour setup and customization

Material Selector reads armour definitions from `Sprocket_Data/StreamingAssets/Technology`. The five supplied presets are glass textolite, NERA cassette, light ERA, passive composite and heavy ERA. Their complete JSON definitions are in `Technology`.

For extra reactions, install matching Shell Selector 0.12.5, use the matching five-material definitions and the complete `examples/sprocket.armour.responses.json` catalogue. Copy the catalogue to `BepInEx/config` only on a fresh installation. It defaults to `enabled: false`; set this to `true` to opt in, then restart. Updating the DLL does not replace or enable a customized catalogue.

Existing installations: back up the catalogue and compare it with the default. Add missing recipes by their `responseId` and compatible material IDs without duplicating or replacing customized entries. Keep your root settings. If there is a conflicting recipe, resolve it manually before enabling reactions. The default contains all five recipes, including heavy ERA.

The native Technology `date` controls when a material is available. `historicalDate` in the response catalogue is descriptive metadata; the legacy `minimumEra` token is not an era-name requirement. Changing a material's saved ID breaks its existing references. Editing density/RHA/spall can also make an associated response recipe incompatible: keep the recipe's passive-property fingerprint consistent with the material definition.

Passive protection, density and spall remain native material properties. Costs use the game's armour/vehicle cost system with the existing balance floor; vanilla RHA pricing remains the baseline. These are gameplay presets, not measured engineering specifications.

Extra protection is conditional. Resolved layers require upstream steel and the configured gap for their APFSDS effect. NERA and ERA depend on thickness and impact angle. Light ERA adds no APFSDS benefit. Heavy ERA adds no extra APFSDS protection head-on. ERA reacts once per local area; afterward its passive armour remains. Read the selected material's tooltip for the applicable conditions.

Catalogue settings include global HEAT/kinetic loss caps, steel preconditioning, passive fingerprints, penetration-retention factors, angle curves, thickness ranges, gaps and ERA cell size. The complete supplied JSON is the supported example. Invalid catalogues disable extra descriptions/reactions rather than silently replacing your file. Material Selector displays the conditions; Shell Selector applies impact reactions.
