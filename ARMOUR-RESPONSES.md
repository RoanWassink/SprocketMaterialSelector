# Armour setup and customization

Material Selector reads armour definitions from `Sprocket_Data/StreamingAssets/Technology`. The original five presets are glass textolite, NERA cassette, light ERA, passive composite and heavy ERA. This release adds Relikt, Kontakt-1, Kontakt-5, Nizh and Duplet. Complete native definitions are in `files/Sprocket_Data/StreamingAssets/Technology`.

For extra reactions, install matching Shell Selector 0.13.0 and Sprocket Json Editor 0.1.0, use the matching definitions and the complete `defaults/sprocket.armour.responses.json` catalogue. Copy the catalogue to `BepInEx/config` only on a fresh installation. It defaults to `enabled: false`; set this to `true` to opt in, then restart. Updating the DLL does not replace or enable a customized catalogue.

Existing installations: back up the catalogue and compare it with the default. Add missing recipes by their `responseId` and compatible material IDs without duplicating or replacing customized entries. Keep your root settings. If there is a conflicting recipe, resolve it manually before enabling reactions. The default contains all ten recipes; exact cassette/mount routes are in `defaults/sprocket.era.bindings.json`. The additive installer preserves existing settings and recipes and stops on conflicting ERA routes.

The native Technology `date` controls when a material is available. `historicalDate` in the response catalogue is descriptive metadata; the legacy `minimumEra` token is not an era-name requirement. Changing a material's saved ID breaks its existing references. Editing density/RHA/spall can also make an associated response recipe incompatible: keep the recipe's passive-property fingerprint consistent with the material definition.

Passive protection, density and spall remain native material properties. Costs use the game's armour/vehicle cost system with the existing balance floor; vanilla RHA pricing remains the baseline. These are gameplay presets, not measured engineering specifications.

Extra protection is conditional. Resolved layers require upstream steel and the configured gap for their APFSDS effect. NERA and ERA depend on thickness and impact angle. Light ERA adds no APFSDS benefit. Heavy ERA behavior varies by recipe and angle; check the selected preset rather than assuming all types share the same protection. ERA reacts once per local area; afterward its passive armour remains. Read the selected material's tooltip for the applicable conditions.

Catalogue settings include global HEAT/kinetic loss caps, steel preconditioning, passive fingerprints, penetration-retention factors, angle curves, thickness ranges, gaps and ERA cell size. The complete supplied JSON is the supported example. Invalid catalogues disable extra descriptions/reactions rather than silently replacing your file. Material Selector displays the conditions; Shell Selector applies impact reactions.

Open **Edit materials** in the plate editor to change properties, labels, dates and response settings, then **Save** to refresh the current vehicle and matching Shell responses. Back up custom materials before experimenting.

Placeable ERA uses exact independent cassette identities. Each regular module is one spent cell; the large three-zone Duplet hull part has three independent cells. Passive mounts do not activate. Cassettes hide after consumption and return on entering Edit; passive collision and mass remain. The older one-zone hull part keeps its saved GUID. No chain reaction, full tandem-warhead simulation or exact historical protection is claimed.
