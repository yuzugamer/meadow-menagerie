totally feel free to make pull requests or contributions for any reason! just make sure to playtest 'em first :p

### required dependencies
- 0Harmony.dll
- Assembly-CSharp-firstpass.dll
- BepInEx.dll
- com.rlabrecque.steamworks.net.dll
- HOOKS-Assembly-CSharp.dll
- Mono.Cecil.dll
- MonoMod.dll
- MonoMod.RuntimeDetour.dll
- MonoMod.Utils.dll
- Newtonsoft.Json.dll
- PUBLIC-Assembly-CSharp.dll
- Unity.Mathematics.dll
- UnityEngine.CoreModule.dll
- UnityEngine.dll
- UnityEngine.InputLegacyModule.dll

and importantly, a stripped/publicized version of Rain-Meadow.dll
not technically necessary, as the mod's assembly already will ignore permissions and access checks, but most compilers will refuse to build without it.
i just used nstrip for it, pretty convenient. exact command i used was `nstrip -p --remove-readonly -cg --cg-exclude-events Rain-Meadow.dll PUBLIC-Rain-Meadow.dll`