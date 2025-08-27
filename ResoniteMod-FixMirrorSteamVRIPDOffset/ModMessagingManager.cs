using System;
using System.Collections.Generic;
using System.Threading;
using Cloudtoid.Interprocess;
using Renderite.Shared;

namespace FixMirrorSteamVRIPDOffset;

// This is uhh very unoptimal as a general solution, but good enough for now

public class ModMessagingManager : IDisposable
{
	// 8 MiB ( "<< 20" == "2^20" )
	public const int DEFAULT_IPC_FILE_SIZE = 8 << 20;
	public bool IsConnected => _writer != null || _reader != null;
	public event Action<Exception>? OnReceivingException;
	
	public ModMessagingManager( IMemoryPackerEntityPool pool )
	{
		_pool = pool;
	}
	
	public void Connect( string ipcFilenameIdentifier, bool isFrooxEngine, long capacity = DEFAULT_IPC_FILE_SIZE ) 
	{
		if ( IsConnected )
			return;
		
		_writerBuffer = new( new byte[capacity] );
		_readerBuffer = new( new byte[capacity] );
		
		QueueFactory queueFactory = new();
		
		// F for FrooxEngine authority
		// R for Renderer authority
		QueueOptions writeQueueOption = new( ipcFilenameIdentifier + ( isFrooxEngine ? "F" : "R" ), capacity, isFrooxEngine );
		QueueOptions readQueueOption  = new( ipcFilenameIdentifier + ( isFrooxEngine ? "R" : "F" ), capacity, isFrooxEngine );
		
		_writer  = queueFactory.CreatePublisher( writeQueueOption );
		_reader = queueFactory.CreateSubscriber( readQueueOption );
		
		cancellation = new();
		_readerThread = new( ReceiveLoop );
		_readerThread.Priority = ThreadPriority.Highest;
		_readerThread.IsBackground = true;
	}

	public void StartReceiving() 
	{
		if ( !( _readerThread?.IsAlive ?? true ) )
			_readerThread.Start();
	}
	
	public void SendMessage<T>( string messageID, T data ) where T : unmanaged 
	{
		try
		{
			if ( _writer == null )
				throw new InvalidOperationException( "Cannot send messages when IPC is gone!" );
			lock ( _lock )
			{
				MemoryPacker packer = new( _writerBuffer.Span );
				
				packer.Write( messageID );
				packer.Write( data );
				
				int length = packer.ComputeLength( _writerBuffer.Span );
				
				if (length == 0)
					throw new Exception( $"Serializing message resulted in zero length. MessageID: {messageID}" );
				
				Span<byte> messageData = _writerBuffer.Span.Slice( 0, length );
				while ( !_writer.TryEnqueue( messageData ) )
				{
					// Queue doesn't have enough free capacity, stalling message writing!
					Thread.Sleep(10);
				}
			}
		}
		catch ( Exception ex )
		{
			OnReceivingException?.Invoke( ex );
			throw;
		}
	}

	public void Dispose() 
	{
		cancellation?.Cancel();
	}

	public void RegisterMessageHandler( IMessageDeserializer handler ) 
	{
		_messageHandlers.Add( handler );
	}
	
	private IMemoryPackerEntityPool _pool;
	private IPublisher? _writer;
	private ISubscriber? _reader;
	private Memory<byte> _writerBuffer;
	private Memory<byte> _readerBuffer;
	private CancellationTokenSource? cancellation;
	private Thread? _readerThread;
	private object _lock = new();
	private List<IMessageDeserializer> _messageHandlers = new();

	private void ReceiveLoop() 
	{
		if ( _writer == null || _reader == null || cancellation == null || _readerBuffer.IsEmpty || _writerBuffer.IsEmpty ) 
		{
			throw new InvalidOperationException( "Not connected. Did you forget to call Connect()?" );
		}
		
		try
		{
			while ( !cancellation.IsCancellationRequested )
			{
				ReadOnlyMemory<byte> readOnlyMemory = _reader.Dequeue( _readerBuffer, cancellation.Token );
				if ( cancellation.IsCancellationRequested )
					break;
				
				if ( !readOnlyMemory.IsEmpty )
				{
					MemoryUnpacker unpacker = new( readOnlyMemory.Span, _pool );
					
					try 
					{
						string messageID = unpacker.ReadString();
						
						foreach ( IMessageDeserializer handler in _messageHandlers )
							handler.Deserialize( messageID, unpacker );
					}
					catch ( Exception ex )
					{
						throw new Exception( $"Failure deserializing message. Message length: {readOnlyMemory.Span.Length}.", ex );
					}
				}
			}
		}
		catch ( Exception ex )
		{
			OnReceivingException?.Invoke( ex );
		}
		finally
		{
			_reader.Dispose();
			_writer.Dispose();
			_reader = null;
			_writer = null;
		}
	}

	private void DebugLog( string s ) 
	{
		#if NETSTANDARD2_0
		BepInExMod.ModLogger.LogInfo( s );
		#endif
		#if NET9_0
		ResoniteModLoader.ResoniteMod.Msg( s );
		#endif
	}
}
