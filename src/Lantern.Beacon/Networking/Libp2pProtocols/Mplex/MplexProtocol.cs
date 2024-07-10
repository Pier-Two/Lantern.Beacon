using System.Buffers;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Exceptions;

namespace Lantern.Beacon.Networking.Libp2pProtocols.Mplex;

public class MplexProtocol : SymmetricProtocol, IProtocol
{
    private const long MaxStreamId = (1L << 60) - 1;
    private const int MaxMessageSize = 1048576; 
    private readonly ConcurrentDictionary<IPeerContext, PeerConnectionState> _peerStates = new();
    private readonly ILogger? _logger;

    public MplexProtocol(MultiplexerSettings? multiplexerSettings = null, ILoggerFactory? loggerFactory = null)
    {
        multiplexerSettings?.Add(this);
        _logger = loggerFactory?.CreateLogger<MplexProtocol>();
    }
    
    public string Id => "/mplex/6.7.0";

    protected override async Task ConnectAsync(IChannel channel, IChannelFactory? channelFactory, IPeerContext context, bool isListener)
    {
        if (context == null)
        {
            throw new ArgumentException("Context cannot be null", nameof(context));
        }

        if (channelFactory == null)
        {
            throw new ArgumentException("ChannelFactory should be available for a muxer", nameof(channelFactory));
        }

        var peerState = new PeerConnectionState();
        _peerStates[context] = peerState;

        _logger?.LogInformation(isListener ? "Listen" : "Dial");

        var downChannelAwaiter = channel.GetAwaiter();
        context.Connected(context.RemotePeer);

        _ = Task.Run(() => HandleSubDialRequests(context, channelFactory, isListener, channel, peerState));

        try
        {
            while (!downChannelAwaiter.IsCompleted)
            {
                var message = await ReadMessageAsync(channel);
                await HandleMessageAsync(message, channel, channelFactory, context, peerState);
            }
        }
        catch (ChannelClosedException ex)
        {
            _logger?.LogDebug("Closed due to transport disconnection: {exception}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Closed with exception {exception}", ex.Message);
            Console.WriteLine(ex);
        }

        _logger?.LogDebug("Closing all channels");

        foreach (var upChannel in peerState.InitiatorChannels.Values)
        {
            _ = upChannel.Channel?.CloseAsync();
        }
        
        foreach (var upChannel in peerState.ReceiverChannels.Values)
        {
            _ = upChannel.Channel?.CloseAsync();
        }

        peerState.InitiatorChannels.Clear();
        peerState.ReceiverChannels.Clear();
        
        _peerStates.TryRemove(context, out _);
    }

    private void HandleSubDialRequests(IPeerContext context, IChannelFactory channelFactory, bool isListener, IChannel channel, PeerConnectionState peerState)
    {
        foreach (var request in context.SubDialRequests.GetConsumingEnumerable())
        {
            if (peerState.StreamIdCounter >= MaxStreamId)
            {
                throw new Exception("Stream ID counter exceeded the maximum value.");
            }
            
            var streamId = Interlocked.Increment(ref peerState.StreamIdCounter);
            
            _logger?.LogDebug("Handling sub dial request for protocol {protocol}", request.SubProtocol?.Id);
            
            var channelState = CreateUpChannel(streamId, request, channelFactory, isListener, context, channel, peerState);
            peerState.InitiatorChannels.TryAdd(streamId, channelState);
        }
    }

    private ChannelState CreateUpChannel(long streamId, IChannelRequest? channelRequest, IChannelFactory channelFactory, bool isListener, IPeerContext context, IChannel channel, PeerConnectionState peerState)
    {
        IChannel upChannel;

        if (isListener)
        {
            upChannel = channelFactory.SubListen(context);
        }
        else
        {
            IPeerContext dialContext = context.Fork();
            dialContext.SpecificProtocolRequest = channelRequest;
            upChannel = channelFactory.SubDial(dialContext);
        }

        var state = new ChannelState(upChannel, channelRequest);
        TaskCompletionSource? tcs = state.Request?.CompletionSource;

        upChannel.GetAwaiter().OnCompleted(() =>
        {
            tcs?.SetResult();
            
            if (isListener)
            {
                _logger?.LogDebug("Stream {stream id} (receiver): Removing channel for closing", streamId);
                peerState.ReceiverChannels.TryRemove(streamId, out _); 
            }
            else
            {
                _logger?.LogDebug("Stream {stream id} (initiator): Removing channel for closing", streamId);
                peerState.InitiatorChannels.TryRemove(streamId, out _); 
            }
        });

        // Initiate background processing of the channel
        _ = Task.Run(() => ProcessChannelAsync(channel, streamId, upChannel, isListener));

        return state;
    }

