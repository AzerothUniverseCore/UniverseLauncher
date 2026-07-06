using System.Windows;

namespace AzerothUniverseLauncher;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Filet de sécurité global : une erreur inattendue ne doit jamais
        // faire planter le launcher sans explication pour l'utilisateur.
        DispatcherUnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(
                "Une erreur inattendue est survenue :\n\n" + args.Exception.Message,
                "Azeroth Universe Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
