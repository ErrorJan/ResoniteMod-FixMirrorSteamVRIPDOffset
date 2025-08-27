# Fix Mirror SteamVR IPD Offset

A (for now until I can find a better way) [BepInEx 5](https://docs.bepinex.dev/v5.4.21/articles/user_guide/installation/index.html) Mod and a [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/).
<br>The BepInEx 5 mod is the actual mod, whereas the ResoniteMod is just for config. Feel free to only install the BepInEx5 Mod

When offsetting virtual IPD in SteamVR configs (steamvr.vrsettings) the mirror doesn't respect that, this mod fixes this issue.

To change your virtual IPD with steamvr.vrsettings open this file with a text editor. This config should be by default on Windows in ``C:\Program Files (x86)\Steam\config\steamvr.vrsettings``
It's good to know how JSON works to be able to modify this config.
Go to where it says ``"steamvr"`` and add the key-value pair ``"ipdOffset" : 0.039,`` with whatever you need (Example here with 0.039). Watch it! These are in meters!
The default IPD by SteamVR seems to always be "0.063", so when adding an offset, make sure to add it to the default IPD

It should look something like this:
```
{
   "steamvr" : {
      "ipdOffset" : 0.039,
   }
}
```

You can specify the IPD Offset in multiple ways:
- Letting the mod read from steamvr.vrsettings automagically. (No manual specification needed)
- Creating a file called "IPDOffset.txt" in the root folder of the renderer and writing something like "0.02" into it.
- Configuring the IPD through [ResoniteModSettings](https://github.com/badhaloninja/ResoniteModSettings) and ticking the checkbox to use RML's Configs

These configs have a ranked importance. ``IPDOffset.txt`` is preferred, then comes SteamVR's config.
If IPDOffset.txt is empty or isn't a number, it'll be ignored.
The configs are only loaded when the game starts!<br>
The [ResoniteModSettings](https://github.com/badhaloninja/ResoniteModSettings)/RML's config are only used, when checking the checkbox, so they have the least importance in a way by default. These are updated immediately!

## Installation
1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.3/BepInEx_win_x64_5.4.23.3.zip) to the Renderer folder in Resonite. Usually `C:\Program Files (x86)\Steam\steamapps\common\Resonite\Renderer`
2. Place the [FixMirrorSteamVRIPDOffset_BepInEx.dll](https://github.com/ErrorJan/ResoniteMod-FixMirrorSteamVRIPDOffset/releases/latest/download/FixMirrorSteamVRIPDOffset_BepInEx.dll) in the BepInEx/plugins folder. Usually `C:\Program Files (x86)\Steam\steamapps\common\Resonite\Renderer\BepInEx\plugins`
3. Optionally. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
4. Place [FixMirrorSteamVRIPDOffset.dll](https://github.com/ErrorJan/ResoniteMod-FixMirrorSteamVRIPDOffset/releases/latest/download/FixMirrorSteamVRIPDOffset.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create this folder for you.
5. Start the game. If you want to verify that the mod is working you can check your Resonite logs.
