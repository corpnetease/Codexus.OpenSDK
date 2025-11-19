using Codexus.OpenTransport.Packet;

namespace Codexus.ExampleMod.Packet;

// ReSharper disable once ClassNeverInstantiated.Global
public record Property(
    string Name,
    string Value,
    string? Signature
)
{
    public string Name { get; set; } = Name;
    public string Value { get; set; } = Value;
    public string? Signature { get; set; } = Signature;
}

public record S2CLoginSuccess(
    Guid Uuid,
    string Username,
    List<Property> Properties,
    bool StrictErrorHandling
) : IClientBoundPacket
{
    public Guid Uuid { get; set; } = Uuid;
    public string Username { get; set; } = Username;
    public List<Property> Properties { get; set; } = Properties;
    public bool StrictErrorHandling { get; set; } = StrictErrorHandling;
}