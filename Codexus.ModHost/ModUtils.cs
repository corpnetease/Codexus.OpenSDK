namespace Codexus.ModHost;

public static class ModUtils
{
    public static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, dest));
        foreach (var file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(source, dest), true);
    }

    public static string? FindModRoot(string path, string rootStop)
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(path)!);
        while (dir != null && dir.FullName != rootStop)
        {
            if (File.Exists(Path.Combine(dir.FullName, "manifest.json"))) return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }
}