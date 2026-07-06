using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AzerothUniverseLauncher.Models;
using AzerothUniverseLauncher.Services;
using WinFormsFolderDialog = System.Windows.Forms.FolderBrowserDialog;

namespace AzerothUniverseLauncher;

public partial class MainWindow : Window
{
    private enum ActionMode { CheckFolder, Update, Play, Busy }

    private readonly ApiService _api = new();
    private readonly UpdateService _updater = new();
    private readonly SettingsService _settingsService = new();

    private LauncherSettings _settings = new();
    private List<ManifestFile> _pendingFiles = new();
    private string _manifestUrl = "";
    private ActionMode _mode = ActionMode.CheckFolder;
    private CancellationTokenSource? _busyCts;

    private readonly ObservableCollection<string> _journalEntries = new();
    private readonly DispatcherTimer _statusTimer;

    public MainWindow()
    {
        InitializeComponent();

        JournalItemsControl.ItemsSource = _journalEntries;

        TitleBarVersionText.Text = "build " + Config.LauncherVersion;
        HeaderTitleText.Text = Config.LauncherTitle;
        HeaderTaglineText.Text = Config.LauncherTagline;

        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Config.StatusRefreshIntervalSeconds)
        };
        _statusTimer.Tick += async (_, _) => await RefreshNewsAndStatusAsync();

        Loaded += MainWindow_Loaded;
        Closing += (_, _) => _statusTimer.Stop();
    }

    // =====================================================================
    // INITIALISATION
    // =====================================================================

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadBackgroundImage();

        _settings = _settingsService.Load();
        DeepVerifyCheckBox.IsChecked = _settings.DeepVerify;

        if (!string.IsNullOrWhiteSpace(_settings.ClientFolder))
        {
            ClientFolderTextBox.Text = _settings.ClientFolder;
        }

        Log("Connexion au serveur...");
        await RefreshNewsAndStatusAsync();
        _statusTimer.Start();

        if (!string.IsNullOrWhiteSpace(_settings.ClientFolder) && Directory.Exists(_settings.ClientFolder))
        {
            await RunCheckAsync();
        }
        else
        {
            SetMode(ActionMode.CheckFolder);
            DownloadStatusText.Text = "Sélectionnez votre dossier client pour commencer.";
        }
    }

    /// <summary>
    /// Charge Assets/background.jpg (ou .png) s'il existe. Sinon, garde le fond
    /// dégradé par défaut — aucune erreur, aucun plantage si le fichier est absent.
    /// </summary>
    private void LoadBackgroundImage()
    {
        var assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

        foreach (var name in new[] { "background.jpg", "background.png", "background.webp" })
        {
            var path = Path.Combine(assetsDir, name);
            if (!File.Exists(path)) continue;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                BackgroundImage.Source = bitmap;
                Log($"Fond d'écran chargé : {path}");
                return;
            }
            catch (Exception ex)
            {
                Log($"Fond d'écran trouvé mais illisible ({name}) : {ex.Message}");
            }
        }

        Log($"Aucun fond d'écran trouvé dans {assetsDir} (background.jpg/.png/.webp) — fond dégradé par défaut utilisé.");
    }

    private async Task RefreshNewsAndStatusAsync()
    {
        try
        {
            var news = await _api.GetNewsAsync();
            _manifestUrl = news.VersionInfo.ManifestUrl;

            NewsItemsControl.ItemsSource = news.News
                .Select(NewsDisplayItem.FromNewsItem)
                .ToList();

            bool online = news.VersionInfo.ServerStatus.Equals("online", StringComparison.OrdinalIgnoreCase);

            StatusLabelText.Text = online ? "Serveur en ligne" : "Serveur hors ligne";
            OnlinePlayersText.Text = online ? $"{news.VersionInfo.OnlinePlayers} joueur(s) connecté(s)" : " ";

            // Couleur du point : vert si en ligne, rouge si hors ligne.
            StatusDot.Fill = new System.Windows.Media.SolidColorBrush(online
                ? System.Windows.Media.Color.FromRgb(0x3E, 0xC9, 0x6B)
                : System.Windows.Media.Color.FromRgb(0xD9, 0x4A, 0x4A));
        }
        catch (Exception ex)
        {
            Log("Impossible de contacter le serveur : " + ex.Message);
            StatusLabelText.Text = "Serveur injoignable";
            OnlinePlayersText.Text = " ";
            StatusDot.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD9, 0x4A, 0x4A));
        }
    }

    // =====================================================================
    // SÉLECTION DU DOSSIER CLIENT
    // =====================================================================

    private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinFormsFolderDialog
        {
            Description = "Choisissez le dossier d'installation du client Azeroth Universe",
            UseDescriptionForTitle = true
        };

        if (!string.IsNullOrWhiteSpace(_settings.ClientFolder) && Directory.Exists(_settings.ClientFolder))
            dialog.SelectedPath = _settings.ClientFolder;

        var result = dialog.ShowDialog();
        if (result != System.Windows.Forms.DialogResult.OK) return;

        _settings.ClientFolder = dialog.SelectedPath;
        ClientFolderTextBox.Text = dialog.SelectedPath;
        _settingsService.Save(_settings);

        Log("Dossier client défini : " + dialog.SelectedPath);
        await RunCheckAsync();
    }

    private void DeepVerifyCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _settings.DeepVerify = DeepVerifyCheckBox.IsChecked == true;
        _settingsService.Save(_settings);
    }

    // =====================================================================
    // VÉRIFICATION
    // =====================================================================

    private async void Verify_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureClientFolderSelected()) return;
        await RunCheckAsync();
    }

    private async Task RunCheckAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientFolder)) return;

        SetMode(ActionMode.Busy);
        DownloadStatusText.Text = "Vérification des fichiers...";
        DownloadProgressBar.IsIndeterminate = true;
        Log("Récupération du manifest distant...");

        try
        {
            if (string.IsNullOrWhiteSpace(_manifestUrl))
            {
                var news = await _api.GetNewsAsync();
                _manifestUrl = news.VersionInfo.ManifestUrl;
            }

            var manifest = await _api.GetManifestAsync(_manifestUrl);
            if (!manifest.Success)
            {
                Log("Erreur manifest : " + manifest.Error);
                DownloadStatusText.Text = "Erreur lors de la récupération du manifest.";
                SetMode(ActionMode.CheckFolder);
                return;
            }

            Directory.CreateDirectory(_settings.ClientFolder);

            var progressLog = new Progress<string>(Log);
            var result = await _updater.CheckAsync(
                _settings.ClientFolder, manifest.Files, _settings.DeepVerify, progressLog, CancellationToken.None);

            _pendingFiles = result.ToDownload;
            DownloadProgressBar.IsIndeterminate = false;
            DownloadProgressBar.Value = 0;

            if (_pendingFiles.Count == 0)
            {
                DownloadStatusText.Text = "Le client est à jour.";
                Log("Aucune mise à jour nécessaire.");
                SetMode(ActionMode.Play);
            }
            else
            {
                DownloadStatusText.Text =
                    $"{_pendingFiles.Count} fichier(s) à télécharger ({UpdateService.FormatSize(result.TotalBytesToDownload)})";
                Log($"{_pendingFiles.Count} fichier(s) manquant(s) ou obsolète(s).");
                SetMode(ActionMode.Update);
            }
        }
        catch (Exception ex)
        {
            Log("Erreur pendant la vérification : " + ex.Message);
            DownloadStatusText.Text = "Erreur pendant la vérification.";
            DownloadProgressBar.IsIndeterminate = false;
            SetMode(ActionMode.CheckFolder);
        }
    }

    // =====================================================================
    // BOUTON D'ACTION PRINCIPAL (JOUER / METTRE À JOUR)
    // =====================================================================

    private async void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        switch (_mode)
        {
            case ActionMode.CheckFolder:
                if (EnsureClientFolderSelected()) await RunCheckAsync();
                break;

            case ActionMode.Update:
                await RunDownloadAsync();
                break;

            case ActionMode.Play:
                LaunchClient();
                break;
        }
    }

    private async Task RunDownloadAsync()
    {
        if (_pendingFiles.Count == 0) return;

        SetMode(ActionMode.Busy);
        _busyCts = new CancellationTokenSource();
        Log($"Démarrage du téléchargement de {_pendingFiles.Count} fichier(s)...");

        var stopwatch = Stopwatch.StartNew();

        var progress = new Progress<DownloadProgressInfo>(info =>
        {
            DownloadProgressBar.Value = info.Percent;

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            var speedBytesPerSecond = elapsedSeconds > 0.5 ? info.DownloadedBytes / elapsedSeconds : 0;

            var speedText = speedBytesPerSecond > 0
                ? $" — {UpdateService.FormatSize((long)speedBytesPerSecond)}/s"
                : "";

            var etaText = "";
            if (speedBytesPerSecond > 0)
            {
                var remainingBytes = info.TotalBytes - info.DownloadedBytes;
                var etaSeconds = remainingBytes / speedBytesPerSecond;
                etaText = $" — restant : {FormatDuration(etaSeconds)}";
            }

            DownloadStatusText.Text =
                $"Téléchargement {info.FilesCompleted}/{info.FilesTotal} — " +
                $"{UpdateService.FormatSize(info.DownloadedBytes)} / {UpdateService.FormatSize(info.TotalBytes)} " +
                $"({info.Percent:0.#}%){speedText}{etaText}";
        });

        var progressLog = new Progress<string>(Log);

        try
        {
            await _updater.DownloadAllAsync(_settings.ClientFolder, _pendingFiles, progress, progressLog, _busyCts.Token);
            Log("Téléchargement terminé avec succès.");
            DownloadStatusText.Text = "Téléchargement terminé.";
            await RunCheckAsync();
        }
        catch (OperationCanceledException)
        {
            Log("Téléchargement annulé.");
            DownloadStatusText.Text = "Téléchargement annulé.";
            SetMode(ActionMode.Update);
        }
        catch (Exception ex)
        {
            Log("Erreur pendant le téléchargement : " + ex.Message);
            DownloadStatusText.Text = "Erreur pendant le téléchargement.";
            SetMode(ActionMode.Update);
        }
    }

    private static string FormatDuration(double totalSeconds)
    {
        if (double.IsInfinity(totalSeconds) || double.IsNaN(totalSeconds) || totalSeconds < 0)
            return "--:--";

        var span = TimeSpan.FromSeconds(totalSeconds);
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes:00}:{span.Seconds:00}";
    }

    private void LaunchClient()
    {
        var exePath = Path.Combine(_settings.ClientFolder, Config.ClientExecutableName);

        if (!File.Exists(exePath))
        {
            Log("Exécutable introuvable : " + exePath);
            System.Windows.MessageBox.Show(
                $"Impossible de trouver {Config.ClientExecutableName} dans le dossier client.",
                "Azeroth Universe Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Log("Lancement du client...");
            Process.Start(new ProcessStartInfo(exePath)
            {
                WorkingDirectory = _settings.ClientFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log("Impossible de lancer le client : " + ex.Message);
            System.Windows.MessageBox.Show(
                "Impossible de lancer le client :\n\n" + ex.Message,
                "Azeroth Universe Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // =====================================================================
    // LIENS EXTERNES
    // =====================================================================

    private void Website_Click(object sender, RoutedEventArgs e) => OpenUrl("https://azeroth-universe.eu/");

    private void Register_Click(object sender, RoutedEventArgs e) => OpenUrl("https://azeroth-universe.eu/fr/register");

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Non bloquant : si le navigateur par défaut ne s'ouvre pas, ce n'est pas critique.
        }
    }

    // =====================================================================
    // FENÊTRE (barre de titre personnalisée)
    // =====================================================================

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();

    // =====================================================================
    // HELPERS
    // =====================================================================

    private bool EnsureClientFolderSelected()
    {
        if (!string.IsNullOrWhiteSpace(_settings.ClientFolder)) return true;

        System.Windows.MessageBox.Show(
            "Merci de sélectionner d'abord votre dossier client (bouton \"...\").",
            "Azeroth Universe Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    private void SetMode(ActionMode mode)
    {
        _mode = mode;
        switch (mode)
        {
            case ActionMode.CheckFolder:
                ActionButton.Content = "VÉRIFIER";
                ActionButton.IsEnabled = true;
                break;
            case ActionMode.Update:
                ActionButton.Content = "METTRE À JOUR";
                ActionButton.IsEnabled = true;
                break;
            case ActionMode.Play:
                ActionButton.Content = "JOUER";
                ActionButton.IsEnabled = true;
                break;
            case ActionMode.Busy:
                ActionButton.IsEnabled = false;
                break;
        }
    }

    private void Log(string message)
    {
        void Append()
        {
            _journalEntries.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            if (_journalEntries.Count > 500) _journalEntries.RemoveAt(0);
            JournalScrollViewer.ScrollToEnd();
        }

        if (Dispatcher.CheckAccess()) Append();
        else Dispatcher.Invoke(Append);
    }
}
