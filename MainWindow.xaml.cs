using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AzerothUniverseLauncher.Localization;
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
    private string _registerUrl = "https://azeroth-universe.eu/fr/register";
    private ActionMode _mode = ActionMode.CheckFolder;

    /// <summary>
    /// Dernier mode "actionnable" (hors Busy). Utilisé pour que le bouton d'action
    /// garde un libellé cohérent (et traduit) pendant qu'il est désactivé (Busy),
    /// au lieu de rester figé sur l'ancien texte.
    /// </summary>
    private ActionMode _lastActionableMode = ActionMode.CheckFolder;

    private CancellationTokenSource? _busyCts;

    // Statut de téléchargement/vérification affiché en bas à gauche : on garde la
    // clé + les arguments (pas juste le texte déjà rendu) pour pouvoir le re-générer
    // dans la nouvelle langue si l'utilisateur bascule FR/EN en cours d'opération.
    private string _statusKey = "status_ready";
    private object?[] _statusArgs = Array.Empty<object?>();

    // Journal : on garde chaque ligne sous forme (horodatage, clé + arguments) pour
    // pouvoir reconstruire tout l'affichage dans la nouvelle langue au moment du
    // changement de langue, sans perdre l'historique.
    private readonly List<(DateTime Timestamp, LogEntry Entry)> _journalRecords = new();
    private readonly ObservableCollection<string> _journalEntries = new();

    private readonly DispatcherTimer _statusTimer;

    public MainWindow()
    {
        InitializeComponent();

        JournalItemsControl.ItemsSource = _journalEntries;

        TitleBarVersionText.Text = "build " + Config.LauncherVersion;
        HeaderTitleText.Text = Config.LauncherTitle;

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
        _settings = _settingsService.Load();
        Strings.SetLanguage(_settings.Language);
        ApplyLanguage();

        LoadBackgroundImage();

        DeepVerifyCheckBox.IsChecked = _settings.DeepVerify;

        if (!string.IsNullOrWhiteSpace(_settings.ClientFolder))
        {
            ClientFolderTextBox.Text = _settings.ClientFolder;
        }

        Log("log_connecting_server");
        await RefreshNewsAndStatusAsync();
        _statusTimer.Start();

        if (!string.IsNullOrWhiteSpace(_settings.ClientFolder) && Directory.Exists(_settings.ClientFolder))
        {
            await RunCheckAsync();
        }
        else
        {
            SetMode(ActionMode.CheckFolder);
            SetStatus("status_select_folder");
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
                Log("log_background_loaded_fmt", path);
                return;
            }
            catch (Exception ex)
            {
                Log("log_background_unreadable_fmt", name, ex.Message);
            }
        }

        Log("log_background_missing_fmt", assetsDir);
    }

    private async Task RefreshNewsAndStatusAsync()
    {
        try
        {
            var news = await _api.GetNewsAsync(Strings.CurrentLanguage);
            _manifestUrl = news.VersionInfo.ManifestUrl;
            if (!string.IsNullOrWhiteSpace(news.VersionInfo.RegisterUrl))
            {
                _registerUrl = news.VersionInfo.RegisterUrl;
            }

            NewsItemsControl.ItemsSource = news.News
                .Select(NewsDisplayItem.FromNewsItem)
                .ToList();

            bool online = news.VersionInfo.ServerStatus.Equals("online", StringComparison.OrdinalIgnoreCase);

            StatusLabelText.Text = online ? Strings.T("status_online") : Strings.T("status_offline");
            OnlinePlayersText.Text = online ? Strings.F("online_players_fmt", news.VersionInfo.OnlinePlayers) : " ";

            // Couleur du point : vert si en ligne, rouge si hors ligne.
            StatusDot.Fill = new System.Windows.Media.SolidColorBrush(online
                ? System.Windows.Media.Color.FromRgb(0x3E, 0xC9, 0x6B)
                : System.Windows.Media.Color.FromRgb(0xD9, 0x4A, 0x4A));
        }
        catch (Exception ex)
        {
            Log("log_server_unreachable_fmt", ex.Message);
            StatusLabelText.Text = Strings.T("status_unreachable");
            OnlinePlayersText.Text = " ";
            StatusDot.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD9, 0x4A, 0x4A));
        }
    }

    // =====================================================================
    // LANGUE (FR / EN)
    // =====================================================================

    private async void LangFr_Click(object sender, RoutedEventArgs e) => await SetLanguageAsync(Strings.French);

    private async void LangEn_Click(object sender, RoutedEventArgs e) => await SetLanguageAsync(Strings.English);

    private async Task SetLanguageAsync(string languageCode)
    {
        if (Strings.CurrentLanguage == languageCode) return;

        Strings.SetLanguage(languageCode);
        _settings.Language = languageCode;
        _settingsService.Save(_settings);

        ApplyLanguage();

        // Les actualités et le libellé "en ligne"/"hors ligne" viennent du serveur :
        // on les redemande immédiatement dans la nouvelle langue.
        await RefreshNewsAndStatusAsync();
    }

    /// <summary>
    /// Met à jour tous les libellés de l'interface dans la langue courante : textes
    /// statiques, libellé du bouton d'action, statut de téléchargement en cours et
    /// historique complet du journal (chaque ligne est re-générée depuis sa clé de
    /// traduction d'origine, rien n'est perdu ni figé dans l'ancienne langue).
    /// </summary>
    private void ApplyLanguage()
    {
        HeaderTaglineText.Text = Strings.T("tagline");
        NewsHeaderText.Text = Strings.T("news_header");
        ClientFolderHeaderText.Text = Strings.T("client_folder_header");
        JournalHeaderText.Text = Strings.T("journal_header");
        DeepVerifyCheckBox.Content = Strings.T("deep_verify_label");
        WebsiteButton.Content = Strings.T("btn_website");
        RegisterButton.Content = Strings.T("btn_register");
        VerifyButton.Content = Strings.T("btn_verify");

        if (string.IsNullOrWhiteSpace(_settings.ClientFolder))
        {
            ClientFolderTextBox.Text = Strings.T("client_folder_placeholder");
        }

        SetMode(_mode); // rafraîchit le texte du bouton d'action dans la nouvelle langue
        RefreshStatusText(); // rafraîchit le statut en cours (bas de fenêtre)
        RebuildJournalDisplay(); // retraduit tout l'historique du journal

        var activeBrush = (System.Windows.Media.Brush)FindResource("BrushGoldBright");
        var inactiveBrush = (System.Windows.Media.Brush)FindResource("BrushTextSecondary");
        LangFrButton.Foreground = Strings.CurrentLanguage == Strings.French ? activeBrush : inactiveBrush;
        LangEnButton.Foreground = Strings.CurrentLanguage == Strings.English ? activeBrush : inactiveBrush;
    }

    // =====================================================================
    // SÉLECTION DU DOSSIER CLIENT
    // =====================================================================

    private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinFormsFolderDialog
        {
            Description = Strings.T("folder_dialog_description"),
            UseDescriptionForTitle = true
        };

        if (!string.IsNullOrWhiteSpace(_settings.ClientFolder) && Directory.Exists(_settings.ClientFolder))
            dialog.SelectedPath = _settings.ClientFolder;

        var result = dialog.ShowDialog();
        if (result != System.Windows.Forms.DialogResult.OK) return;

        _settings.ClientFolder = dialog.SelectedPath;
        ClientFolderTextBox.Text = dialog.SelectedPath;
        _settingsService.Save(_settings);

        Log("log_client_folder_set_fmt", dialog.SelectedPath);
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
        SetStatus("status_checking_files");
        DownloadProgressBar.IsIndeterminate = true;
        Log("log_fetching_manifest");

        try
        {
            if (string.IsNullOrWhiteSpace(_manifestUrl))
            {
                var news = await _api.GetNewsAsync(Strings.CurrentLanguage);
                _manifestUrl = news.VersionInfo.ManifestUrl;
            }

            var manifest = await _api.GetManifestAsync(_manifestUrl);
            if (!manifest.Success)
            {
                Log("log_manifest_error_fmt", manifest.Error);
                SetStatus("status_manifest_error");
                SetMode(ActionMode.CheckFolder);
                return;
            }

            Directory.CreateDirectory(_settings.ClientFolder);

            var progressLog = new Progress<LogEntry>(entry => Log(entry.Key, entry.Args));
            var result = await _updater.CheckAsync(
                _settings.ClientFolder, manifest.Files, _settings.DeepVerify, progressLog, CancellationToken.None);

            _pendingFiles = result.ToDownload;
            DownloadProgressBar.IsIndeterminate = false;
            DownloadProgressBar.Value = 0;

            if (_pendingFiles.Count == 0)
            {
                SetStatus("status_client_up_to_date");
                Log("log_no_update_needed");
                SetMode(ActionMode.Play);
            }
            else
            {
                SetStatus(
                    "status_files_to_download_fmt",
                    _pendingFiles.Count,
                    UpdateService.FormatSize(result.TotalBytesToDownload));
                Log("log_files_missing_fmt", _pendingFiles.Count);
                SetMode(ActionMode.Update);
            }
        }
        catch (Exception ex)
        {
            Log("log_verify_error_fmt", ex.Message);
            SetStatus("status_verify_error");
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
        Log("log_download_start_fmt", _pendingFiles.Count);

        var stopwatch = Stopwatch.StartNew();

        var progress = new Progress<DownloadProgressInfo>(info =>
        {
            DownloadProgressBar.Value = info.Percent;

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            var speedBytesPerSecond = elapsedSeconds > 0.5 ? info.DownloadedBytes / elapsedSeconds : 0;

            var speedText = speedBytesPerSecond > 0
                ? Strings.F("speed_suffix_fmt", UpdateService.FormatSize((long)speedBytesPerSecond))
                : "";

            var etaText = "";
            if (speedBytesPerSecond > 0)
            {
                var remainingBytes = info.TotalBytes - info.DownloadedBytes;
                var etaSeconds = remainingBytes / speedBytesPerSecond;
                etaText = Strings.F("eta_remaining_fmt", FormatDuration(etaSeconds));
            }

            SetStatus(
                "status_download_progress_fmt",
                info.FilesCompleted,
                info.FilesTotal,
                UpdateService.FormatSize(info.DownloadedBytes),
                UpdateService.FormatSize(info.TotalBytes),
                $"{info.Percent:0.#}",
                speedText,
                etaText);
        });

        var progressLog = new Progress<LogEntry>(entry => Log(entry.Key, entry.Args));

        try
        {
            await _updater.DownloadAllAsync(_settings.ClientFolder, _pendingFiles, progress, progressLog, _busyCts.Token);
            Log("log_download_success");
            SetStatus("status_download_done");
            await RunCheckAsync();
        }
        catch (OperationCanceledException)
        {
            Log("log_download_canceled");
            SetStatus("status_download_canceled");
            SetMode(ActionMode.Update);
        }
        catch (Exception ex)
        {
            Log("log_download_error_fmt", ex.Message);
            SetStatus("status_download_error");
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
            Log("log_exe_not_found_fmt", exePath);
            System.Windows.MessageBox.Show(
                Strings.F("msg_exe_not_found_fmt", Config.ClientExecutableName),
                Strings.T("app_box_title"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Log("log_launching_client");
            Process.Start(new ProcessStartInfo(exePath)
            {
                WorkingDirectory = _settings.ClientFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log("log_launch_error_fmt", ex.Message);
            System.Windows.MessageBox.Show(
                Strings.F("msg_launch_error_fmt", ex.Message),
                Strings.T("app_box_title"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // =====================================================================
    // LIENS EXTERNES
    // =====================================================================

    private void Website_Click(object sender, RoutedEventArgs e) => OpenUrl("https://azeroth-universe.eu/");

    private void Register_Click(object sender, RoutedEventArgs e) => OpenUrl(_registerUrl);

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
            Strings.T("msg_select_folder_first"),
            Strings.T("app_box_title"), MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    /// <summary>Définit le statut affiché en bas de fenêtre, en gardant la clé/les arguments
    /// pour pouvoir le re-générer si la langue change pendant que ce statut est affiché.</summary>
    private void SetStatus(string key, params object?[] args)
    {
        _statusKey = key;
        _statusArgs = args;
        DownloadStatusText.Text = args.Length == 0 ? Strings.T(key) : Strings.F(key, args);
    }

    private void RefreshStatusText()
    {
        DownloadStatusText.Text = _statusArgs.Length == 0 ? Strings.T(_statusKey) : Strings.F(_statusKey, _statusArgs);
    }

    private void SetMode(ActionMode mode)
    {
        _mode = mode;
        if (mode != ActionMode.Busy)
        {
            _lastActionableMode = mode;
        }

        switch (mode)
        {
            case ActionMode.CheckFolder:
                ActionButton.Content = Strings.T("btn_verify");
                ActionButton.IsEnabled = true;
                break;
            case ActionMode.Update:
                ActionButton.Content = Strings.T("btn_update");
                ActionButton.IsEnabled = true;
                break;
            case ActionMode.Play:
                ActionButton.Content = Strings.T("btn_play");
                ActionButton.IsEnabled = true;
                break;
            case ActionMode.Busy:
                // Le bouton est désactivé, mais garde un libellé cohérent (et traduit)
                // correspondant à la dernière action disponible, au lieu de figer
                // l'ancien texte (ex. rester sur "VÉRIFIER" après un passage en anglais).
                ActionButton.Content = GetActionLabel(_lastActionableMode);
                ActionButton.IsEnabled = false;
                break;
        }
    }

    private static string GetActionLabel(ActionMode mode) => mode switch
    {
        ActionMode.Update => Strings.T("btn_update"),
        ActionMode.Play => Strings.T("btn_play"),
        _ => Strings.T("btn_verify")
    };

    /// <summary>Ajoute une ligne au journal à partir d'une clé de traduction (+ arguments),
    /// pour que l'historique puisse être retraduit intégralement si la langue change.</summary>
    private void Log(string key, params object?[] args)
    {
        var record = (DateTime.Now, new LogEntry(key, args));

        void Append()
        {
            _journalRecords.Add(record);
            if (_journalRecords.Count > 500) _journalRecords.RemoveAt(0);

            _journalEntries.Add(FormatJournalLine(record));
            if (_journalEntries.Count > 500) _journalEntries.RemoveAt(0);

            JournalScrollViewer.ScrollToEnd();
        }

        if (Dispatcher.CheckAccess()) Append();
        else Dispatcher.Invoke(Append);
    }

    private static string FormatJournalLine((DateTime Timestamp, LogEntry Entry) record) =>
        $"[{record.Timestamp:HH:mm:ss}] {record.Entry.Render()}";

    /// <summary>Reconstruit tout l'affichage du journal dans la langue courante, à partir
    /// de l'historique conservé (aucune ligne n'est perdue lors d'un changement de langue).</summary>
    private void RebuildJournalDisplay()
    {
        _journalEntries.Clear();
        foreach (var record in _journalRecords)
        {
            _journalEntries.Add(FormatJournalLine(record));
        }
    }
}
