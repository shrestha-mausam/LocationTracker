using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocationTracker.Models;
using LocationTracker.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace LocationTracker.ViewModels;

/// <summary>
/// ViewModel for the main page of the location tracker application.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILocationService _locationService;
    private readonly IDatabaseService _databaseService;
    private readonly IHeatmapService _heatmapService;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private bool _isTracking;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _trackingStatusText = "Ready to track";

    [ObservableProperty]
    private int _locationCount;

    [ObservableProperty]
    private bool _canClearData;

    [ObservableProperty]
    private ObservableCollection<LocationPoint> _locationPoints = new();

    [ObservableProperty]
    private LocationPoint? _currentLocation;

    [ObservableProperty]
    private double _heatmapRadius = 50.0;

    [ObservableProperty]
    private bool _showHeatmap = true;

    /// <summary>
    /// Initializes a new instance of the MainViewModel class.
    /// </summary>
    /// <param name="locationService">The location service.</param>
    /// <param name="databaseService">The database service.</param>
    /// <param name="heatmapService">The heatmap service.</param>
    /// <param name="logger">The logger.</param>
    public MainViewModel(
        ILocationService locationService,
        IDatabaseService databaseService,
        IHeatmapService heatmapService,
        ILogger<MainViewModel> logger)
    {
        _locationService = locationService;
        _databaseService = databaseService;
        _heatmapService = heatmapService;
        _logger = logger;

        // Subscribe to location service events
        _locationService.LocationReceived += OnLocationReceived;
        _locationService.TrackingStatusChanged += OnTrackingStatusChanged;

        // Initialize the view model
        _ = Task.Run(async () => await InitializeAsync());
    }

    /// <summary>
    /// Command to start or stop location tracking.
    /// </summary>
    [RelayCommand]
    private async Task ToggleTrackingAsync()
    {
        try
        {
            IsLoading = true;

            if (IsTracking)
            {
                await _locationService.StopTrackingAsync();
                _logger.LogInformation("Location tracking stopped by user");
            }
            else
            {
                var success = await _locationService.StartTrackingAsync();
                if (success)
                {
                    _logger.LogInformation("Location tracking started by user");
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert(
                        "Permission Required",
                        "Location permission is required to track your movement. Please enable location access in your device settings.",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling location tracking");
            await Application.Current!.MainPage!.DisplayAlert(
                "Error",
                "An error occurred while starting/stopping location tracking.",
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Command to clear all location data.
    /// </summary>
    [RelayCommand]
    private async Task ClearDataAsync()
    {
        try
        {
            var result = await Application.Current!.MainPage!.DisplayAlert(
                "Clear All Data",
                "Are you sure you want to delete all location data? This action cannot be undone.",
                "Yes, Clear All",
                "Cancel");

            if (result)
            {
                IsLoading = true;
                
                await _databaseService.ClearAllLocationsAsync();
                LocationPoints.Clear();
                LocationCount = 0;
                CanClearData = false;
                
                _logger.LogInformation("All location data cleared by user");
                
                await Application.Current.MainPage.DisplayAlert(
                    "Data Cleared",
                    "All location data has been successfully deleted.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing location data");
            await Application.Current!.MainPage!.DisplayAlert(
                "Error",
                "An error occurred while clearing location data.",
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Command to get the current location.
    /// </summary>
    [RelayCommand]
    private async Task GetCurrentLocationAsync()
    {
        try
        {
            IsLoading = true;
            TrackingStatusText = "Getting current location...";

            var location = await _locationService.GetCurrentLocationAsync();
            if (location != null)
            {
                CurrentLocation = location;
                TrackingStatusText = $"Current location: {location.Latitude:F6}, {location.Longitude:F6}";
                _logger.LogInformation("Retrieved current location: {Location}", location);
            }
            else
            {
                TrackingStatusText = "Unable to get current location";
                await Application.Current!.MainPage!.DisplayAlert(
                    "Location Unavailable",
                    "Unable to get your current location. Please check your location settings.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current location");
            TrackingStatusText = "Error getting current location";
            await Application.Current!.MainPage!.DisplayAlert(
                "Error",
                "An error occurred while getting your current location.",
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Command to toggle heatmap visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleHeatmap()
    {
        ShowHeatmap = !ShowHeatmap;
        TrackingStatusText = ShowHeatmap ? "Heatmap visible" : "Heatmap hidden";
        _logger.LogInformation("Heatmap visibility toggled: {ShowHeatmap}", ShowHeatmap);
    }

    /// <summary>
    /// Initializes the view model.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            TrackingStatusText = "Initializing...";

            // Load existing location data
            await LoadLocationDataAsync();

            // Get current location
            await GetCurrentLocationAsync();

            TrackingStatusText = "Ready to track";
            _logger.LogInformation("MainViewModel initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MainViewModel");
            TrackingStatusText = "Initialization failed";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads existing location data from the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadLocationDataAsync()
    {
        try
        {
            var locations = await _databaseService.GetAllLocationsAsync();
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                LocationPoints.Clear();
                foreach (var location in locations)
                {
                    LocationPoints.Add(location);
                }
                LocationCount = LocationPoints.Count;
                CanClearData = LocationCount > 0;
            });

            _logger.LogInformation("Loaded {Count} location points from database", LocationCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading location data");
        }
    }

    /// <summary>
    /// Handles location received events from the location service.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private async void OnLocationReceived(object? sender, LocationEventArgs e)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                LocationPoints.Add(e.LocationPoint);
                LocationCount = LocationPoints.Count;
                CanClearData = LocationCount > 0;
                CurrentLocation = e.LocationPoint;
                TrackingStatusText = $"Tracking... {LocationCount} points recorded";
            });

            // Save to database asynchronously
            await _databaseService.SaveLocationAsync(e.LocationPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling location received event");
        }
    }

    /// <summary>
    /// Handles tracking status changed events from the location service.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnTrackingStatusChanged(object? sender, TrackingStatusEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsTracking = e.IsTracking;
            TrackingStatusText = e.IsTracking ? "Tracking active" : "Tracking stopped";
        });
    }

    /// <summary>
    /// Updates the heatmap radius and triggers a refresh.
    /// </summary>
    /// <param name="value">The new radius value.</param>
    partial void OnHeatmapRadiusChanged(double value)
    {
        _logger.LogDebug("Heatmap radius changed to {Radius}", value);
    }

    /// <summary>
    /// Updates the heatmap visibility and triggers a refresh.
    /// </summary>
    /// <param name="value">The new visibility value.</param>
    partial void OnShowHeatmapChanged(bool value)
    {
        TrackingStatusText = value ? "Heatmap visible" : "Heatmap hidden";
        _logger.LogDebug("Heatmap visibility changed to {ShowHeatmap}", value);
    }

    /// <summary>
    /// Disposes the view model.
    /// </summary>
    public void Dispose()
    {
        _locationService.LocationReceived -= OnLocationReceived;
        _locationService.TrackingStatusChanged -= OnTrackingStatusChanged;
    }
}
