# Fix Mirror SteamVR IPD Offset

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/).

When offsetting virtual IPD in SteamVR configs (steamvr.vrsettings) the mirror doesn't respect that, this mod fixes this issue.

To change your virtual IPD with steamvr.vrsettings open this file with a text editor. This config should be by default on Windows in ``C:\Program Files (x86)\Steam\config\steamvr.vrsettings``
It's good to know how JSON works to be able to modify this config.
Go to where it says ``"steamvr"`` and add the key-value pair ``"ipdOffset" : 0.039,`` with whatever you need (Example here with 0.039). Watch it! These are in meters!

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
- Creating a file called "IPDOffset.txt" in the root of where the game is installed and writing something like "0.02" into it.
- Configuring the IPD through [ResoniteModSettings](https://github.com/badhaloninja/ResoniteModSettings) and ticking the checkbox to use RML's Configs

These configs have a ranked importance. ``IPDOffset.txt`` is preferred, then comes SteamVR's config.
If IPDOffset.txt is empty or isn't a number, it'll be ignored.
The configs are only loaded when the game starts!<br>
The [ResoniteModSettings](https://github.com/badhaloninja/ResoniteModSettings)/RML's config are only used, when checking the checkbox, so they have the least importance in a way by default. These are updated immediately!

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Place [FixMirrorSteamVRIPDOffset.dll](https://github.com/ErrorJan/ResoniteMod-FixMirrorSteamVRIPDOffset/releases/latest/download/FixMirrorSteamVRIPDOffset.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create this folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Resonite logs.
