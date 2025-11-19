using Codexus.OpenTransport.Packet;

namespace Codexus.ExampleMod.Packet;

public record C2SLoginStart(string Profile, Guid Uuid) : IServerBoundPacket
{
    public string Profile { get; set; } = Profile;
    public Guid Uuid { get; set; } = Uuid;
}