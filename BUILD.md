# Build from source

Players can use the release ZIP without compiling. Developers need .NET SDK 8, the current Sprocket BepInEx IL2CPP installation and generated interop, plus Shell Selector 0.13.0 and Sprocket Json Editor 0.1.0 installed as local build references.

Run `dotnet build SprocketMaterialSelector.csproj -c Release -p:GameDir="C:\path\to\Sprocket"`. Output is `bin/Release/net6.0/SprocketMaterialSelector.dll`. Building does not install the mod. Game and loader assemblies are referenced locally and are not included.

Authored assets are in `files`; release catalogues are in `defaults`. The legacy embedded editor templates under `examples` remain part of the accepted runtime implementation.
