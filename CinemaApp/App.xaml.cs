using CinemaApp.Data;
using CinemaApp.Services;
using CinemaApp.ViewModels;
using System.Windows;

namespace CinemaApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Initialize DB structure synchronously (fast — just EnsureCreated + schema patches)
        using (var db = new CinemaDbContext())
        {
            DatabaseInitializer.Initialize(db);
        }

        // 2. After window is shown, sync TMDB in background then refresh UI
        Dispatcher.InvokeAsync(async () =>
        {
            // Wait for MainWindow to fully render
            await Task.Delay(500);

            var progress = new Progress<string>(msg =>
            {
                // Update SyncStatus on MoviesVm so user sees progress
                if (MainWindow?.DataContext is MainViewModel main)
                    main.MoviesVm.SyncStatus = msg;

                System.Diagnostics.Debug.WriteLine($"[TMDB] {msg}");
            });

            await Task.Run(async () => await MovieSyncService.SyncAsync(progress));

            // 3. Refresh movies list on UI thread after sync
            if (MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.MoviesVm.SyncStatus = string.Empty;
                await mainVm.MoviesVm.LoadAsync();

                // Also refresh schedule if currently visible
                if (mainVm.CurrentPage == AppPage.Schedule)
                    mainVm.ScheduleVm.Load();
            }
        });
    }
}
