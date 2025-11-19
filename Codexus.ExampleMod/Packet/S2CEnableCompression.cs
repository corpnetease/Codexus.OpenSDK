using Codexus.OpenTransport.Packet;

namespace Codexus.ExampleMod.Packet;

public record S2CEnableCompression(
    int CompressionThreshold
) : IClientBoundPacket
{
    public int CompressionThreshold { get; set; } = CompressionThreshold;
}