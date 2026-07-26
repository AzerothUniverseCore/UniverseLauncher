using System;
using System.Collections.Generic;

namespace AzerothUniverseLauncher.Localization;

/// <summary>
/// Système de traduction minimal du launcher (Français / Anglais).
/// Toutes les chaînes affichées à l'utilisateur passent par Strings.T("clé")
/// (ou Strings.F("clé", ...) pour les chaînes avec des paramètres), ce qui
/// permet de piloter la langue de toute l'interface depuis un seul endroit.
/// </summary>
public static class Strings
{
    public const string French = "fr";
    public const string English = "en";

    /// <summary>Langue actuellement affichée. "fr" ou "en".</summary>
    public static string CurrentLanguage { get; private set; } = French;

    /// <summary>Change la langue courante ("fr" ou "en" ; toute autre valeur retombe sur "fr").</summary>
    public static void SetLanguage(string? languageCode)
    {
        CurrentLanguage = languageCode == English ? English : French;
    }

    /// <summary>Renvoie la chaîne traduite pour la clé donnée (ou la clé elle-même si introuvable).</summary>
    public static string T(string key)
    {
        var table = CurrentLanguage == English ? En : Fr;
        return table.TryGetValue(key, out var value) ? value : key;
    }

    /// <summary>Comme T(), mais applique string.Format avec les arguments fournis.</summary>
    public static string F(string key, params object?[] args) => string.Format(T(key), args);

    /// <summary>Unités de taille de fichier dans la langue courante (o/Ko/Mo/Go/To ou B/KB/MB/GB/TB).</summary>
    public static string[] SizeUnits => CurrentLanguage == English
        ? new[] { "B", "KB", "MB", "GB", "TB" }
        : new[] { "o", "Ko", "Mo", "Go", "To" };

