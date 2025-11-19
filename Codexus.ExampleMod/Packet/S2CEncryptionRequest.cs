using Codexus.OpenTransport.Packet;

namespace Codexus.ExampleMod.Packet;

public record S2CEncryptionRequest(
    string ServerId,
    byte[] PublicKey,
    byte[] VerifyToken,
    bool ShouldAuthenticate
) : IClientBoundPacket
{
    public string ServerId { get; set; } = ServerId;
    public byte[] PublicKey { get; set; } = PublicKey;
    public byte[] VerifyToken { get; set; } = VerifyToken;
    public bool ShouldAuthenticate { get; set; } = ShouldAuthenticate;
}