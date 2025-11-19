using System.Collections.Concurrent;
using Codexus.OpenTransport.Codecs.Stream;
using Codexus.OpenTransport.Packet;
using Codexus.OpenTransport.Packet.Handler;

namespace Codexus.OpenTransport.Registry;

public class MinecraftRegistry
{
    private readonly ConcurrentDictionary<PacketRegistrationKey, PacketRegistration> _byId = new();
    private readonly ConcurrentDictionary<PacketTypeKey, PacketRegistration> _byType = new();
    private readonly ConcurrentDictionary<Type, List<PacketHandlerEntry>> _handlers = new();

    public RegistryScope ApplyRegistry(IRegistryApply applier)
    {
        var scope = new RegistryScope(this);
        applier.ApplyTo(this, scope);
        return scope;
    }

    public RegistryBuilder Builder(RegistryScope? scope = null)
    {
        return new RegistryBuilder(this, scope);
    }

    public MinecraftRegistry Attach<TPacket>(IPacketHandler<TPacket> handler, int priority = 0,
        RegistryScope? scope = null)
        where TPacket : IPacket
    {
        ArgumentNullException.ThrowIfNull(handler);

        var packetType = typeof(TPacket);
        var packetHandler = new PacketHandlerEntry
        {
            Priority = priority,
            Handler = (ctx, pkt) => handler.Handle(ctx, (TPacket)pkt)
        };

        _handlers.AddOrUpdate(
            packetType,
            _ => [packetHandler],
            (_, existingList) =>
            {
                existingList.Add(packetHandler);
                existingList.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                return existingList;
            }
        );

        scope?.TrackHandler(packetType, packetHandler);

        return this;
    }

    internal void RemoveHandler(Type packetType, PacketHandlerEntry handler)
    {
        if (!_handlers.TryGetValue(packetType, out var handlers)) return;

        handlers.Remove(handler);
        if (handlers.Count == 0) _handlers.TryRemove(packetType, out _);
    }

    public EnumPacketHandleResult BroadcastHandler(PacketHandlerContext context, IPacket packet)
    {
        var packetType = packet.GetType();

        if (!_handlers.TryGetValue(packetType, out var handlers)) return EnumPacketHandleResult.NoHandler;

        foreach (var handler in handlers)
        {
            handler.Handler(context, packet);

            if (!context.IsPropagation) break;
        }

        return context.IsCancel ? EnumPacketHandleResult.Cancelled : EnumPacketHandleResult.Completed;
    }

    public MinecraftRegistry Register<TPacket>(
        EnumProtocolVersion protocolVersion,
        EnumConnectionState connectionState,
        EnumPacketDirection packetDirection,
        int packetId,
        IByteBufferCodec<TPacket> codec,
        bool writeOnly = false,
        RegistryScope? scope = null) where TPacket : IPacket
    {
        var registration = new PacketRegistration
        {
            PacketId = packetId,
            PacketType = typeof(TPacket),
            Codec = codec,
            ProtocolVersion = protocolVersion,
            ConnectionState = connectionState,
            PacketDirection = packetDirection,
            WriteOnly = writeOnly
        };

        var idKey = new PacketRegistrationKey(
            protocolVersion,
            connectionState,
            packetDirection,
            packetId);

        var typeKey = new PacketTypeKey(
            protocolVersion,
            connectionState,
            packetDirection,
            typeof(TPacket));

        if (!writeOnly)
            if (!_byId.TryAdd(idKey, registration))
                throw new InvalidOperationException(
                    $"Packet ID {packetId:X2} already registered for {idKey}");

        if (_byType.TryAdd(typeKey, registration))
        {
            scope?.TrackRegistration(idKey, typeKey, writeOnly);
            return this;
        }

        if (!writeOnly) _byId.TryRemove(idKey, out _);

        throw new InvalidOperationException(
            $"Packet type {typeof(TPacket).Name} already registered for {typeKey}");
    }

    internal void RemoveById(PacketRegistrationKey key)
    {
        _byId.TryRemove(key, out _);
    }

    internal void RemoveByType(PacketTypeKey key)
    {
        _byType.TryRemove(key, out _);
    }

    public MinecraftRegistry RegisterServerBound<TPacket>(
        EnumProtocolVersion protocolVersion,
        EnumConnectionState connectionState,
        int packetId,
        IByteBufferCodec<TPacket> codec,
        bool writeOnly = false,
        RegistryScope? scope = null) where TPacket : IServerBoundPacket
    {
        return Register(protocolVersion, connectionState, EnumPacketDirection.ServerBound, packetId, codec, writeOnly,
            scope);
    }

