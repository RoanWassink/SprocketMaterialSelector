# Build from source (optional)

Players should use the release ZIP. Building requires .NET SDK 8 and a working local Sprocket 0.2.55.5 / BepInEx 6 IL2CPP installation with generated interop.

Run `dotnet build SprocketMaterialSelector.csproj -c Release -p:GameDir="C:\path\to\Sprocket"` from this source folder. Output: `bin/Release/net6.0/SprocketMaterialSelector.dll`.

Game and loader assemblies are referenced locally and are not redistributed. Building does not install the plugin. Close the game and back up existing mod files before copying the resulting DLL.
