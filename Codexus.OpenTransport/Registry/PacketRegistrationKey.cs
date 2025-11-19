using Codexus.OpenTransport.Packet;

namespace Codexus.OpenTransport.Registry;

public readonly struct PacketRegistrationKey(
    EnumProtocolVersion protocolVersion,
    EnumConnectionState connectionState,
    EnumPacketDirection packetDirection,
    int packetId)
    : IEquatable<PacketRegistrationKey>
{
    // ReSharper disable once MemberCanBePrivate.Global
    public EnumProtocolVersion ProtocolVersion { get; } = protocolVersion;

    // ReSharper disable once MemberCanBePrivate.Global
    public EnumConnectionState ConnectionState { get; } = connectionState;

    // ReSharper disable once MemberCanBePrivate.Global
    public EnumPacketDirection PacketDirection { get; } = packetDirection;

    // ReSharper disable once MemberCanBePrivate.Global
    public int PacketId { get; } = packetId;

    public bool Equals(PacketRegistrationKey other)
    {
        return ProtocolVersion == other.ProtocolVersion &&
               ConnectionState == other.ConnectionState &&
               PacketDirection == other.PacketDirection &&
               PacketId == other.PacketId;
    }

    public override bool Equals(object? obj)
    {
        return obj is PacketRegistrationKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ProtocolVersion, ConnectionState, PacketDirection, PacketId);
    }

    public override string ToString()
    {
        return $"{ProtocolVersion}/{ConnectionState}/{PacketDirection}/{PacketId:X2}";
    }
}