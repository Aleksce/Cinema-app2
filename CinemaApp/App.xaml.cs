using CinemaApp.Data;
using System.Windows;

namespace CinemaApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        using var db = new CinemaDbContext();
        DatabaseInitializer.Initialize(db);
    }
}
