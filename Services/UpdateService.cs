using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using AzerothUniverseLauncher.Models;

namespace AzerothUniverseLauncher.Services;

public class FileCheckResult
{
    public List<ManifestFile> ToDownload { get; set; } = new();
    public long TotalBytesToDownload { get; set; }
}

public class DownloadProgressInfo
{
    public string CurrentFile { get; set; } = "";
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public int FilesCompleted { get; set; }
    public int FilesTotal { get; set; }

    public double Percent => TotalBytes <= 0 ? 0 : Math.Min(100.0, DownloadedBytes * 100.0 / TotalBytes);
}

public class UpdateService
{
    /// <summary>
    /// Compare le dossier client local au manifest distant.
    /// Rapide par défaut (comparaison de taille). Passe deepVerify=true pour
    /// recalculer le MD5 de chaque fichier local (plus fiable, mais beaucoup plus lent).
    /// </summary>
    public Task<FileCheckResult> CheckAsync(
        string clientFolder,
        List<ManifestFile> manifestFiles,
        bool deepVerify,
        IProgress<string>? log,
        CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var toDownload = new List<ManifestFile>();

            foreach (var mf in manifestFiles)
            {
                ct.ThrowIfCancellationRequested();

                var localPath = ToLocalPath(clientFolder, mf.Path);
                bool needsDownload;

                if (!File.Exists(localPath))
                {
                    needsDownload = true;
                }
                else
                {
                    var fi = new FileInfo(localPath);
                    if (fi.Length != mf.Size)
                    {
                        needsDownload = true;
                    }
                    else if (deepVerify)
                    {
                        var localMd5 = ComputeMd5(localPath);
                        needsDownload = !string.Equals(localMd5, mf.Md5, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        needsDownload = false;
                    }
                }

                if (needsDownload)
                {
                    toDownload.Add(mf);
                    log?.Report($"À télécharger : {mf.Path}");
                }
            }

            return new FileCheckResult
            {
                ToDownload = toDownload,
                TotalBytesToDownload = toDownload.Sum(f => f.Size)
            };
        }, ct);
    }

    /// <summary>Télécharge tous les fichiers manquants/obsolètes avec une concurrence limitée.</summary>
    public async Task DownloadAllAsync(
        string clientFolder,
        List<ManifestFile> files,
        IProgress<DownloadProgressInfo> progress,
        IProgress<string>? log,
        CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AzerothUniverseLauncher/" + Config.LauncherVersion);

        long totalBytes = files.Sum(f => f.Size);
        long downloadedBytes = 0;
        int filesCompleted = 0;
        var sync = new object();

        using var semaphore = new SemaphoreSlim(Config.MaxConcurrentDownloads);

        var tasks = files.Select(async file =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                log?.Report($"Téléchargement : {file.Path} ({FormatSize(file.Size)})");

                await DownloadFileAsync(client, clientFolder, file, bytesRead =>
                {
                    long snapshot;
                    lock (sync)
                    {
                        downloadedBytes += bytesRead;
                        snapshot = downloadedBytes;
                    }

                    progress.Report(new DownloadProgressInfo
                    {
                        CurrentFile = file.Path,
                        TotalBytes = totalBytes,
                        DownloadedBytes = snapshot,
                        FilesCompleted = filesCompleted,
                        FilesTotal = files.Count
                    });
                }, ct);

                lock (sync) { filesCompleted++; }
                log?.Report($"Terminé : {file.Path}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log?.Report($"ERREUR sur {file.Path} : {ex.Message}");
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private static async Task DownloadFileAsync(
        HttpClient client,
        string clientFolder,
        ManifestFile file,
        Action<long> onBytesReceived,
        CancellationToken ct)
    {
        var localPath = ToLocalPath(clientFolder, file.Path);
        var dir = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var tempPath = localPath + ".downloading";

        using (var response = await client.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            response.EnsureSuccessStatusCode();

            await using var httpStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(
                tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

            var buffer = new byte[81920];
            int read;
            while ((read = await httpStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                onBytesReceived(read);
            }
        }

        if (File.Exists(localPath)) File.Delete(localPath);
        File.Move(tempPath, localPath);
    }

    public static string ComputeMd5(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = md5.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ToLocalPath(string clientFolder, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(clientFolder, normalized);
    }

    public static string FormatSize(long bytes)
    {
        string[] units = { "o", "Ko", "Mo", "Go", "To" };
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:0.##} {units[unit]}";
    }
}
