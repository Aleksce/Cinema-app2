using CinemaApp.Data;
using CinemaApp.Services;
using System.Windows;

namespace CinemaApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Initialize DB structure (halls, demo user)
        using (var db = new CinemaDbContext())
        {
            DatabaseInitializer.Initialize(db);
        }

        // 2. Sync movies from TMDB in background (non-blocking)
        _ = Task.Run(async () =>
        {
            var progress = new Progress<string>(msg =>
                System.Diagnostics.Debug.WriteLine($"[TMDB] {msg}"));

            await MovieSyncService.SyncAsync(progress);
        });
    }
}
