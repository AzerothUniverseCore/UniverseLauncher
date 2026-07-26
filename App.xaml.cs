using System.Windows;
using AzerothUniverseLauncher.Localization;

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
                Strings.F("msg_unexpected_error_fmt", args.Exception.Message),
                Strings.T("app_box_title"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
