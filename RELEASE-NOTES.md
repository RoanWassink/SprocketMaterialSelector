<!-- sp-compat {"hamish.sprocket": "0.2.55.5", "bepinex.bepinex": "6.0.0-be.788"} -->

## What changes for you

Modern armour materials now use the vehicle design date from 3 September 1945 instead of requiring an era named Coldwar. Valid custom postwar and future eras are supported. Native technology availability still applies; material protection, mass and costs are unchanged.

**Beta.** 315 automatic checks passed, including parity with the Thermal date policy. Native custom-era selection and save/load testing remains pending.

The shared availability cutoff is **3 September 1945**, inclusive, without a finite future cutoff for valid registered eras. Earlier eras keep their supported features. Saved dates and customized settings are preserved. This does not change historical balance coefficients.

## Install or update

Requires Sprocket **0.2.55.5**, Windows x64 and a working **Sprocket Mod Loader / BepInEx 6 IL2CPP 6.0.0-be.788** setup. **Loader not included; Quality of Life not required.**


Close Sprocket and back up saves and matching mod files. Merge the ZIP's **BepInEx** and, where included, **Sprocket_Data** folders into the folder containing Sprocket.exe. Keep one DLL per plugin. **Preserve existing configs, custom Technology/material files, thermal-models.json and WAV overrides.**

For the supplied armour reactions, update **Material Selector 0.4.5 and Shell Selector 0.12.4 together**, retain the corresponding material definitions and ensure your response catalogue is enabled. Material Selector alone provides passive properties. No new preset values are needed.

See the [installation and customization guide](https://github.com/RoanWassink/SprocketMaterialSelector#readme) for requirements, examples and rollback.

[Support my ChatGPT budget and help me reverse engineer Sprocket to make more mods](https://www.paypal.com/donate/?hosted_button_id=7PE3SDBETXFQ6).
