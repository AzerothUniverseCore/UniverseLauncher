using System.Text.Json.Serialization;

namespace AzerothUniverseLauncher.Models;

public class NewsApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("version_info")]
    public VersionInfo VersionInfo { get; set; } = new();

    [JsonPropertyName("news")]
    public List<NewsItem> News { get; set; } = new();

    [JsonPropertyName("generated_at")]
    public string GeneratedAt { get; set; } = "";
}

public class VersionInfo
{
    [JsonPropertyName("required_version")]
    public string RequiredVersion { get; set; } = "";

    [JsonPropertyName("launcher_version")]
    public string LauncherVersion { get; set; } = "";

    [JsonPropertyName("manifest_url")]
    public string ManifestUrl { get; set; } = "";

    [JsonPropertyName("website_url")]
    public string WebsiteUrl { get; set; } = "";

    [JsonPropertyName("register_url")]
    public string RegisterUrl { get; set; } = "";

    [JsonPropertyName("server_status")]
    public string ServerStatus { get; set; } = "offline";

    [JsonPropertyName("online_players")]
    public int OnlinePlayers { get; set; }
}

public class NewsItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("date")]
    public string Date { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "info";

    [JsonPropertyName("image")]
    public string? Image { get; set; }
}
