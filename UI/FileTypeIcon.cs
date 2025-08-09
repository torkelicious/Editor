using System.Text.Json;
using Editor.Core;

namespace Editor.UI;

public static class FileTypeLookup
{
    private static readonly Dictionary<string, FileType?> _fileTypes;

    static FileTypeLookup()
    {
        var resourcePath = Path.Combine(AppContext.BaseDirectory, "resources", "filetypeicons.json");
        if (File.Exists(resourcePath))
            try
            {
                var json = File.ReadAllText(resourcePath);
                var doc = JsonDocument.Parse(json);
                _fileTypes = new Dictionary<string, FileType?>();

                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    var key = property.Name;
                    var value = property.Value;

                    var fileType = new FileType
                    {
                        name = value.GetProperty("name").GetString() ?? string.Empty,
                        icon = value.GetProperty("icon").GetString() ?? string.Empty
                    };

                    _fileTypes[key] = fileType;
                }
            }
            catch
            {
                _fileTypes = new Dictionary<string, FileType?>();
            }
        else
            _fileTypes = new Dictionary<string, FileType?>();
    }

    public static bool TryGet(string key, out FileType? fileType)
    {
        return _fileTypes.TryGetValue(key, out fileType);
    }
}