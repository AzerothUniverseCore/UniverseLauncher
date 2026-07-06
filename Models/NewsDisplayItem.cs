namespace AzerothUniverseLauncher.Models;

/// <summary>Enveloppe une NewsItem avec des propriétés prêtes pour l'affichage XAML.</summary>
public class NewsDisplayItem
{
    public string Title { get; init; } = "";
    public string Content { get; init; } = "";
    public string Date { get; init; } = "";
    public string TypeLabel { get; init; } = "INFO";

    public static NewsDisplayItem FromNewsItem(NewsItem item)
    {
        var label = item.Type?.ToLowerInvariant() switch
        {
            "update" => "MAJ",
            "event" => "ÉVÉNEMENT",
            _ => "INFO"
        };

        return new NewsDisplayItem
        {
            Title = item.Title,
            Content = item.Content,
            Date = item.Date,
            TypeLabel = label
        };
    }
}