    private static readonly Dictionary<string, string> Fr = new()
    {
        ["tagline"] = "Choisissez votre faction et préparez-vous à une aventure épique !",

        ["status_connecting"] = "Connexion...",
        ["status_online"] = "Serveur en ligne",
        ["status_offline"] = "Serveur hors ligne",
        ["status_unreachable"] = "Serveur injoignable",
        ["online_players_fmt"] = "{0} joueur(s) connecté(s)",

        ["client_folder_placeholder"] = "Aucun dossier sélectionné",
        ["deep_verify_label"] = "Vérification approfondie (MD5)",

        ["news_header"] = "ACTUALITÉS",
        ["client_folder_header"] = "DOSSIER CLIENT",
        ["journal_header"] = "JOURNAL",

        ["btn_website"] = "SITE WEB",
        ["btn_register"] = "S'INSCRIRE",
        ["btn_verify"] = "VÉRIFIER",
        ["btn_update"] = "METTRE À JOUR",
        ["btn_play"] = "JOUER",

        ["status_ready"] = "Prêt.",
        ["log_connecting_server"] = "Connexion au serveur...",
        ["status_select_folder"] = "Sélectionnez votre dossier client pour commencer.",

        ["log_background_loaded_fmt"] = "Fond d'écran chargé : {0}",
        ["log_background_unreadable_fmt"] = "Fond d'écran trouvé mais illisible ({0}) : {1}",
        ["log_background_missing_fmt"] = "Aucun fond d'écran trouvé dans {0} (background.jpg/.png/.webp) — fond dégradé par défaut utilisé.",

        ["log_server_unreachable_fmt"] = "Impossible de contacter le serveur : {0}",

        ["folder_dialog_description"] = "Choisissez le dossier d'installation du client Azeroth Universe",
        ["log_client_folder_set_fmt"] = "Dossier client défini : {0}",

        ["status_checking_files"] = "Vérification des fichiers...",
        ["log_fetching_manifest"] = "Récupération du manifest distant...",
        ["log_manifest_error_fmt"] = "Erreur manifest : {0}",
        ["status_manifest_error"] = "Erreur lors de la récupération du manifest.",
        ["status_client_up_to_date"] = "Le client est à jour.",
        ["log_no_update_needed"] = "Aucune mise à jour nécessaire.",
        ["status_files_to_download_fmt"] = "{0} fichier(s) à télécharger ({1})",
        ["log_files_missing_fmt"] = "{0} fichier(s) manquant(s) ou obsolète(s).",
        ["log_verify_error_fmt"] = "Erreur pendant la vérification : {0}",
        ["status_verify_error"] = "Erreur pendant la vérification.",

        ["log_download_start_fmt"] = "Démarrage du téléchargement de {0} fichier(s)...",
        ["speed_suffix_fmt"] = " — {0}/s",
        ["eta_remaining_fmt"] = " — restant : {0}",
        ["status_download_progress_fmt"] = "Téléchargement {0}/{1} — {2} / {3} ({4}%){5}{6}",
        ["log_download_success"] = "Téléchargement terminé avec succès.",
        ["status_download_done"] = "Téléchargement terminé.",
        ["log_download_canceled"] = "Téléchargement annulé.",
        ["status_download_canceled"] = "Téléchargement annulé.",
        ["log_download_error_fmt"] = "Erreur pendant le téléchargement : {0}",
        ["status_download_error"] = "Erreur pendant le téléchargement.",

        ["log_exe_not_found_fmt"] = "Exécutable introuvable : {0}",
        ["msg_exe_not_found_fmt"] = "Impossible de trouver {0} dans le dossier client.",
        ["log_launching_client"] = "Lancement du client...",
        ["log_launch_error_fmt"] = "Impossible de lancer le client : {0}",
        ["msg_launch_error_fmt"] = "Impossible de lancer le client :\n\n{0}",

        ["msg_select_folder_first"] = "Merci de sélectionner d'abord votre dossier client (bouton \"...\").",
        ["msg_unexpected_error_fmt"] = "Une erreur inattendue est survenue :\n\n{0}",
        ["app_box_title"] = "Azeroth Universe Launcher",

        ["news_type_update"] = "MAJ",
        ["news_type_event"] = "ÉVÉNEMENT",
        ["news_type_info"] = "INFO",

        ["log_to_download_fmt"] = "À télécharger : {0}",
        ["log_downloading_fmt"] = "Téléchargement : {0} ({1})",
        ["log_file_done_fmt"] = "Terminé : {0}",
        ["log_file_error_fmt"] = "ERREUR sur {0} : {1}",

        ["btn_pause"] = "PAUSE",
        ["btn_resume"] = "REPRENDRE",
        ["status_download_paused"] = "Téléchargement en pause.",
        ["log_download_paused"] = "Téléchargement mis en pause.",
        ["log_download_resumed"] = "Téléchargement repris.",

        ["btn_repair"] = "RÉPARER",
        ["log_repair_start_fmt"] = "Réparation de {0}...",
        ["log_repair_success_fmt"] = "Réparation réussie : {0}",
        ["log_repair_failed_fmt"] = "Échec de la réparation de {0} : {1}",
    };

