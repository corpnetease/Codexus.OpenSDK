using Codexus.OpenTransport.Packet;

namespace Codexus.OpenTransport.Registry;

public readonly struct PacketTypeKey(
    EnumProtocolVersion protocolVersion,
    EnumConnectionState connectionState,
    EnumPacketDirection packetDirection,
    Type packetType)
    : IEquatable<PacketTypeKey>
{
    // ReSharper disable once MemberCanBePrivate.Global
    public EnumProtocolVersion ProtocolVersion { get; } = protocolVersion;

    // ReSharper disable once MemberCanBePrivate.Global
    public EnumConnectionState ConnectionState { get; } = connectionState;

    // ReSharper disable once MemberCanBePrivate.Global
    public EnumPacketDirection PacketDirection { get; } = packetDirection;

    // ReSharper disable once MemberCanBePrivate.Global
    public Type PacketType { get; } = packetType;

    public bool Equals(PacketTypeKey other)
    {
        return ProtocolVersion == other.ProtocolVersion &&
               ConnectionState == other.ConnectionState &&
               PacketDirection == other.PacketDirection &&
               PacketType == other.PacketType;
    }

    public override bool Equals(object? obj)
    {
        return obj is PacketTypeKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ProtocolVersion, ConnectionState, PacketDirection, PacketType);
    }
}