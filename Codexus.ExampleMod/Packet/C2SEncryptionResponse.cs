using Codexus.OpenTransport.Packet;

namespace Codexus.ExampleMod.Packet;

public record C2SEncryptionResponse(
    byte[] SecretKeyEncrypted,
    byte[] VerifyTokenEncrypted
) : IServerBoundPacket
{
    public byte[] SecretKeyEncrypted { get; set; } = SecretKeyEncrypted;
    public byte[] VerifyTokenEncrypted { get; set; } = VerifyTokenEncrypted;
}