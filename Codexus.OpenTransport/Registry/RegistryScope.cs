namespace Codexus.OpenTransport.Registry;

public class RegistryScope : IDisposable
{
    private readonly List<PacketRegistrationKey> _registeredByIds = [];
    private readonly List<PacketTypeKey> _registeredByTypes = [];
    private readonly List<(Type packetType, PacketHandlerEntry handler)> _registeredHandlers = [];
    private readonly MinecraftRegistry _registry;

    internal RegistryScope(MinecraftRegistry registry)
    {
        _registry = registry;
    }

    public void Dispose()
    {
        Restore();
        GC.SuppressFinalize(this);
    }

    internal void TrackRegistration(PacketRegistrationKey idKey, PacketTypeKey typeKey, bool writeOnly)
    {
        if (!writeOnly)
            _registeredByIds.Add(idKey);
        _registeredByTypes.Add(typeKey);
    }

    internal void TrackHandler(Type packetType, PacketHandlerEntry handler)
    {
        _registeredHandlers.Add((packetType, handler));
    }

    public void Restore()
    {
        foreach (var (packetType, handler) in _registeredHandlers) _registry.RemoveHandler(packetType, handler);

        foreach (var key in _registeredByIds) _registry.RemoveById(key);

        foreach (var key in _registeredByTypes) _registry.RemoveByType(key);

        _registeredByIds.Clear();
        _registeredByTypes.Clear();
        _registeredHandlers.Clear();
    }
}