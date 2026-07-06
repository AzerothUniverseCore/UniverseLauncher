namespace AzerothUniverseLauncher;

/// <summary>
/// Tous les réglages "à personnaliser" du launcher sont centralisés ici.
/// C'est le seul fichier que tu dois modifier pour brancher le launcher
/// sur ton serveur.
/// </summary>
public static class Config
{
    // -------------------------------------------------------------
    // Endpoints serveur
    // -------------------------------------------------------------

    /// <summary>
    /// URL de ton news.php. Il doit renvoyer version_info.manifest_url,
    /// c'est ce lien qui sera utilisé pour aller chercher manifest.php.
    /// </summary>
    public const string NewsUrl = "https://azeroth-universe.eu/universe_launcher/news.php";

    // -------------------------------------------------------------
    // Client de jeu
    // -------------------------------------------------------------

    /// <summary>Nom de l'exécutable à lancer dans le dossier client choisi par le joueur.</summary>
    public const string ClientExecutableName = "AzerothUniverse.exe";

    // -------------------------------------------------------------
    // Identité du launcher
    // -------------------------------------------------------------

    public const string LauncherTitle = "AZEROTH UNIVERSE";
    public const string LauncherTagline = "Choisissez votre faction et préparez-vous à une aventure épique !";
    public const string LauncherVersion = "3.3.9";

    // -------------------------------------------------------------
    // Comportement des mises à jour
    // -------------------------------------------------------------

    /// <summary>Nombre de téléchargements simultanés.</summary>
    public const int MaxConcurrentDownloads = 4;

    /// <summary>
    /// Intervalle (secondes) de rafraîchissement automatique du statut serveur
    /// (nombre de joueurs en ligne / en ligne-hors ligne).
    /// </summary>
    public const int StatusRefreshIntervalSeconds = 30;
}
