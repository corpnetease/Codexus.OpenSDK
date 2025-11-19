using System.Net;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenTransport.Codecs.Netty;
using Codexus.OpenTransport.Codecs.Stream;
using Codexus.OpenTransport.Entities.Transport;
using Codexus.OpenTransport.Extensions;
using Codexus.OpenTransport.NettyHandler;
using Codexus.OpenTransport.Packet;
using Codexus.OpenTransport.Packet.Handler;
using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;

namespace Codexus.OpenTransport.Session;

public class NetworkSession(OpenTransport transport, IChannel local)
{
    private readonly SemaphoreSlim _clientPacketSemaphore = new(1, 1);
    private readonly ILogger? _logger = transport.Logger;
    private readonly SemaphoreSlim _serverPacketSemaphore = new(1, 1);

    // ReSharper disable once MemberCanBePrivate.Global
    public readonly IChannel Local = local;
    private bool _initialized;
    private MultithreadEventLoopGroup? _workerGroup;

    // ReSharper disable once MemberCanBePrivate.Global
    public IChannel? Remote;

    // ReSharper disable once MemberCanBePrivate.Global
    public EnumProtocolVersion ProtocolVersion { get; private set; } = EnumProtocolVersion.None;

    // ReSharper disable once MemberCanBePrivate.Global
    public EnumConnectionState State { get; private set; } = EnumConnectionState.Handshake;

    // ReSharper disable once MemberCanBePrivate.Global
    public GameProfile Profile { get; init; } = transport.Profile.Clone();

    // ReSharper disable once MemberCanBePrivate.Global
    public CreateRequest Request { get; init; } = transport.Request.Clone();

    // ReSharper disable once MemberCanBePrivate.Global
    public OpenTransport Transport => transport;

    private async Task OnAddAsync()
    {
        if (_initialized) return;

        _workerGroup = new MultithreadEventLoopGroup();

        var bootstrap = new Bootstrap()
            .Group(_workerGroup)
            .Channel<TcpSocketChannel>()
            .Option(ChannelOption.TcpNodelay, true)
            .Option(ChannelOption.SoKeepalive, true)
            .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
            .Option(ChannelOption.SoSndbuf, 1048576)
            .Option(ChannelOption.SoRcvbuf, 1048576)
            .Option(ChannelOption.WriteBufferHighWaterMark, 1048576)
            .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(30.0))
            .Handler(new ActionChannelInitializer<IChannel>(channel =>
            {
                channel.Pipeline
                    .AddLast("splitter", new MessageDeserializer21Bit())
                    .AddLast("handler", new ServerInboundMessageHandler(this))
                    .AddLast("pre-encoder", new MessageSerializer21Bit())
                    .AddLast("encoder", new MessageSerializer());
            }));

        try
        {
            if (IPAddress.TryParse(Request.ServerAddress, out var address))
                Remote = await bootstrap.ConnectAsync(address, Request.ServerPort).ConfigureAwait(false);
            else
                Remote = await bootstrap.ConnectAsync(Request.ServerAddress, Request.ServerPort).ConfigureAwait(false);

            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Failed to connect to {Address}:{Port}", Request.ServerAddress, Request.ServerPort);
            throw new Exception($"Cannot connect to: {Request.ServerAddress}:{Request.ServerPort}", ex);
        }

