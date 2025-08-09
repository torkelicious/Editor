using System.Text.Json;

namespace Editor.Core;

public static class Config
{
    private const string ConfigFileName = "config.json";
    public static string ConfigDir { get; private set; }
    public static string ConfigFilePath { get; private set; }
    public static ConfigOptions? Options { get; private set; }

    public static void FindConfigDirectory()
    {
        if (OperatingSystem.IsWindows())
            ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tgent");
        else if (OperatingSystem.IsMacOS())
            ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library",
                "Application Support", "tgent");
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
            ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config",
                "tgent");
        else
            throw new PlatformNotSupportedException("Unknown OS");

        ConfigFilePath = Path.Combine(ConfigDir, ConfigFileName);
    }

    public static void Load()
    {
        FindConfigDirectory();

        if (!Directory.Exists(ConfigDir))
            Directory.CreateDirectory(ConfigDir);

        if (File.Exists(ConfigFilePath))
        {
            var json = File.ReadAllText(ConfigFilePath);
            Options = JsonSerializer.Deserialize<ConfigOptions>(json);
        }
        else
        {
            Options = new ConfigOptions(); // defaults 
            Save();
        }
    }

    public static void Save()
    {
        var json = JsonSerializer.Serialize(Options, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFilePath, json);
    }
}

public class ConfigOptions
{
    public bool UseNerdFonts { get; set; }
    public bool EnableDebugMode { get; set; }
}