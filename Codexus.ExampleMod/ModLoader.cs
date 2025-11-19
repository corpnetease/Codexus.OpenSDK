using Codexus.ModSDK;
using Codexus.OpenTransport.Event;
using Codexus.OpenTransport.Registry;

namespace Codexus.ExampleMod;

// ReSharper disable once UnusedType.Global
public class ModLoader : IMod
{
    private IModContext? _context;
    private RegistryScope? _scope;

    public void OnLoad(IModContext context)
    {
        _context = context;
        context.EventBus.Subscribe<EventCreateTransport>(HandleTransport);
    }

    public void OnUnload()
    {
        _scope?.Restore();
    }

    private void HandleTransport(EventCreateTransport e)
    {
        _scope = e.Transport.Registry.ApplyRegistry(new ProtocolSupport(_context));
    }
}