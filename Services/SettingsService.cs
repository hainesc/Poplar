using System.IO;
using System.Text.Json;
using Poplar.Models;

namespace Poplar.Services;

public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    public AppSettings Settings { get; private set; } = new();

    public SettingsService()
    {
        Load();
    }

    public void Load()
    {
        if (File.Exists(SettingsPath))
        {
            try
            {
                var content = File.ReadAllText(SettingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(content) ?? new();
            }
            catch
            {
                Settings = new();
            }
        }
    }

    public void Save()
    {
        var content = JsonSerializer.Serialize(Settings);
        File.WriteAllText(SettingsPath, content);
    }
}
