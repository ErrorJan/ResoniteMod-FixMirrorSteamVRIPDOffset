namespace FixMirrorSteamVRIPDOffset;

public static class ModInfo 
{
	internal const string ModVersion = "2.0.0";
	public const string ModName = "Fix Mirror - SteamVR IPDOffset";
	public const string ModAuthor = "ErrorJan";
	public const string ModPage = "https://github.com/ErrorJan/ResoniteMod-FixMirrorSteamVRIPDOffset/";
	public const string ModRDName = "ErrorJan.FixMirrorSteamVRIPDOffset";
	public const string ModDescription =
		"When offsetting virtual IPD in SteamVR configs (steamvr.vrsettings) the mirror doesn't respect that, this mod fixes this issue.";
	public const string ModCopyright = "Copyright © 2025 ErrorJan";

	public static class ModConfigKeys 
	{
		public const string ModEnabled = "modEnabled";
		public const string OverrideOffset = "overrideOffset";
		public const string OverridenIPDOffset = "ipdOffset";
	}
}
