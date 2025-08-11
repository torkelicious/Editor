using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.Core;

public static class Config
{
    private const string ConfigFileName = "config.json";
    private static string? ConfigDir { get; set; }
    public static string? ConfigFilePath { get; set; }
    public static ConfigOptions? Options { get; private set; }

    private static void FindConfigDirectory()
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
            if (ConfigDir != null)
                Directory.CreateDirectory(ConfigDir);

        if (File.Exists(ConfigFilePath))
        {
            var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
            Options = JsonSerializer.Deserialize(json, ConfigOptionsJsonContext.Default.ConfigOptions) ??
                      new ConfigOptions();
        }
        else
        {
            Options = new ConfigOptions(); // defaults
            Save();
        }
    }

    private static void Save()
    {
        var json = JsonSerializer.Serialize(Options, ConfigOptionsJsonContext.Default.ConfigOptions);
        if (ConfigFilePath != null) File.WriteAllText(ConfigFilePath, json, Encoding.UTF8);
    }
}

public class ConfigOptions
{
    public bool UseNerdFonts { get; set; }
    public bool EnableDebugMode { get; set; }
    public bool StatusBarShowFileType { get; set; } = true;
}

[JsonSerializable(typeof(ConfigOptions))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class ConfigOptionsJsonContext : JsonSerializerContext
{
}