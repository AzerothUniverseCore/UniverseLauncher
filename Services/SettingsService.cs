using System.IO;
using System.Text.Json;
using AzerothUniverseLauncher.Models;

namespace AzerothUniverseLauncher.Services;

public class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AzerothUniverseLauncher");

        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public LauncherSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<LauncherSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch
        {
            // Fichier corrompu ou illisible : on repart sur des réglages par défaut.
        }

        return new LauncherSettings();
    }

    public void Save(LauncherSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Non bloquant : si on ne peut pas sauvegarder, tant pis, on redemandera au prochain lancement.
        }
    }
}
