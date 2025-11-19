namespace Codexus.OpenTransport.Packet.Handshake;

public record C2SHandshake(
    EnumProtocolVersion ProtocolVersion,
    string ServerAddress,
    ushort ServerPort,
    EnumConnectionState NextState) : IServerBoundPacket
{
    public EnumProtocolVersion ProtocolVersion { get; set; } = ProtocolVersion;
    public string ServerAddress { get; set; } = ServerAddress;
    public ushort ServerPort { get; set; } = ServerPort;
    public EnumConnectionState NextState { get; set; } = NextState;
}