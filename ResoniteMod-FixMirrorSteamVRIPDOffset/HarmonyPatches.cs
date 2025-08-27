namespace FixMirrorSteamVRIPDOffset;

#if NETSTANDARD2_0
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

// Need to use a Transpiler, since what I want to edit is in the middle of the method and I cannot
// Postfix UnityEngine::Camera::get_stereoSeparation, since that is linked to an internal C++ Method,
// which I cannot access..
// Transpiler it is!
[HarmonyPatch( typeof( CameraPortal ), "OnWillRenderObject" )]
public static class CameraPortal_OnWillRenderObject_UnityPatch 
{
	public static bool patched = false;
	
	public static bool modEnabled = true;
	public static float ipdOffset = 0f;
	public static bool overrideIPDOffset = false;
	public static float overridenIPDOffset = 0f;
	
	// Will be called by UnityFrooxEngineRunner::CameraPortal::OnWillRenderObject
	public static float AddSteamVRIPDOffset( float original )
	{
		if ( !modEnabled )
			return original;
		
		if ( overrideIPDOffset )
			return original + overridenIPDOffset;
		
		return original + ipdOffset;
	}

	// Harmony will call this!
	// This modifies the Assembly's OpCodes
	static IEnumerable<CodeInstruction> Transpiler( IEnumerable<CodeInstruction> instructions ) 
	{
		// Get the MethodInfo from the method which I want to add an offset to
		var stereoSeparationCall = AccessTools.Method( typeof( Camera ), "get_stereoSeparation" );
		// Call my function to get the offset!
		var addSteamVRIPDOffsetFunc = AccessTools.Method( typeof( CameraPortal_OnWillRenderObject_UnityPatch ), nameof( AddSteamVRIPDOffset ) );
		
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

				patched = true;
				
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
#endif
