#!/bin/sh
export PATH="$PATH:$HOME/.dotnet/tools"
export RAINWORLD_ROOT="$HOME/Desktop/Games/Rain World"

echo '>> COPY'
mkdir -p lib
cp "$RAINWORLD_ROOT/RainWorld_Data/Managed/Assembly-CSharp.dll" lib/
cp "$RAINWORLD_ROOT/RainWorld_Data/Managed/Assembly-CSharp-firstpass.dll" lib/
cp "$RAINWORLD_ROOT/RainWorld_Data/Managed/UnityEngine.dll" lib/
cp "$RAINWORLD_ROOT/RainWorld_Data/Managed/UnityEngine.CoreModule.dll" lib/
cp "$RAINWORLD_ROOT/RainWorld_Data/Managed/UnityEngine.InputLegacyModule.dll" lib/
cp "$RAINWORLD_ROOT/RainWorld_Data/Managed/com.rlabrecque.steamworks.net.dll" lib/
cp "$RAINWORLD_ROOT/BepInEx/plugins/HOOKS-Assembly-CSharp.dll" lib/
cp "$RAINWORLD_ROOT/BepInEx/core/0Harmony.dll" lib/
cp "$RAINWORLD_ROOT/BepInEx/core/BepInEx.dll" lib/
cp "$RAINWORLD_ROOT/BepInEx/core/Mono.Cecil.dll" lib/
cp "$RAINWORLD_ROOT/BepInEx/core/MonoMod.dll" lib/
cp "$RAINWORLD_ROOT/BepInEx/core/MonoMod.Utils.dll" lib/
cp "$RAINWORLD_ROOT/BepInEx/utils/PUBLIC-Assembly-CSharp.dll" lib/

# meadow must be stripped
echo '>> STRIP'
cp "$RAINWORLD_ROOT/RainWorld_Data/StreamingAssets/mods/rainmeadow/plugins/Rain Meadow.dll" lib/Rain-Meadow.dll
[ -f nstrip.exe ] || mv NStrip.exe nstrip.exe
[ -f nstrip.exe ] || exit
[ -f lib/PUBLIC-Rain-Meadow.dll ] || WINEARCH=win64 WINEPREFIX=~/.wine64 wine nstrip.exe -p --remove-readonly -cg --cg-exclude-events lib/Rain-Meadow.dll lib/PUBLIC-Rain-Meadow.dll || exit
rm lib/Rain-Meadow.dll

echo '>> BUILD'
dotnet build || exit

echo '>> COPY'
cp bin/* "$RAINWORLD_ROOT/RainWorld_Data/StreamingAssets/mods/meadow-menagerie/plugins"

echo '>> FINISH'
