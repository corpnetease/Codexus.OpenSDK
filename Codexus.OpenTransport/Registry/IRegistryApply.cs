namespace Codexus.OpenTransport.Registry;

public interface IRegistryApply
{
    void ApplyTo(MinecraftRegistry registry, RegistryScope scope);
}