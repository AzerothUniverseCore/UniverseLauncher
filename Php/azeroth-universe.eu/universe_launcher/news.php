<?php
/**
 * Azeroth Universe Launcher - API Actualités / News API
 * À placer dans : C:/xampp/htdocs/eons_launcher/news.php
 */

header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');

// =============================================
// LANGUE DEMANDÉE PAR LE LAUNCHER (?lang=fr|en)
// =============================================

$lang = (isset($_GET['lang']) && $_GET['lang'] === 'en') ? 'en' : 'fr';

// =============================================
// ACTUALITÉS (modifie ici tes news, en fr ET en en)
// =============================================

$newsSource = [
    [
        'id'      => 1,
        'title'   => [
            'fr' => 'Bienvenue sur Azeroth Universe !',
            'en' => 'Welcome to Azeroth Universe!',
        ],
        'content' => [
            'fr' => 'Le Royaume Azeroth Universe est de retour avec de nouvelles fonctionnalités. Rejoignez-nous pour une aventure épique en Azeroth.',
            'en' => 'The Azeroth Universe realm is back with new features. Join us for an epic adventure in Azeroth.',
        ],
        'date'    => '2026-04-13',
        'type'    => 'info',
        'image'   => null,
    ],

    //[
    //    'id'      => 2,
    //    'title'   => [
    //        'fr' => 'Mise à jour du client v3.3.5a',
    //        'en' => 'Client update v3.3.5a',
    //    ],
    //    'content' => [
    //        'fr' => 'Nouveau patch disponible ! Des corrections de bugs et des améliorations de performance ont été apportées.',
    //        'en' => 'New patch available! Bug fixes and performance improvements have been made.',
    //    ],
    //    'date'    => '2026-04-11',
    //    'type'    => 'update',
    //    'image'   => null,
    //],

    //[
    //    'id'      => 3,
    //    'title'   => [
    //        'fr' => 'Événement de Printemps',
    //        'en' => 'Spring Event',
    //    ],
    //    'content' => [
    //        'fr' => 'Profitez de bonus d\'expérience x2 ce weekend ! Connectez-vous entre le 12 et le 14 avril pour en profiter.',
    //        'en' => 'Enjoy a x2 experience bonus this weekend! Log in between April 12 and 14 to take advantage of it.',
    //    ],
    //    'date'    => '2026-04-10',
    //    'type'    => 'event',
    //    'image'   => null,
    //],
];

// Sélectionne la langue demandée pour chaque news (repli sur le français si la
// traduction anglaise n'a pas encore été renseignée pour cette actualité).
$news = array_map(function ($item) use ($lang) {
    return [
        'id'      => $item['id'],
        'title'   => $item['title'][$lang] ?? $item['title']['fr'],
        'content' => $item['content'][$lang] ?? $item['content']['fr'],
        'date'    => $item['date'],
        'type'    => $item['type'],
        'image'   => $item['image'],
    ];
}, $newsSource);

// =============================================
// VERSION & URLS
// =============================================

$versionInfo = [
    'required_version' => '3.3.9',
    'launcher_version' => '3.3.9',

    // ⚠️ Le manifest est servi par XAMPP en local
    'manifest_url'     => 'https://azeroth-universe.eu/universe_launcher/manifest.php',

    'website_url'      => 'https://azeroth-universe.eu/',

    // ⚠️ Vérifie que ce préfixe correspond bien à la route anglaise de ton site
    // (ex. /en/register). Si ton site utilise un autre préfixe (ex. /us/register),
    // adapte simplement la ligne ci-dessous.
    'register_url'     => $lang === 'en'
        ? 'https://azeroth-universe.eu/en/register'
        : 'https://azeroth-universe.eu/fr/register',

    'server_status'    => getServerStatus(),
    'online_players'   => getOnlinePlayers(),
];

// =============================================
// STATUT SERVEUR — connexion MySQL réelle
// =============================================

function getServerStatus(): string
{
    try {
        $pdo = new PDO(
            'mysql:host=localhost:3309;dbname=auc_chars;charset=utf8',
            'root',
            'root',
            [PDO::ATTR_TIMEOUT => 2, PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]
        );
        // Vérifie si la DB répond
        $pdo->query('SELECT 1');
        return 'online';
    } catch (Exception $e) {
        return 'offline';
    }
}

function getOnlinePlayers(): int
{
    try {
        $pdo = new PDO(
            'mysql:host=localhost:3309;dbname=auc_chars;charset=utf8',
            'root',
            'root',
            [PDO::ATTR_TIMEOUT => 2, PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]
        );
        $stmt = $pdo->query("SELECT COUNT(*) FROM characters WHERE online = 1");
		return (int) $stmt->fetchColumn();
    } catch (Exception $e) {
        return 0;
    }
}

// =============================================
// RÉPONSE
// =============================================

echo json_encode([
    'success'      => true,
    'version_info' => $versionInfo,
    'news'         => $news,
    'generated_at' => date('Y-m-d H:i:s'),
], JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