    public MinecraftRegistry RegisterClientBound<TPacket>(
        EnumProtocolVersion protocolVersion,
        EnumConnectionState connectionState,
        int packetId,
        IByteBufferCodec<TPacket> codec,
        bool writeOnly = false,
        RegistryScope? scope = null) where TPacket : IClientBoundPacket
    {
        return Register(protocolVersion, connectionState, EnumPacketDirection.ClientBound, packetId, codec, writeOnly,
            scope);
    }

    public IByteBufferCodec<TPacket>? GetCodecById<TPacket>(
        EnumProtocolVersion protocolVersion,
        EnumConnectionState connectionState,
        EnumPacketDirection packetDirection,
        int packetId) where TPacket : IPacket
    {
        var key = new PacketRegistrationKey(protocolVersion, connectionState, packetDirection, packetId);

        if (_byId.TryGetValue(key, out var registration)) return registration.Codec as IByteBufferCodec<TPacket>;

        return null;
    }

    public object? GetCodecById(
        EnumProtocolVersion protocolVersion,
        EnumConnectionState connectionState,
        EnumPacketDirection packetDirection,
        int packetId)
    {
        var key = new PacketRegistrationKey(protocolVersion, connectionState, packetDirection, packetId);

        return _byId.TryGetValue(key, out var registration) ? registration.Codec : null;
    }

    public IByteBufferCodec<TPacket>? GetCodecByType<TPacket>(
        EnumProtocolVersion protocolVersion,
        EnumConnectionState connectionState,
        EnumPacketDirection packetDirection) where TPacket : IPacket
    {
        var key = new PacketTypeKey(protocolVersion, connectionState, packetDirection, typeof(TPacket));

        if (_byType.TryGetValue(key, out var registration)) return registration.Codec as IByteBufferCodec<TPacket>;

        return null;
    }

    public IByteBufferCodecBase? GetCodecByType(
        EnumProtocolVersion protocolVersion,
        EnumConnectionState connectionState,
        EnumPacketDirection packetDirection,
        Type packetType)
    {
        var key = new PacketTypeKey(protocolVersion, connectionState, packetDirection, packetType);

        if (_byType.TryGetValue(key, out var registration)) return registration.Codec as IByteBufferCodecBase;

        return null;
    }

    public int? GetPacketId<TPacket>(
        EnumProtocolVersion protocolVersion,
        EnumConnectionState connectionState,
        EnumPacketDirection packetDirection) where TPacket : IPacket
    {
        var key = new PacketTypeKey(protocolVersion, connectionState, packetDirection, typeof(TPacket));

        if (_byType.TryGetValue(key, out var registration)) return registration.PacketId;

        return null;
    }

    public int? GetPacketId(
        EnumProtocolVersion protocolVersion,
        EnumConnectionState connectionState,
        EnumPacketDirection packetDirection,
        Type packetType)
    {
        var key = new PacketTypeKey(protocolVersion, connectionState, packetDirection, packetType);

        if (_byType.TryGetValue(key, out var registration)) return registration.PacketId;

        return null;
    }

    public Type? GetPacketType(
        EnumProtocolVersion protocolVersion,
        EnumConnectionState connectionState,
        EnumPacketDirection packetDirection,
        int packetId)
    {
        var key = new PacketRegistrationKey(protocolVersion, connectionState, packetDirection, packetId);

        return _byId.TryGetValue(key, out var registration) ? registration.PacketType : null;
    }

    public bool IsRegistered(
        EnumProtocolVersion protocolVersion,
        EnumConnectionState connectionState,
        EnumPacketDirection packetDirection,
        int packetId)
    {
        var key = new PacketRegistrationKey(protocolVersion, connectionState, packetDirection, packetId);
        return _byId.ContainsKey(key);
    }

    public IEnumerable<PacketRegistration> GetRegistrations(
        EnumProtocolVersion? protocolVersion = null,
        EnumConnectionState? connectionState = null,
        EnumPacketDirection? packetDirection = null,
        bool? writeOnly = null)
    {
        return _byId.Values
            .Concat(_byType.Values.Where(r => r.WriteOnly))
            .Distinct()
            .Where(reg =>
                (!protocolVersion.HasValue || reg.ProtocolVersion == protocolVersion.Value) &&
                (!connectionState.HasValue || reg.ConnectionState == connectionState.Value) &&
                (!packetDirection.HasValue || reg.PacketDirection == packetDirection.Value) &&
                (!writeOnly.HasValue || reg.WriteOnly == writeOnly.Value));
    }

    public void Clear()
    {
        _byId.Clear();
        _byType.Clear();
    }
}