        if (Remote == null) throw new Exception($"Cannot connect to: {Request.ServerAddress}:{Request.ServerPort}");
    }

    public void OnAdd()
    {
        Task.Run(async () => await OnAddAsync().ConfigureAwait(false))
            .GetAwaiter()
            .GetResult();
    }

    private async Task OnRemoveAsync()
    {
        if (Remote is { Active: true })
        {
            await Remote.CloseAsync().ConfigureAwait(false);
            Remote = null;
        }

        if (Local.Active) await Local.CloseAsync().ConfigureAwait(false);

        if (_workerGroup != null)
        {
            await _workerGroup.ShutdownGracefullyAsync(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(5)
            ).ConfigureAwait(false);
            _workerGroup = null;
        }

        _clientPacketSemaphore.Dispose();
        _serverPacketSemaphore.Dispose();

        _initialized = false;
    }

    public void OnRemove()
    {
        Task.Run(async () => await OnRemoveAsync().ConfigureAwait(false))
            .GetAwaiter()
            .GetResult();
    }

    private async Task HandlePacketReceivedAsync(IByteBuffer buffer, EnumPacketDirection direction,
        Func<object, Func<Task>?, Task> onRedirect)
    {
        buffer.MarkReaderIndex();

        var id = buffer.ReadVarInt();
        var codec = transport.Registry.GetCodecById(ProtocolVersion, State, direction, id) as IByteBufferCodecBase;

        if (transport.Request.Debug)
            _logger?.Debug(
                "Redirect direction: {Direction}, Id: {Id}, Codec: {Packet}, ProtocolVersion: {ProtocolVersion}, State: {State}",
                direction, id, codec, ProtocolVersion, State);

        if (codec == null)
        {
            buffer.ResetReaderIndex();
            await onRedirect(buffer, null).ConfigureAwait(false);
            return;
        }

        try
        {
            if (codec.Decode(buffer) is not IPacket packet)
            {
                _logger?.Warning(
                    "Decoded packet is null for ID {Id:X2}, Direction: {Direction}",
                    id, direction);

                buffer.ResetReaderIndex();
                await onRedirect(buffer, null).ConfigureAwait(false);
                return;
            }

            var wrapper = new PacketWrapper(id, ProtocolVersion, State, direction, packet);

            if (buffer.ReadableBytes > 0)
                _logger?.Warning(
                    "Packet {PacketType} (ID: {Id:X2}) has {UnreadBytes} unread bytes",
                    packet.GetType().Name, id, buffer.ReadableBytes);

            var context = new PacketHandlerContext { Session = this, Transport = transport };
            var result = transport.Registry.BroadcastHandler(context, packet);

            var hasPendingActions = context.SendAfterActions.Count > 0;

            if (result == EnumPacketHandleResult.Cancelled)
            {
                if (hasPendingActions)
                    await Task.Run(async () =>
                    {
                        foreach (var action in context.SendAfterActions)
                            try
                            {
                                await action().ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _logger?.Error(ex, "Error executing SendAfterAction after cancelled packet");
                            }
                    }).ConfigureAwait(false);

                return;
            }

            if (hasPendingActions)
                await onRedirect(wrapper, async () =>
                {
                    await Task.Run(async () =>
                    {
                        foreach (var action in context.SendAfterActions)
                            try
                            {
                                await action().ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _logger?.Error(ex, "Error executing SendAfterAction");
                            }
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);
            else
                await onRedirect(wrapper, null).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.Error(ex,
                "Fatal error in HandlePacketReceivedAsync, Direction: {Direction}, State: {State}",
                direction, State);

            buffer.ResetReaderIndex();
            await onRedirect(buffer, null).ConfigureAwait(false);
        }
    }

    public async void OnClientReceived(IByteBuffer buffer)
    {
        try
        {
            await _clientPacketSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                await HandlePacketReceivedAsync(buffer, EnumPacketDirection.ServerBound, async (data, after) =>
                {
                    if (Remote != null) await Remote.WriteAndFlushAsync(data).ConfigureAwait(false);

                    if (after != null) await after().ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            finally
            {
                _clientPacketSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Error in OnClientReceived");
        }
    }

    public async void OnServerReceived(IByteBuffer buffer)
    {
        try
        {
            await _serverPacketSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                await HandlePacketReceivedAsync(buffer, EnumPacketDirection.ClientBound, async (data, after) =>
                {
                    await Local.WriteAndFlushAsync(data).ConfigureAwait(false);

                    if (after != null) await after().ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            finally
            {
                _serverPacketSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Error in OnServerReceived");
        }
    }

    public void SetProtocolVersion(int version)
    {
        SetProtocolVersion((EnumProtocolVersion)version);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public void SetProtocolVersion(EnumProtocolVersion version)
    {
        ProtocolVersion = version;
    }

    public void SetState(int state)
    {
        SetState((EnumConnectionState)state);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public void SetState(EnumConnectionState state)
    {
        State = state;
    }

    public static void EnableCompression(IChannel channel, int threshold)
    {
        if (threshold < 0)
        {
            if (channel.Pipeline.Get("decompress") is NettyCompressionDecoder) channel.Pipeline.Remove("decompress");

            if (channel.Pipeline.Get("compress") is NettyCompressionEncoder) channel.Pipeline.Remove("compress");

            return;
        }

        if (channel.Pipeline.Get("decompress") is NettyCompressionDecoder packetDecompressor)
            packetDecompressor.Threshold = threshold;
        else
            channel.Pipeline.AddAfter("splitter", "decompress", new NettyCompressionDecoder(threshold));

        if (channel.Pipeline.Get("compress") is NettyCompressionEncoder packetCompressor)
        {
            packetCompressor.Threshold = threshold;
            return;
        }

        channel.Pipeline.AddBefore("encoder", "compress", new NettyCompressionEncoder(threshold));
    }

    public static void EnableEncryption(IChannel channel, byte[] secretKey)
    {
        channel.Pipeline.AddBefore("splitter", "decrypt", new NettyEncryptionDecoder(secretKey))
            .AddBefore("pre-encoder", "encrypt", new NettyEncryptionEncoder(secretKey));
    }
}