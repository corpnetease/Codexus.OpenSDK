using Codexus.OpenTransport.Packet;

namespace Codexus.OpenTransport.Registry;

public class PacketRegistration
{
    public required int PacketId { get; init; }

    public required Type PacketType { get; init; }

    public required object Codec { get; init; }

    public required EnumProtocolVersion ProtocolVersion { get; init; }

    public required EnumConnectionState ConnectionState { get; init; }

    public required EnumPacketDirection PacketDirection { get; init; }

    public bool WriteOnly { get; init; }
}