    private async Task ProcessChannelAsync(IChannel channel, long streamId, IChannel upChannel, bool isListener)
    {
        try
        {
            // If this is a listener, we need to create a new stream
            if (isListener)
            {
                await foreach (var upData in upChannel.ReadAllAsync())
                {
                    if (upData.Length > MaxMessageSize)
                    {
                        _logger?.LogError("Stream {streamId} (receiver): Data size exceeds the maximum allowed limit of {maxSize} bytes. Resetting stream.", streamId, MaxMessageSize);
                        await WriteMessageAsync(channel, new MplexMessage
                        {
                            Flag = MplexMessageFlag.ResetReceiver,
                            StreamId = streamId,
                            Data = default
                        });
                        return;
                    }

                    _logger?.LogDebug("Stream {streamId} (receiver): Collected data from upper channel for sending, length={length}", streamId, upData.Length);
                    await WriteMessageAsync(channel, new MplexMessage
                    {
                        Flag = MplexMessageFlag.MessageReceiver,
                        StreamId = streamId,
                        Data = upData
                    });
                }
            }
            else // If this is a dialer, we need to send a new stream request
            {
                var streamName = string.Empty;
                var streamNameBytes = System.Text.Encoding.UTF8.GetBytes(streamName);

                // Send NewStream message
                _logger?.LogDebug("Stream {streamId} (initiator): Creating new stream", streamId);
                await WriteMessageAsync(channel, new MplexMessage
                {
                    Flag = MplexMessageFlag.NewStream,
                    StreamId = streamId,
                    Data = new ReadOnlySequence<byte>(streamNameBytes)
                });

                // Send data from upper channel as MessageInitiator
                await foreach (var upData in upChannel.ReadAllAsync())
                {
                    if (upData.Length > MaxMessageSize)
                    {
                        _logger?.LogError("Stream {streamId} (initiator): Data size exceeds the maximum allowed limit of {maxSize} bytes. Resetting stream.", streamId, MaxMessageSize);
                        await WriteMessageAsync(channel, new MplexMessage
                        {
                            Flag = MplexMessageFlag.ResetInitiator,
                            StreamId = streamId,
                            Data = default
                        });
                        return;
                    }

                    _logger?.LogDebug("Stream {streamId} (initiator): Collected data from upper channel for sending, length={length}. Writing data to stream", streamId, upData.Length);
                    await WriteMessageAsync(channel, new MplexMessage
                    {
                        Flag = MplexMessageFlag.MessageInitiator,
                        StreamId = streamId,
                        Data = upData
                    });
                }

                // Send CloseInitiator message
                _logger?.LogDebug("Stream {streamId} (initiator): Finished sending all data from upper channel. Sending CloseInitiator", streamId);
                await WriteMessageAsync(channel, new MplexMessage
                {
                    Flag = MplexMessageFlag.CloseInitiator,
                    StreamId = streamId,
                    Data = default
                });
            }
        }
        catch (Exception e)
        {
            _logger?.LogDebug("Stream {streamId} ({isListener}): Unexpected error: {error}. Resetting stream", streamId, isListener ? "receiver" : "listener", e.Message);

            await WriteMessageAsync(channel, new MplexMessage
            {
                Flag = isListener ? MplexMessageFlag.ResetReceiver : MplexMessageFlag.ResetInitiator,
                StreamId = streamId,
                Data = default
            });
        }
    }
    
