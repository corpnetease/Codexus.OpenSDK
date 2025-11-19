namespace Codexus.OpenTransport.Packet;

public record PacketWrapper(
    int Id,
    EnumProtocolVersion ProtocolVersion,
    EnumConnectionState State,
    EnumPacketDirection Direction,
    IPacket Packet)
{
}