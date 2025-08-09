using System.Text.Json;
using Editor.Core;

namespace Editor.UI;

public static class FileTypeLookup
{
    private static readonly Dictionary<string, FileType> _fileTypes;

    static FileTypeLookup()
    {
        string configPath = "filetypes.json"; // Adjust path if needed
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                _fileTypes = JsonSerializer.Deserialize<Dictionary<string, FileType>>(json) 
                             ?? new Dictionary<string, FileType>();
            }
            catch
            {
                _fileTypes = new Dictionary<string, FileType>();
            }
        }
        else
        {
            _fileTypes = new Dictionary<string, FileType>();
        }
    }

    public static bool TryGet(string key, out FileType fileType)
    {
        return _fileTypes.TryGetValue(key, out fileType);
    }
}
