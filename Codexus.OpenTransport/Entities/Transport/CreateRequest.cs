namespace Codexus.OpenTransport.Entities.Transport;

// ReSharper disable once ClassNeverInstantiated.Global
public class CreateRequest
{
    public required string ServerAddress { get; set; }
    public required int ServerPort { get; set; }
    public required string RoleName { get; set; }
    public required bool Debug { get; set; }

    public int LocalPort { get; set; } = 6445;

    public CreateRequest Clone()
    {
        return (CreateRequest)MemberwiseClone();
    }
}