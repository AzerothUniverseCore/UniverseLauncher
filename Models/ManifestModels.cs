using System.Text.Json.Serialization;

namespace AzerothUniverseLauncher.Models;

public class ManifestResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("generated_at")]
    public string GeneratedAt { get; set; } = "";

    [JsonPropertyName("total_files")]
    public long TotalFiles { get; set; }

    [JsonPropertyName("total_size")]
    public long TotalSize { get; set; }

    [JsonPropertyName("total_size_mb")]
    public double TotalSizeMb { get; set; }

    [JsonPropertyName("base_url")]
    public string BaseUrl { get; set; } = "";

    [JsonPropertyName("files")]
    public List<ManifestFile> Files { get; set; } = new();

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class ManifestFile
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; } = "";

    [JsonPropertyName("modified")]
    public long Modified { get; set; }
}
