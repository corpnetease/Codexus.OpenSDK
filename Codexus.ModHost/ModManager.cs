using System.Collections.Concurrent;
using System.Text.Json;
using Codexus.ModHost.Event;
using Codexus.ModSDK;
using NuGet.Versioning;
using Serilog;
using Timer = System.Timers.Timer;

namespace Codexus.ModHost;

public class ModManager
{
    private readonly ConcurrentDictionary<string, RuntimeMod> _activeMods = new();
    private readonly Lock _lock = new();
    private readonly ILogger _logger;
    private readonly string _modsRoot;
    private readonly string _shadowCacheRoot;
    private FileSystemWatcher? _watcher;

    public ModManager(ILogger logger, string rootPath)
    {
        _logger = logger;
        _modsRoot = rootPath;
        _shadowCacheRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModCache");

        if (Directory.Exists(_shadowCacheRoot))
            Directory.Delete(_shadowCacheRoot, true);
    }

    public void Initialize()
    {
        LoadAll();
        StartHotReloadWatcher();
    }

    private void LoadAll()
    {
        lock (_lock)
        {
            var manifests = DiscoverManifests();

            try
            {
                var loadOrder = DependencyResolver.Solve(manifests);
                foreach (var meta in loadOrder) LoadModInternal(meta.Manifest, meta.Path);
            }
            catch (Exception ex)
            {
                throw new Exception($"Dependency resolution failed: {ex.Message}");
            }
        }
    }

    private void LoadModInternal(ModManifest manifest, string originalPath)
    {
        if (_activeMods.ContainsKey(manifest.Id)) return;

        _logger.Information("Loading {ManifestName} ({ManifestId}, {ManifestVersion})...", manifest.Name, manifest.Id,
            manifest.Version);

        var timestamp = DateTime.Now.Ticks.ToString();
        var shadowPath = Path.Combine(_shadowCacheRoot, manifest.Id, timestamp);
        ModUtils.CopyDirectory(originalPath, shadowPath);

        try
        {
            var alc = new ModAssemblyLoadContext(shadowPath);

            var dllPath = Path.Combine(shadowPath, manifest.EntryDll);
            var asm = alc.LoadFromAssemblyPath(dllPath);

            var modType = asm.GetTypes().FirstOrDefault(t => typeof(IMod).IsAssignableFrom(t));
            if (modType == null) throw new Exception("No IMod implementation found.");

            var instance = (IMod)Activator.CreateInstance(modType)!;
            var context = new ContextImpl(_logger, EventBus.Instance, manifest.Name);
            instance.OnLoad(context);

            _activeMods[manifest.Id] = new RuntimeMod(alc, instance, manifest, context, originalPath);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load {ManifestId}: {ExMessage}", manifest.Id, ex.Message);
        }
    }

    private void UnloadMod(string modId)
    {
        if (!_activeMods.TryRemove(modId, out var mod)) return;

        _logger.Information("Unloading {ModId}...", modId);
        try
        {
            mod.Instance.OnUnload();
        }
        catch (Exception ex)
        {
            _logger.Error("Error unloading {ModId}: {Exception}", modId, ex);
        }

        mod.Alc.Unload();
    }

    private void StartHotReloadWatcher()
    {
        _watcher = new FileSystemWatcher(_modsRoot)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite,
            Filter = "*.dll",
            EnableRaisingEvents = true
        };

        var debounceTimer = new Timer(500) { AutoReset = false };
        string? changedPath = null;

        _watcher.Changed += (_, e) =>
        {
            changedPath = e.FullPath;
            debounceTimer.Stop();
            debounceTimer.Start();
        };

        debounceTimer.Elapsed += (_, _) =>
        {
            if (changedPath == null) return;
            var modDir = ModUtils.FindModRoot(changedPath, _modsRoot);
            if (modDir != null) ReloadChain(modDir);
        };
    }

    private void ReloadChain(string modDir)
    {
        lock (_lock)
        {
            var jsonPath = Path.Combine(modDir, "manifest.json");
            if (!File.Exists(jsonPath)) return;

            var newManifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(jsonPath));
            if (newManifest == null) return;

            _logger.Warning("Detected change in {NewManifestName} ({NewManifestId}, {NewManifestVersion})",
                newManifest.Name, newManifest.Id, newManifest.Version);

            var toReload = new HashSet<string> { newManifest.Id };
            bool foundNew;
            do
            {
                foundNew = false;
                foreach (var kvp in _activeMods)
                {
                    if (toReload.Contains(kvp.Key)) continue;

                    if (!kvp.Value.Manifest.Dependencies.Keys.Any(dep => toReload.Contains(dep))) continue;

                    toReload.Add(kvp.Key);
                    foundNew = true;
                }
            } while (foundNew);

            foreach (var id in toReload) UnloadMod(id);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            LoadAll();
        }
    }

    private List<(ModManifest, string)> DiscoverManifests()
    {
        var list = new List<(ModManifest, string)>();
        if (!Directory.Exists(_modsRoot)) Directory.CreateDirectory(_modsRoot);

        foreach (var dir in Directory.GetDirectories(_modsRoot))
        {
            var jsonPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(jsonPath)) continue;

            try
            {
                var m = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(jsonPath));
                if (m != null) list.Add((m, dir));
            }
            catch
            {
                // ignored
            }
        }

        return list;
    }

    private record RuntimeMod(
        ModAssemblyLoadContext Alc,
        IMod Instance,
        ModManifest Manifest,
        // ReSharper disable once NotAccessedPositionalProperty.Local
        IModContext Context,
        // ReSharper disable once NotAccessedPositionalProperty.Local
        string OriginalPath
    );

    private static class DependencyResolver
    {
        public static List<(ModManifest Manifest, string Path)> Solve(List<(ModManifest m, string p)> inputs)
        {
            var sorted = new List<(ModManifest, string)>();
            var visited = new HashSet<string>();
            var processing = new HashSet<string>();
            var map = inputs.ToDictionary(x => x.m.Id, x => x);

            foreach (var item in inputs) Visit(item.m.Id);
            return sorted;

            void Visit(string id)
            {
                if (visited.Contains(id)) return;
                if (!processing.Add(id)) throw new Exception($"Circular dependency detected: {id}");

                if (!map.TryGetValue(id, out var current))
                    throw new Exception($"Missing dependency: {id}");

                foreach (var (targetId, rangeStr) in current.m.Dependencies)
                {
                    if (!map.TryGetValue(targetId, out var target))
                        throw new Exception($"Mod {id} requires missing {targetId}");

                    var targetVersion = NuGetVersion.Parse(target.m.Version);
                    var range = VersionRange.Parse(rangeStr);

                    if (!range.Satisfies(targetVersion))
                        throw new Exception(
                            $"Version conflict: {id} requires {targetId} ({rangeStr}), but found {targetVersion}");

                    Visit(targetId);
                }

                processing.Remove(id);
                visited.Add(id);
                sorted.Add(current);
            }
        }
    }
}