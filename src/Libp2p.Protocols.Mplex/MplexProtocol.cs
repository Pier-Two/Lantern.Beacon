using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using System.Buffers;
using System.Collections.Concurrent;
using Nethermind.Libp2p.Core.Exceptions;

namespace Libp2p.Protocols.Mplex;

public class MplexProtocol(ILoggerFactory? loggerFactory = null) : SymmetricProtocol, IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<MplexProtocol>();
    private readonly ConcurrentDictionary<int, ChannelState> _channels = new();
    private int _streamIdCounter;

    public string Id => "/mplex/6.7.0";

    protected override async Task ConnectAsync(IChannel channel, IChannelFactory? channelFactory, IPeerContext context, bool isListener)
    {
        if (channelFactory is null)
        {
            throw new ArgumentException("ChannelFactory should be available for a muxer", nameof(channelFactory));
        }

        _logger?.LogInformation(isListener ? "Listen" : "Dial");

        var downChannelAwaiter = channel.GetAwaiter();
        context.Connected(context.RemotePeer);
        _ = Task.Run(() => HandleSubDialRequests(context, channelFactory, isListener, channel));

        try
        {
            while (!downChannelAwaiter.IsCompleted)
            {
                var message = await ReadMessageAsync(channel);
                await HandleMessageAsync(message, channel, channelFactory, context);
            }
        }
        catch (ChannelClosedException ex)
        {
            _logger?.LogDebug("Closed due to transport disconnection: {exception}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Closed with exception {exception}", ex.Message);
            _logger?.LogTrace("{stackTrace}", ex.StackTrace);
        }

        _logger?.LogDebug("Closing all channels");
        foreach (var upChannel in _channels.Values)
        {
            _ = upChannel.Channel?.CloseAsync();
        }

        _channels.Clear();
    }

    private void HandleSubDialRequests(IPeerContext context, IChannelFactory channelFactory, bool isListener, IChannel channel)
    {
        foreach (var request in context.SubDialRequests.GetConsumingEnumerable())
        {
            var streamId = Interlocked.Increment(ref _streamIdCounter);
            
            _logger?.LogDebug("Handling sub dial request for protocol {protocol}", request.SubProtocol?.Id);
            var channelState = CreateUpChannel(streamId, MplexMessageFlag.NewStream, request, channelFactory, isListener, context, channel);
            _channels.TryAdd(streamId, channelState);
        }
    }

    private ChannelState CreateUpChannel(int streamId, MplexMessageFlag initiationFlag, IChannelRequest? channelRequest, IChannelFactory channelFactory, bool isListener, IPeerContext context, IChannel channel)
    {
        _logger?.LogDebug("Creating upChannel for stream {streamId} as {isListener}", streamId, isListener ? "listener" : "dialer");
        IChannel upChannel;

        if (isListener)
        {
            _logger?.LogDebug("Stream {streamId}: Listening in new stream", streamId);
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
            _logger?.LogDebug("Stream {stream id}: Preparing to close stream", streamId);
            _channels.TryRemove(streamId, out _); 
            Interlocked.Decrement(ref _streamIdCounter);
        });

        // Initiate background processing of the channel
        _ = Task.Run(() => ProcessChannelAsync(channel, streamId, initiationFlag, channelRequest, upChannel, isListener));

        return state;
    }

    private async Task ProcessChannelAsync(IChannel channel, int streamId, MplexMessageFlag initiationFlag, IChannelRequest? channelRequest, IChannel upChannel, bool isListenerChannel)
    {
        try
        {
            _logger?.LogDebug("Stream {streamId}: Processing channel with flag={initiationFlag} as {isListener}", streamId, initiationFlag, isListenerChannel ? "listener" : "dialer");
            
            if (initiationFlag == MplexMessageFlag.NewStream)
            {
                var streamName = channelRequest?.SubProtocol?.Id ?? string.Empty;
                var streamNameBytes = System.Text.Encoding.UTF8.GetBytes(streamName);
                
                _logger?.LogDebug("Stream {streamId}: Creating new stream with name {streamName} as {isListener} ", streamId, streamName, isListenerChannel ? "listener" : "dialer");
                await WriteMessageAsync(channel, new MplexMessage
                {
                    Flag = initiationFlag,
                    StreamId = streamId,
                    Data = new ReadOnlySequence<byte>(streamNameBytes)
                });
                
                await foreach (var upData in upChannel.ReadAllAsync())
                {
                    _logger?.LogDebug("Stream {streamId}: Received data from upChannel as {isListener}, length={length}. Writing {data} to stream", streamId, isListenerChannel ? "listener" : "dialer", upData.Length, Convert.ToHexString(upData.ToArray()));
                    await WriteMessageAsync(channel, new MplexMessage
                    {
                        Flag = MplexMessageFlag.MessageInitiator,
                        StreamId = streamId,
                        Data = upData
                    });
                }
            }
            else if (initiationFlag == MplexMessageFlag.MessageReceiver)
            {
                await foreach (var upData in upChannel.ReadAllAsync())
                {
                    _logger?.LogDebug("Stream {streamId}: Received data from upChannel as {isListener}, length={length}, data={data}", streamId, isListenerChannel ? "listener" : "dialer", upData.Length, Convert.ToHexString(upData.ToArray()));
                    await WriteMessageAsync(channel, new MplexMessage
                    {
                        Flag = MplexMessageFlag.MessageReceiver,
                        StreamId = streamId,
                        Data = upData
                    });
                }
            }
        }
        catch (Exception e)
        {
            _logger?.LogDebug("Stream {streamId}: Unexpected error: {error}", streamId, e.Message);
        }
    }
    
    private async Task HandleMessageAsync(MplexMessage message, IChannel channel, IChannelFactory? channelFactory, IPeerContext context)
    {
        if (channelFactory is null)
        {
            throw new ArgumentException("ChannelFactory should be available for a muxer", nameof(channelFactory));
        }

        var streamId = message.StreamId;
        var flag = message.Flag;

        _logger?.LogDebug("Decoded received message flag={flag}, streamId={streamId}, len={len}, data={data}", flag, streamId, message.Data.Length, Convert.ToHexString(message.Data.ToArray()));
        
        // If this flag is for a new stream and the stream does not already exist, create a new stream channel
        if (flag == MplexMessageFlag.NewStream)
        {
            if (!_channels.ContainsKey(streamId))
            {
                 _logger?.LogDebug("Stream {streamId}: Opening new stream", streamId);
                 var newChannelState = CreateUpChannel(streamId, MplexMessageFlag.MessageReceiver, new ChannelRequest(), channelFactory, true, context, channel);
                 _channels.TryAdd(streamId, newChannelState);
            }
            else
            {
                _logger?.LogDebug("Received a new stream request for existing stream {streamId}. Ignoring", streamId);
            }
        }

        // Try to get the existing stream channel
        if (!_channels.TryGetValue(streamId, out var channelState))
        {
            if (message.Data.Length > 0)
            {
                _logger?.LogDebug("Stream {streamId}: Drain the data if stream not found", streamId);
                _ = channel.ReadAsync((int)message.Data.Length); // Drain the data if stream not found
            }

            _logger?.LogDebug("Received a message for unknown stream {streamId}. Ignoring", streamId);
            return;
        }

        switch (flag)
        {
            case MplexMessageFlag.MessageReceiver:
                _logger?.LogDebug("Stream {streamId}: Received MessageReceiver. Writing data to channel for protocol {protocol}", streamId, channelState.Request.SubProtocol?.Id);
                _channels[streamId].Channel?.WriteAsync(message.Data);
                _logger?.LogDebug("Stream {streamId}: Wrote data to channel", streamId);
                break;
            case MplexMessageFlag.MessageInitiator:
                _logger?.LogDebug("Stream {streamId}: Received MessageInitiator. Writing data to channel for protocol {protocol} ", streamId, channelState.Request.SubProtocol?.Id);
                _channels[streamId].Channel?.WriteAsync(message.Data);
                break;
            case MplexMessageFlag.CloseReceiver:
                _logger?.LogDebug("Stream {streamId}: Received CloseReceiver", streamId);
                _channels[streamId].Channel?.WriteEofAsync();
                break;
            case MplexMessageFlag.CloseInitiator:
                _logger?.LogDebug("Stream {streamId}: Received CloseInitiator", streamId);
                break;
            case MplexMessageFlag.ResetReceiver:
                _logger?.LogDebug("Stream {streamId}: Received ResetReceiver", streamId);
                break;
            case MplexMessageFlag.ResetInitiator:
                _logger?.LogDebug("Stream {streamId}: Received ResetInitiator. Draining the data to reset", streamId);
                _ = channel.ReadAsync((int)message.Data.Length); // Drain the data 
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
            
            _logger?.LogDebug("Stream {streamId}: Send flag={flag}, length={length}, data={data}", message.StreamId, message.Flag, message.Data.Length, Convert.ToHexString(message.Data.ToArray()));
        }
        catch (Exception e)
        {
            _logger?.LogError($"Failed to write message for stream {message.StreamId} with flag {message.Flag}: {e.Message}");
        }
    }
}