    private async Task HandleMessageAsync(MplexMessage message, IChannel channel, IChannelFactory? channelFactory, IPeerContext context, PeerConnectionState peerState)
    {
        if (channelFactory is null)
        {
            throw new ArgumentException("ChannelFactory should be available for a muxer", nameof(channelFactory));
        }

        var streamId = message.StreamId;
        var flag = message.Flag;
        
        // Check the size of the data 
        if (message.Data.Length > MaxMessageSize)
        {
            _logger?.LogError("Stream {streamId} ({isReceiver}): Received message with data size exceeding the limit of {maxSize} bytes. Resetting stream", 
                streamId, 
                flag == MplexMessageFlag.MessageInitiator ? "receiver" : "initiator",
                MaxMessageSize);
            
            await WriteMessageAsync(channel, new MplexMessage
            {
                Flag = flag == MplexMessageFlag.MessageInitiator ? MplexMessageFlag.ResetReceiver : MplexMessageFlag.ResetInitiator,
                StreamId = streamId,
                Data = default
            });
            
            return;
        }

        _logger?.LogDebug("Stream {streamId}: Decoded received message flag={flag}, len={len}", 
            streamId, 
            flag,
            message.Data.Length);
        
        // If this flag is for a new stream and the stream does not already exist, create a new stream channel
        if (flag == MplexMessageFlag.NewStream)
        {
            if (!peerState.ReceiverChannels.ContainsKey(streamId) && streamId <= MaxStreamId)
            {
                 _logger?.LogDebug("Stream {streamId} (receiver): Opening new stream", streamId);
                 
                 var newChannelState = CreateUpChannel(streamId, null, channelFactory, true, context, channel, peerState);
                 peerState.ReceiverChannels.TryAdd(streamId, newChannelState);
            }
            else
            {
                _logger?.LogDebug("Received a new stream request for existing stream ID {streamId}. Sending reset message", streamId);
                
                await WriteMessageAsync(channel, new MplexMessage
                {
                    Flag = MplexMessageFlag.ResetReceiver,
                    StreamId = streamId,
                    Data = default
                });
            }
            
            return;
        }
        
        // if (!peerState.ReceiverChannels.TryGetValue(streamId, out _) || !peerState.InitiatorChannels.TryGetValue(streamId, out _))
        // {
        //     if (message.Data.Length > 0)
        //     {
        //         _logger?.LogDebug("Stream {streamId} ({isReceiver}): Drain the data if stream not found", 
        //             streamId,
        //             flag == MplexMessageFlag.MessageInitiator ? "receiver" : "initiator");
        //         _ = channel.ReadAsync((int)message.Data.Length); 
        //     }
        //
        //     _logger?.LogDebug("Received a message for unknown stream {streamId}. Ignoring", streamId);
        //     return;
        // }
       
        switch (flag)
        {
            case MplexMessageFlag.MessageReceiver:
                if (!peerState.InitiatorChannels.ContainsKey(streamId))
                {
                    _logger?.LogDebug("Stream {streamId} (initiator): Received MessageReceiver for unknown stream. Ignoring", streamId);
                }
                else
                {
                    _logger?.LogDebug("Stream {streamId} (initiator): Received MessageReceiver. Writing data to channel", streamId);
                    peerState.InitiatorChannels[streamId].Channel?.WriteAsync(message.Data);
                }
                break;
            case MplexMessageFlag.MessageInitiator:
                if (!peerState.ReceiverChannels.ContainsKey(streamId))
                {
                    _logger?.LogDebug("Stream {streamId} (receiver): Received MessageInitiator for unknown stream. Ignoring", streamId);
                }
                else
                {
                    _logger?.LogDebug("Stream {streamId} (receiver): Received MessageInitiator. Writing data to channel", streamId);
                    peerState.ReceiverChannels[streamId].Channel?.WriteAsync(message.Data);
                }
                break;
            case MplexMessageFlag.CloseReceiver:
                if(!peerState.InitiatorChannels.ContainsKey(streamId))
                {
                    _logger?.LogDebug("Stream {streamId} (initiator): Received CloseReceiver for unknown stream. Ignoring", streamId);
                }
                else
                {
                    _logger?.LogDebug("Stream {streamId} (initiator): Received CloseReceiver", streamId);
                    peerState.InitiatorChannels[streamId].Channel?.WriteEofAsync();
                }
                break;
            case MplexMessageFlag.CloseInitiator:
                if(!peerState.ReceiverChannels.ContainsKey(streamId))
                {
                    _logger?.LogDebug("Stream {streamId} (receiver): Received CloseInitiator for unknown stream. Ignoring", streamId);
                }
                else
                {
                    _logger?.LogDebug("Stream {streamId} (receiver): Received CloseInitiator", streamId);
                    peerState.ReceiverChannels[streamId].Channel?.WriteEofAsync();
                }
                break;
            case MplexMessageFlag.ResetReceiver:
                _logger?.LogDebug("Stream {streamId} (initiator): Received ResetReceiver", streamId);
                break;
            case MplexMessageFlag.ResetInitiator:
                _logger?.LogDebug("Stream {streamId} (receiver): Received ResetInitiator", streamId);
                break;
        }
    }
    
    private static async Task<MplexMessage> ReadMessageAsync(IChannel channel)
    {
        var header = await VarInt.DecodeUlong(channel);
        var flag = header & 0x07;
        var streamId = header >> 3;
        var length = await VarInt.DecodeUlong(channel);
        ReadOnlySequence<byte> data = default;

        if (length > 0)
        {
            data = new ReadOnlySequence<byte>((await channel.ReadAsync((int)length).OrThrow()).ToArray());
        }

        return new MplexMessage
        {
            StreamId = (int)streamId,
            Flag = (MplexMessageFlag)flag,
            Data = data
        };
    }

    private async Task WriteMessageAsync(IChannel channel, MplexMessage message)
    {
        try
        {
            var header = (uint)(message.StreamId << 3) | (ulong)message.Flag;
            var headerBytes = new byte[VarInt.GetSizeInBytes(header)];
            var headerOffset = 0;
            VarInt.Encode(header, headerBytes, ref headerOffset);

            var lengthBytes = new byte[VarInt.GetSizeInBytes((ulong)message.Data.Length)];
            var lengthOffset = 0;
            VarInt.Encode((ulong)message.Data.Length, lengthBytes, ref lengthOffset);
            
            await channel.WriteAsync(new ReadOnlySequence<byte>(headerBytes));
            await channel.WriteAsync(new ReadOnlySequence<byte>(lengthBytes));
            await channel.WriteAsync(message.Data);
            
            _logger?.LogDebug("Stream {streamId}: Send flag={flag}, length={length}", message.StreamId, message.Flag, message.Data.Length);
        }
        catch (Exception e)
        {
            _logger?.LogError($"Failed to write message for stream {message.StreamId} with flag {message.Flag}: {e.Message}");
        }
    }
}
