using Codexus.OpenTransport.Codecs.Stream;
using Codexus.OpenTransport.Packet;
using Codexus.OpenTransport.Packet.Handler;

namespace Codexus.OpenTransport.Registry;

public class RegistryBuilder(MinecraftRegistry registry, RegistryScope? scope = null)
{
    private EnumConnectionState _connectionState;
    private EnumPacketDirection _packetDirection;
    private List<EnumProtocolVersion> _protocolVersions = [];
    private bool _writeOnly;

    public RegistryBuilder ForVersion(EnumProtocolVersion protocolVersion)
    {
        _protocolVersions = [protocolVersion];
        return this;
    }

    public RegistryBuilder ForVersion(params EnumProtocolVersion[] protocolVersions)
    {
        _protocolVersions = protocolVersions.ToList();
        return this;
    }

    public RegistryBuilder ForVersion(IEnumerable<EnumProtocolVersion> protocolVersions)
    {
        _protocolVersions = protocolVersions.ToList();
        return this;
    }

    public RegistryBuilder ForAllVersion()
    {
        _protocolVersions = Enum.GetValues<EnumProtocolVersion>().ToList();
        return this;
    }

    public RegistryBuilder InState(EnumConnectionState connectionState)
    {
        _connectionState = connectionState;
        return this;
    }

    public RegistryBuilder ServerBound()
    {
        _packetDirection = EnumPacketDirection.ServerBound;
        return this;
    }

    public RegistryBuilder ClientBound()
    {
        _packetDirection = EnumPacketDirection.ClientBound;
        return this;
    }

    public RegistryBuilder WriteOnly()
    {
        _writeOnly = true;
        return this;
    }

    public RegistryBuilder ReadWrite()
    {
        _writeOnly = false;
        return this;
    }

    public RegistryBuilder Register<TPacket>(int packetId, IByteBufferCodec<TPacket> codec)
        where TPacket : IPacket
    {
        ValidateState();

        foreach (var version in _protocolVersions)
            registry.Register(version, _connectionState, _packetDirection, packetId, codec, _writeOnly, scope);

        return this;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public RegistryBuilder Register<TPacket>(int[] packetIds, IByteBufferCodec<TPacket> codec)
        where TPacket : IPacket
    {
        ValidateState();

        foreach (var version in _protocolVersions)
        foreach (var packetId in packetIds)
            registry.Register(version, _connectionState, _packetDirection, packetId, codec, _writeOnly, scope);

        return this;
    }

    public RegistryBuilder Register<TPacket>(IEnumerable<int> packetIds, IByteBufferCodec<TPacket> codec)
        where TPacket : IPacket
    {
        return Register(packetIds.ToArray(), codec);
    }

    public RegistryBuilder RegisterBatch(params (int packetId, object codec)[] registrations)
    {
        ValidateState();

        foreach (var version in _protocolVersions)
        foreach (var (packetId, codec) in registrations)
        {
            var codecType = codec.GetType();
            var packetType = codecType.GetGenericArguments()[0];

            var registerMethod = registry.GetType()
                .GetMethod(nameof(MinecraftRegistry.Register))!
                .MakeGenericMethod(packetType);

            registerMethod.Invoke(registry, [
                version,
                _connectionState,
                _packetDirection,
                packetId,
                codec,
                _writeOnly,
                scope
            ]);
        }

        return this;
    }

    public RegistryBuilder Attach<T>(Action<PacketHandlerContext, T> handler, int priority = Priority.Normal)
        where T : IPacket
    {
        registry.Attach(new DelegatePacketHandler<T>(handler), priority, scope);
        return this;
    }

    public void Unpack()
    {
        scope?.Restore();
    }

    private void ValidateState()
    {
        if (_protocolVersions == null || _protocolVersions.Count == 0)
            throw new InvalidOperationException("Protocol version(s) must be set before registering packets");
    }

    public IReadOnlyList<EnumProtocolVersion> GetProtocolVersions()
    {
        return _protocolVersions.AsReadOnly();
    }

    public EnumConnectionState GetConnectionState()
    {
        return _connectionState;
    }

    public EnumPacketDirection GetPacketDirection()
    {
        return _packetDirection;
    }

    public bool GetWriteOnly()
    {
        return _writeOnly;
    }
}