using Microsoft.Extensions.Logging;
using LocationTracker.Services;
using LocationTracker.ViewModels;
using LocationTracker.Controls;

namespace LocationTracker;

public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the MAUI application.
    /// </summary>
    /// <returns>The configured MAUI application.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IHeatmapService, HeatmapService>();

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();

        // Register Views
        builder.Services.AddTransient<MainPage>();

        // Register custom controls
        builder.Services.AddTransient<HeatmapMapControl>();

        // Register custom handlers
#if ANDROID
        builder.Services.AddTransient<Platforms.Android.Controls.HeatmapMapHandler>();
#endif

#if IOS
        builder.Services.AddTransient<Platforms.iOS.Controls.HeatmapMapHandler>();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
