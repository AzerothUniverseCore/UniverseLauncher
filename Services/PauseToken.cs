namespace AzerothUniverseLauncher.Services;

/// <summary>
/// Jeton de pause "asynchrone" (l'équivalent d'un CancellationToken, mais réversible).
/// Un même PauseTokenSource peut être partagé par plusieurs téléchargements en parallèle :
/// le fait de le mettre en pause bloque immédiatement toutes les tâches qui attendent sur
/// son Token, et le fait de le reprendre les débloque toutes en même temps.
/// </summary>
public sealed class PauseTokenSource
{
    private volatile TaskCompletionSource<bool> _resumeSignal = CreateSignaledSource();

    /// <summary>Vrai si le téléchargement est actuellement en pause.</summary>
    public bool IsPaused
    {
        get => !_resumeSignal.Task.IsCompleted;
        set
        {
            if (value)
            {
                if (!_resumeSignal.Task.IsCompleted) return; // déjà en pause
                _resumeSignal = new TaskCompletionSource<bool>();
            }
            else
            {
                _resumeSignal.TrySetResult(true);
            }
        }
    }

    public PauseToken Token => new(this);

    internal Task WaitWhilePausedAsync() => _resumeSignal.Task;

    private static TaskCompletionSource<bool> CreateSignaledSource()
    {
        var tcs = new TaskCompletionSource<bool>();
        tcs.SetResult(true);
        return tcs;
    }
}

public readonly struct PauseToken
{
    private readonly PauseTokenSource? _source;

    internal PauseToken(PauseTokenSource source) => _source = source;

    /// <summary>Jeton "toujours actif" (jamais en pause), pour les opérations qui n'ont pas besoin d'être mises en pause.</summary>
    public static PauseToken None => default;

    public bool IsPaused => _source?.IsPaused ?? false;

    /// <summary>Attend tant que la source associée est en pause. Ne fait rien si le jeton n'est pas en pause (ou est PauseToken.None).</summary>
    public Task WaitWhilePausedAsync() => _source?.WaitWhilePausedAsync() ?? Task.CompletedTask;
}
