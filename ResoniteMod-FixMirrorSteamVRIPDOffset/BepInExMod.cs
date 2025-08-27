#if NETSTANDARD2_0
using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Renderite.Shared;
using Renderite.Unity;
using Valve.Newtonsoft.Json.Linq;

namespace FixMirrorSteamVRIPDOffset;

[BepInPlugin(  ModInfo.ModRDName, ModInfo.ModName, ModInfo.ModVersion )]
public class BepInExMod : BaseUnityPlugin, IMessageDeserializer
{
	public static ManualLogSource ModLogger => _logger;
	
	private static ManualLogSource _logger = null!;
	private static string _currentResoniteQueueName = "";
	private static long _currentResoniteQueueCapacity = 0;
	private static ModMessagingManager? _messagingManager;
	
	private void Awake()
	{
		_logger = base.Logger;
		_logger.LogInfo($"Plugin {ModInfo.ModRDName} is loaded! Initializing...");
		
		Harmony harmony = new( ModInfo.ModRDName );
		harmony.CreateClassProcessor( typeof( CameraPortal_OnWillRenderObject_UnityPatch ) ).Patch();
		
		if ( CameraPortal_OnWillRenderObject_UnityPatch.patched )
			_logger.LogInfo("Patch successfull!");
		else 
		{
			_logger.LogWarning("Patch failed...");
			return;
		}
		
		CameraPortal_OnWillRenderObject_UnityPatch.ipdOffset = GetIPDOffsetFromConfigFiles() ?? 0f;
		SetupCommunicationWithFrooxEngine();
		
		_logger.LogInfo("Finished initializing!");
	}

	private void SetupCommunicationWithFrooxEngine() 
	{
		// Communicate with FrooxEngine for configs
		RenderingManager? manager = FindObjectOfType( typeof( RenderingManager ) ) as RenderingManager;
		if ( manager == null ) 
		{
			_logger.LogError( "Could not find RenderingManager!" );
			return;
		} 
		
		object?[] args = { null, null };		
		AccessTools.Method(typeof(RenderingManager), "GetConnectionParameters").Invoke( manager, args );
		if (args[0] == null || args[1] == null) 
		{
			_logger.LogError( "Could not find queuename or capacity!" );
			return;
		}
		
		_currentResoniteQueueName = (string)args[0]!;
		_currentResoniteQueueCapacity = (long)args[1]!;

		_messagingManager = new( PackerMemoryPool.Instance );
		_messagingManager.Connect( _currentResoniteQueueName + ModInfo.ModRDName, false, _currentResoniteQueueCapacity );
		_messagingManager.OnReceivingException += MessageException;
		_messagingManager.RegisterMessageHandler( this );
		_messagingManager.StartReceiving();
	}
	
	public void Deserialize( string messageID, MemoryUnpacker unpacker ) 
	{
		switch ( messageID ) 
		{
			case ModInfo.ModConfigKeys.ModEnabled:
				CameraPortal_OnWillRenderObject_UnityPatch.modEnabled = unpacker.Read<bool>();
				_logger.LogInfo( $"ModEnabled: {CameraPortal_OnWillRenderObject_UnityPatch.modEnabled}" );
				break;
			case ModInfo.ModConfigKeys.OverridenIPDOffset:
				CameraPortal_OnWillRenderObject_UnityPatch.overridenIPDOffset = unpacker.Read<float>();
				_logger.LogInfo( $"overridenIPDOffset: {CameraPortal_OnWillRenderObject_UnityPatch.overridenIPDOffset}" );
				break;
			case ModInfo.ModConfigKeys.OverrideOffset:
				CameraPortal_OnWillRenderObject_UnityPatch.overrideIPDOffset = unpacker.Read<bool>();
				_logger.LogInfo( $"overrideIPDOffset: {CameraPortal_OnWillRenderObject_UnityPatch.overrideIPDOffset}" );
				break;
			default:
				_logger.LogWarning( $"Unknown messageID: {messageID}" );
				break;
		}
	}

	private void MessageException( Exception ex ) 
	{
		_logger.LogError( ex.Message );
	}
	
	// Terribly if-nested Function. Eh, does it's job.
	private static float? GetIPDOffsetFromConfigFiles() 
	{
		try 
		{
			string localFileName = $"{ Directory.GetCurrentDirectory() }/IPDOffset.txt";
			string ovrpathsFileName = $"{ Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ) }/openvr/openvrpaths.vrpath";
			string steamvrsettingsPath;
			float ipdOffset;
		
			if ( File.Exists( localFileName ) ) 
			{
				string s = File.ReadAllText( localFileName );
			
				if ( !string.IsNullOrWhiteSpace( s ) && float.TryParse( s, out ipdOffset ) ) {
					_logger.LogInfo( "Using local file!" );
					return ipdOffset;
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
								ipdOffset = (float)steamvrconfigPaths;
								_logger.LogInfo( "Using SteamVR Config!" );
								return ipdOffset;
							}
						}
					}	
				}
			}
		
			_logger.LogWarning( "Couldn't find any file with IPD offset data!" );
			return null;
		}
		catch ( Exception ) 
		{
			_logger.LogWarning( "Couldn't find any file with IPD offset data!" );
			return null;
		}
	}
		
}
#endif
