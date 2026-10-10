# Build from source

Use .NET SDK 8 to build the net6.0 plugin against your own working Sprocket BepInEx IL2CPP installation. Start the supported loader once to generate its interop. Run `dotnet build SprocketJsonEditor.csproj -c Release -p:GameDir="C:/path/to/Sprocket"`. No game or loader assemblies are included. Consumers must reference the built public DLL without bundling duplicate private copies.
