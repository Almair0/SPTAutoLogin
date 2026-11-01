using System.Windows;

namespace SPTAutoLogin;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        using Mutex mutex = new(true, "Global\\SPTAutoLogin_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            return 0;
        }

        Application app = new()
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose,
        };

        MainWindow window = new();
        app.MainWindow = window;
        window.Show();

        app.Run();
        return 0;
    }
}
