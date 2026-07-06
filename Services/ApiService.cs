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

    public async Task<NewsApiResponse> GetNewsAsync(CancellationToken ct = default)
    {
        var json = await _http.GetStringAsync(Config.NewsUrl, ct);
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
