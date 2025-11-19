using System.Collections.Concurrent;
using System.Net;
using Codexus.ModHost.Event;
using Codexus.OpenSDK.Entities;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenTransport.Codecs.Netty;
using Codexus.OpenTransport.Entities.Transport;
using Codexus.OpenTransport.Event;
using Codexus.OpenTransport.Extensions;
using Codexus.OpenTransport.NettyHandler;
using Codexus.OpenTransport.Packet.Protocol;
using Codexus.OpenTransport.Registry;
using Codexus.OpenTransport.Session;
using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;

namespace Codexus.OpenTransport;

public class OpenTransport
{
    private readonly ConcurrentDictionary<IChannelId, IChannel> _activeChannels = new();
    private readonly ConcurrentDictionary<IChannelId, NetworkSession> _activeSessions = new();
    private readonly RegistryScope _registryScope;

    private bool _isDisposed;
    private bool _isRunning;
    private IChannel? _serverChannel;

    private OpenTransport()
    {
        _registryScope = Registry.ApplyRegistry(new MinecraftBase());
    }

    public required GameProfile Profile { get; init; }
    public required CreateRequest Request { get; init; }
    public required MultithreadEventLoopGroup BossGroup { get; init; }
    public required MultithreadEventLoopGroup WorkerGroup { get; init; }
    public required ILogger? Logger { get; init; }
    public MinecraftRegistry Registry { get; } = new();

    public static OpenTransport Create(GameProfile profile, CreateRequest request, ILogger? logger = null)
    {
        return EventBus.Instance.Publish(new EventCreateTransport(new OpenTransport
        {
            Profile = profile,
            Request = request,
            BossGroup = new MultithreadEventLoopGroup(1),
            WorkerGroup = new MultithreadEventLoopGroup(),
            Logger = logger
        })).Transport;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public async Task<Result> StartAsync()
    {
        if (_isRunning)
            return Result.Failure("Already running");

        if (_isDisposed)
            return Result.Failure(nameof(OpenTransport) + " is already disposed");

        try
        {
            var freePort = Request.LocalPort.FindFreePort(50);
            if (freePort.IsFailure)
                return Result.Clone(freePort);

            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(BossGroup, WorkerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                .Option(ChannelOption.SoSndbuf, 1048576)
                .Option(ChannelOption.SoRcvbuf, 1048576)
                .Option(ChannelOption.SoBacklog, 128)
                .Option(ChannelOption.SoReuseaddr, true)
                .Option(ChannelOption.SoReuseport, true)
                .Option(ChannelOption.TcpNodelay, true)
                .Option(ChannelOption.SoKeepalive, true)
                .Option(ChannelOption.WriteBufferHighWaterMark, 1048576)
                .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(10.0))
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    channel.Pipeline
                        .AddLast("splitter", new MessageDeserializer21Bit())
                        .AddLast("handler", new ClientInboundMessageHandler(this))
                        .AddLast("pre-encoder", new MessageSerializer21Bit())
                        .AddLast("encoder", new MessageSerializer());
                }))
                .LocalAddress(IPAddress.Any, freePort.Value);

            await bootstrap.BindAsync().ContinueWith(channel => { _serverChannel = channel.Result; });
            _isRunning = true;

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("Error: " + ex.Message);
        }
    }

    public void Start()
    {
        StartAsync().GetAwaiter().GetResult();
    }

    public async Task WaitForShutdownAsync()
    {
        if (_serverChannel != null) await _serverChannel.CloseCompletion;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public async Task StopAsync()
    {
        if (!_isRunning) return;

        try
        {
            if (_serverChannel != null) await _serverChannel.CloseAsync();
            
            foreach (var channel in _activeChannels.Values)
            {
                await channel.CloseAsync();
            }
        }
        finally
        {
            _isRunning = false;
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public async Task CloseAsync()
    {
        if (_isDisposed) return;

        try
        {
            await StopAsync();

            await WorkerGroup.ShutdownGracefullyAsync(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(5));

            await BossGroup.ShutdownGracefullyAsync(
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(5));

            _registryScope.Dispose();
        }
        finally
        {
            _isDisposed = true;
        }
    }

    public void Close()
    {
        CloseAsync().GetAwaiter().GetResult();
    }

    public void AddConnection(IChannel channel, NetworkSession session)
    {
        _activeChannels.TryAdd(channel.Id, channel);
        _activeSessions.TryAdd(channel.Id, session);

        channel.SetSession(session);
        session.OnAdd();
    }

    public void RemoveConnection(IChannel channel)
    {
        _activeChannels.TryRemove(channel.Id, out _);
        _activeSessions.TryRemove(channel.Id, out _);

        channel.RemoveSession()?.OnRemove();
    }
}