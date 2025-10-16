using LocationTracker.Services;
using Microsoft.Extensions.Logging;

namespace LocationTracker;

/// <summary>
/// Main application class for the Location Tracker app.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes a new instance of the App class.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates the main window for the application.
    /// </summary>
    /// <param name="activationState">The activation state.</param>
    /// <returns>The main window.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        
        // Initialize database on startup
        _ = Task.Run(async () => await InitializeDatabaseAsync());
        
        return window;
    }

    /// <summary>
    /// Initializes the database on application startup.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task InitializeDatabaseAsync()
    {
        try
        {
            var databaseService = Application.Current?.Handler?.MauiContext?.Services?.GetService<IDatabaseService>();
            if (databaseService != null)
            {
                await databaseService.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("Database initialized successfully");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize database: {ex.Message}");
        }
    }
}