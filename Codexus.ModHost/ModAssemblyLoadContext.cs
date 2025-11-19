using System.Reflection;
using System.Runtime.Loader;

namespace Codexus.ModHost;

public class ModAssemblyLoadContext(string modPath) : AssemblyLoadContext(true)
{
    private readonly AssemblyDependencyResolver _resolver = new(modPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null) return LoadFromAssemblyPath(assemblyPath);

        var localDll = Path.Combine(modPath, assemblyName.Name + ".dll");
        return File.Exists(localDll) ? LoadFromAssemblyPath(localDll) : null;
    }
}