using System.Net.Http;
using System.Text.Json;
using AzerothUniverseLauncher.Models;

namespace AzerothUniverseLauncher.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService()
    {
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("AzerothUniverseLauncher/" + Config.LauncherVersion);
    }

    /// <summary>
    /// Récupère les actualités et le statut serveur. <paramref name="lang"/> ("fr" ou "en")
    /// est transmis au serveur en query string (?lang=..) pour recevoir les actualités et
    /// l'URL d'inscription dans la bonne langue.
    /// </summary>
    public async Task<NewsApiResponse> GetNewsAsync(string lang = "fr", CancellationToken ct = default)
    {
        var url = BuildUrlWithLang(Config.NewsUrl, lang);
        var json = await _http.GetStringAsync(url, ct);
        var result = JsonSerializer.Deserialize<NewsApiResponse>(json, JsonOptions);
        return result ?? new NewsApiResponse();
    }

    public async Task<ManifestResponse> GetManifestAsync(string manifestUrl, CancellationToken ct = default)
    {
        // Le scan du manifest peut prendre du temps côté serveur (calcul de MD5 sur plusieurs Go),
        // on utilise donc un client dédié avec un timeout beaucoup plus large.
        using var manifestHttp = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        manifestHttp.DefaultRequestHeaders.UserAgent.ParseAdd("AzerothUniverseLauncher/" + Config.LauncherVersion);

        var json = await manifestHttp.GetStringAsync(manifestUrl, ct);
        var result = JsonSerializer.Deserialize<ManifestResponse>(json, JsonOptions);
        return result ?? new ManifestResponse { Success = false, Error = "Réponse manifest invalide." };
    }

    private static string BuildUrlWithLang(string url, string lang)
    {
        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}lang={Uri.EscapeDataString(lang)}";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
