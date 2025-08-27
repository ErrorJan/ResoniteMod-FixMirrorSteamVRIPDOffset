using Renderite.Shared;
namespace FixMirrorSteamVRIPDOffset;

public interface IMessageDeserializer 
{
	public void Deserialize( string messageID, MemoryUnpacker unpacker );	
}
