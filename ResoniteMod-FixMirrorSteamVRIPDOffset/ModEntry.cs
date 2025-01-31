using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;
using HarmonyLib;
using ResoniteModLoader;
using UnityFrooxEngineRunner;
using Newtonsoft.Json.Linq;

namespace FixMirrorSteamVRIPDOffset;

// More info on creating mods can be found https://github.com/resonite-modding-group/ResoniteModLoader/wiki/Creating-Mods
// My first mod I ever did, don't be harsh xD
public class ModEntry : ResoniteMod 
{
	internal const string VERSION_CONSTANT = "1.0.0";
	public override string Name => "Fix Mirror - SteamVR IPDOffset";
	public override string Author => "ErrorJan";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/ErrorJan/ResoniteMod-FixMirrorSteamVRIPDOffset/";
	
	private static float m_ipdOffset;
	private static ModConfiguration? rmlConfig;

	public override void OnEngineInit() 
	{
		//Thread.Sleep( 30 * 1000 ); // For debugging
		//ManualHackyBreakPoint();

		rmlConfig = GetConfiguration();
		rmlConfig?.Save( true );

		try {
			GetIPDOffsetSource();
		} catch ( Exception e ) {
			Warn( "Could not load IPD Offsets. Not from Resonite Dir nor from SteamVR!" );
			Warn( e.Message );
		}

		Harmony harmony = new Harmony( "ErrorJan.FixMirrorSteamVRIPDOffset" );
		harmony.PatchAll();
	}
	
	// Optional Configs.
	[AutoRegisterConfigKey]
	private static readonly 
		ModConfigurationKey<bool> modEnabled = 
			new("Enabled", 
				"Should the mod be enabled? (Has immediate effect!)", 
				() => true);
	
	[AutoRegisterConfigKey]
	private static readonly 
		ModConfigurationKey<bool> preferThisConfig = 
			new("Prefer this config", 
				"Should the offset in here (ResoniteModLoader Config) be preffered? (Has immediate effect!)", 
				() => false);
	[AutoRegisterConfigKey]
	private static readonly 
		ModConfigurationKey<float> offset = 
			new("offset", 
				"How much should the IPD be offset? (Has immediate effect!)", 
				() => 0.0f);

	// Terribly if-nested Function. Eh, does it's job.
	private static void GetIPDOffsetSource() 
	{
		string localFileName = $"{ Directory.GetCurrentDirectory() }/IPDOffset.txt";
		string ovrpathsFileName = $"{ Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ) }/openvr/openvrpaths.vrpath";
		string steamvrsettingsPath;
		
		if ( File.Exists( localFileName ) ) 
		{
			string s = File.ReadAllText( localFileName );
			
			if ( !string.IsNullOrWhiteSpace( s ) && float.TryParse( s, out m_ipdOffset ) ) {
				Msg( "Using local file!" );
				return;
			}
		}

		if ( File.Exists( ovrpathsFileName ) ) 
		{
			string s = File.ReadAllText( ovrpathsFileName );
			
			JObject ovrpaths = JObject.Parse( s );
			
			JToken? ovrconfigPaths = ovrpaths[ "config" ];
			if ( ovrconfigPaths != null ) 
			{
				foreach ( var path in ovrconfigPaths ) 
				{
					steamvrsettingsPath = $"{ path }/steamvr.vrsettings";
				
					if ( File.Exists( steamvrsettingsPath ) ) 
					{
						s = File.ReadAllText( steamvrsettingsPath );
						JObject steamvrsettings = JObject.Parse( s );
						JToken? steamvrconfigPaths = steamvrsettings[ "steamvr" ]?[ "ipdOffset" ];
						if ( steamvrconfigPaths != null ) 
						{
							m_ipdOffset = (float)steamvrconfigPaths;
							Msg( "Using SteamVR Config!" );
							return;
						}
					}
				}	
			}
		}
		
		Error( "Couldn't find any file with IPD offset data!" );
	}
	
	// A hacky way to be able to trigger the debugger
	/*public static void ManualHackyBreakPoint()
	{
		CommentBehaviour c = new CommentBehaviour();
		c.AssignConnector( null );
	}*/

	// Need to use a Transpiler, since what I want to edit is in the middle of the method and I cannot
	// Postfix UnityEngine::Camera::get_stereoSeparation, since that is linked to an internal C++ Method,
	// which I cannot access..
	// Transpiler it is!
	[HarmonyPatch( typeof( CameraPortal ), "OnWillRenderObject" )]
	public static class CameraPortal_OnWillRenderObject_Patch 
	{
		// Will be called by UnityFrooxEngineRunner::CameraPortal::OnWillRenderObject
		public static float AddSteamVRIPDOffset( float original )
		{
			if ( !rmlConfig?.GetValue( modEnabled ) ?? false ) 
				return original;

			if ( rmlConfig?.GetValue( preferThisConfig ) ?? false )
				return original + rmlConfig.GetValue( offset );
			
			return original + m_ipdOffset;
		}

		// Harmony will call this!
		// This modifies the Assembly's OpCodes
		static IEnumerable<CodeInstruction> Transpiler( IEnumerable<CodeInstruction> instructions ) 
		{
			// Get the MethodInfo from the method which I want to add an offset to
			var stereoSeparationCall = AccessTools.Method( typeof( Camera ), "get_stereoSeparation" );
			// Call my function to get the offset!
			var addSteamVRIPDOffsetFunc = SymbolExtensions.GetMethodInfo( () => AddSteamVRIPDOffset );
			
			// Iterate through all OpCodes of the CIL/IL Assembly
			// Return the entire assembly, but when UnityEngine::Camera::get_stereoSeparation gets called add
			// some OpCodes to offset the stereoSeparation.
			foreach ( var instruction in instructions ) 
			{
				// If this instruction plans on calling the UnityEngine::Camera::get_stereoSeparation via a
				// "callvirt" OpCode (or any other), insert after that a "call" opcode to my function
				//
				// Example: Before the code looked like this:
				//     Vector3 camPos = current.transform.TransformPoint( new Vector3( -0.5f * current.stereoSeparation, 0.0f, 0.0f ) );
				// The inserted Call OpCode will call my function and change the code to look like this:
				//	   ... new Vector3( -0.5f * AddSteamVRIPDOffset( current.stereoSeparation ), 0.0f, 0.0f ) );
				// This works because the way CIL passes parameters into methods/functions it puts them onto the stack.
				// The multiplication with -0.5f itself expects on the stack next a value, which current.stereoSeparation would have provided
				// Since I before the multiply call my function, which has a parameter, it'll pop the value current.stereoSeparation
				// from the stack and give me access to it. Then with my function I add a new value onto the Stack, which then
				// the multiply OpCode can use.
				if ( instruction.Calls( stereoSeparationCall ) ) 
				{
					yield return instruction;
					yield return new CodeInstruction( OpCodes.Call, addSteamVRIPDOffsetFunc );
					
					// Hard-coding values isn't the *best* idea, but for reference this is how you'd do it
					// instead via hard coding:
					//yield return new CodeInstruction( OpCodes.Ldc_R4, 1f ); // Loads a float32 (4 bytes) with value 1 onto the stack
					//yield return new CodeInstruction( OpCodes.Add ); // Adds the two immediate following values together
					continue;
				}
				yield return instruction;
			}
		}
	}
}
