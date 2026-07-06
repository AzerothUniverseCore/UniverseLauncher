namespace AzerothUniverseLauncher.Models;

public class LauncherSettings
{
    /// <summary>Dossier local où est installé (ou sera installé) le client de jeu.</summary>
    public string ClientFolder { get; set; } = "";

    /// <summary>
    /// Si vrai, la vérification recalcule le MD5 de chaque fichier local
    /// (fiable à 100% mais plus lent). Sinon, seule la taille du fichier
    /// est comparée (rapide, suffisant dans l'immense majorité des cas).
    /// </summary>
    public bool DeepVerify { get; set; } = false;
}