    private static readonly Dictionary<string, string> En = new()
    {
        ["tagline"] = "Choose your faction and get ready for an epic adventure!",

        ["status_connecting"] = "Connecting...",
        ["status_online"] = "Server online",
        ["status_offline"] = "Server offline",
        ["status_unreachable"] = "Server unreachable",
        ["online_players_fmt"] = "{0} player(s) online",

        ["client_folder_placeholder"] = "No folder selected",
        ["deep_verify_label"] = "Deep verification (MD5)",

        ["news_header"] = "NEWS",
        ["client_folder_header"] = "CLIENT FOLDER",
        ["journal_header"] = "LOG",

        ["btn_website"] = "WEBSITE",
        ["btn_register"] = "REGISTER",
        ["btn_verify"] = "VERIFY",
        ["btn_update"] = "UPDATE",
        ["btn_play"] = "PLAY",

        ["status_ready"] = "Ready.",
        ["log_connecting_server"] = "Connecting to server...",
        ["status_select_folder"] = "Select your client folder to get started.",

        ["log_background_loaded_fmt"] = "Background loaded: {0}",
        ["log_background_unreadable_fmt"] = "Background found but unreadable ({0}): {1}",
        ["log_background_missing_fmt"] = "No background found in {0} (background.jpg/.png/.webp) — using default gradient background.",

        ["log_server_unreachable_fmt"] = "Unable to contact server: {0}",

        ["folder_dialog_description"] = "Choose the installation folder for the Azeroth Universe client",
        ["log_client_folder_set_fmt"] = "Client folder set: {0}",

        ["status_checking_files"] = "Checking files...",
        ["log_fetching_manifest"] = "Fetching remote manifest...",
        ["log_manifest_error_fmt"] = "Manifest error: {0}",
        ["status_manifest_error"] = "Error while fetching the manifest.",
        ["status_client_up_to_date"] = "The client is up to date.",
        ["log_no_update_needed"] = "No update needed.",
        ["status_files_to_download_fmt"] = "{0} file(s) to download ({1})",
        ["log_files_missing_fmt"] = "{0} file(s) missing or outdated.",
        ["log_verify_error_fmt"] = "Error during verification: {0}",
        ["status_verify_error"] = "Error during verification.",

        ["log_download_start_fmt"] = "Starting download of {0} file(s)...",
        ["speed_suffix_fmt"] = " — {0}/s",
        ["eta_remaining_fmt"] = " — remaining: {0}",
        ["status_download_progress_fmt"] = "Download {0}/{1} — {2} / {3} ({4}%){5}{6}",
        ["log_download_success"] = "Download completed successfully.",
        ["status_download_done"] = "Download completed.",
        ["log_download_canceled"] = "Download canceled.",
        ["status_download_canceled"] = "Download canceled.",
        ["log_download_error_fmt"] = "Error during download: {0}",
        ["status_download_error"] = "Error during download.",

        ["log_exe_not_found_fmt"] = "Executable not found: {0}",
        ["msg_exe_not_found_fmt"] = "Could not find {0} in the client folder.",
        ["log_launching_client"] = "Launching client...",
        ["log_launch_error_fmt"] = "Unable to launch client: {0}",
        ["msg_launch_error_fmt"] = "Unable to launch client:\n\n{0}",

        ["msg_select_folder_first"] = "Please select your client folder first (the \"...\" button).",
        ["msg_unexpected_error_fmt"] = "An unexpected error occurred:\n\n{0}",
        ["app_box_title"] = "Azeroth Universe Launcher",

        ["news_type_update"] = "UPDATE",
        ["news_type_event"] = "EVENT",
        ["news_type_info"] = "INFO",

        ["log_to_download_fmt"] = "To download: {0}",
        ["log_downloading_fmt"] = "Downloading: {0} ({1})",
        ["log_file_done_fmt"] = "Done: {0}",
        ["log_file_error_fmt"] = "ERROR on {0}: {1}",

        ["btn_pause"] = "PAUSE",
        ["btn_resume"] = "RESUME",
        ["status_download_paused"] = "Download paused.",
        ["log_download_paused"] = "Download paused.",
        ["log_download_resumed"] = "Download resumed.",

        ["btn_repair"] = "REPAIR",
        ["log_repair_start_fmt"] = "Repairing {0}...",
        ["log_repair_success_fmt"] = "Repair successful: {0}",
        ["log_repair_failed_fmt"] = "Failed to repair {0}: {1}",
    };
}

/// <summary>
/// Message différé (clé de traduction + arguments) au lieu d'une chaîne déjà rendue.
/// Permet de re-générer l'affichage (journal, statut) dans une autre langue si
/// l'utilisateur change de langue après coup, sans perdre les messages passés.
/// </summary>
public readonly record struct LogEntry(string Key, object?[] Args)
{
    public LogEntry(string key) : this(key, Array.Empty<object?>())
    {
    }

    public string Render() => Args.Length == 0 ? Strings.T(Key) : Strings.F(Key, Args);
}
