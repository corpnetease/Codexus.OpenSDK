using Codexus.OpenTransport.Registry;

namespace Codexus.OpenTransport.Extensions;

public static class MinecraftRegistryExtensions
{
    public static RegistryBuilder Builder(this MinecraftRegistry registry)
    {
        return new RegistryBuilder(registry);
    }
}