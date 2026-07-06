# Azeroth Universe Launcher

Launcher WPF (.NET 8) style Blizzard pour le serveur **Azeroth Universe**.
Il consomme directement `news.php` et `manifest.php` (les scripts que tu utilises déjà)
pour afficher les actualités, le statut serveur, et télécharger/mettre à jour le client.

## Fonctionnalités

- Design sombre "façon Blizzard" : fond d'écran personnalisable, dorures, panneaux vitrés.
- Fenêtre sans bordure Windows, avec sa propre barre de titre (glisser / réduire / fermer).
- Statut serveur en direct (en ligne / hors ligne, nombre de joueurs) via `news.php`.
- Flux d'actualités affiché sous forme de cartes (info / MAJ / événement).
- Sélection du dossier client (persistée entre les sessions).
- Vérification des fichiers locaux contre `manifest.php` :
  - rapide par défaut (comparaison de taille),
  - option "vérification approfondie" qui recalcule le MD5 de chaque fichier.
- Téléchargement multi-fichiers en parallèle (4 téléchargements simultanés par défaut),
  avec barre de progression globale et journal détaillé.
- Bouton principal contextuel : `VÉRIFIER` → `METTRE À JOUR` → `JOUER`.
- Boutons Site Web / S'inscrire (ouvrent le navigateur par défaut).

## Pré-requis pour compiler

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (ou Visual Studio 2022 avec la charge
  de travail ".NET Desktop Development")

## Compiler / lancer

```bash
cd AzerothUniverseLauncher
dotnet restore
dotnet build -c Release
dotnet run
```

Ou ouvre simplement `AzerothUniverseLauncher.csproj` dans Visual Studio et fais F5.

Pour produire un exécutable autonome (un seul .exe, sans installer .NET sur le PC du joueur) :

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

L'exe sera dans `bin/Release/net8.0-windows/win-x64/publish/`.
**Pense à copier le dossier `Assets/` à côté de l'exe publié** (le fond d'écran est chargé
depuis là au runtime, il n'est pas embarqué dans le binaire).

## Configuration — le seul fichier à modifier

Tout ce qui est spécifique à ton serveur est centralisé dans **`Config.cs`** :

```csharp
public const string NewsUrl = "https://azeroth-universe.eu/universe_launcher/news.php";
public const string ClientExecutableName = "AzerothUniverse.exe";
public const string LauncherTitle = "AZEROTH UNIVERSE";
public const string LauncherTagline = "...";
public const string LauncherVersion = "3.3.9";
public const int MaxConcurrentDownloads = 4;
public const int StatusRefreshIntervalSeconds = 30;
```

Le launcher va chercher `NewsUrl`, qui doit renvoyer un champ
`version_info.manifest_url` — c'est ce lien qui sera automatiquement appelé
pour récupérer la liste des fichiers (`manifest.php`).

Les URLs "Site Web" / "S'inscrire" sont pour l'instant en dur dans
`MainWindow.xaml.cs` (méthodes `Website_Click` / `Register_Click`) ; tu peux
aussi les faire venir de `news.php` (`version_info.website_url` /
`register_url`, déjà renvoyés par ton script) si tu préfères piloter ça
côté serveur plutôt que côté launcher.

## Côté serveur PHP

Rien à changer dans tes scripts, le launcher est compatible tel quel avec :

- `news.php` : actualités + statut serveur + URL du manifest.
- `manifest.php` : liste des fichiers (chemin, url, taille, md5, date de modif),
  avec son cache 24h. **Après chaque mise à jour du client**, pense à relancer
  `generate_cache.php` en ligne de commande pour régénérer le cache
  (sinon les joueurs verront l'ancienne version pendant jusqu'à 24h) :

  ```
  php generate_cache.php
  ```

  Ou force la régénération via l'URL avec le token :
  `manifest.php?refresh=1&token=TON_TOKEN`.

## Personnaliser le visuel

- **Fond d'écran** : remplace `Assets/background.jpg` par ton propre visuel
  (tu peux réutiliser une de tes illustrations Leonardo AI). Voir
  `Assets/LISEZ-MOI.txt`.
- **Couleurs / dorures / boutons** : tout est dans `Resources/Styles.xaml`
  (palette en haut du fichier — `ColorGold`, `ColorPanel`, etc.).
- **Icône de l'exe** : ajoute `Assets/icon.ico` puis décommente la ligne
  `<ApplicationIcon>` dans le `.csproj`.

## Structure du projet

```
AzerothUniverseLauncher/
├── App.xaml / App.xaml.cs           Point d'entrée, styles globaux, gestion des erreurs
├── Config.cs                        ⚙️ Tous les réglages à personnaliser
├── MainWindow.xaml / .xaml.cs       Fenêtre principale + logique du launcher
├── Models/
│   ├── NewsModels.cs                Modèles pour news.php
│   ├── ManifestModels.cs            Modèles pour manifest.php
│   ├── NewsDisplayItem.cs           Adaptation des news pour l'affichage
│   └── LauncherSettings.cs          Réglages persistés (dossier client, etc.)
├── Services/
│   ├── ApiService.cs                Appels HTTP vers news.php / manifest.php
│   ├── UpdateService.cs             Comparaison locale/distante + téléchargement
│   └── SettingsService.cs           Sauvegarde JSON dans %AppData%
├── Converters/                      Convertisseurs XAML (statut → couleur, etc.)
├── Resources/Styles.xaml            Thème visuel (couleurs, boutons, panneaux)
└── Assets/                          Fond d'écran, icône
```

Les réglages du joueur (dossier client choisi, option MD5) sont sauvegardés dans :
`%AppData%\AzerothUniverseLauncher\settings.json`

## Limites connues / pistes d'amélioration

- Pas de reprise de téléchargement en cas de coupure sur un gros fichier en cours
  (le fichier repart de zéro s'il est interrompu — les MPQ étant volumineux,
  ça peut valoir le coup d'ajouter un support des requêtes `Range` plus tard).
- Pas de vérification de version du launcher lui-même (tu as `required_version`
  / `launcher_version` dans `news.php`, prêts à être exploités si tu veux
  forcer une mise à jour du launcher).
- Le lancement du jeu suppose un exécutable simple ; si ton client a besoin
  d'arguments de lancement spécifiques, ajuste `LaunchClient()` dans
  `MainWindow.xaml.cs`.
