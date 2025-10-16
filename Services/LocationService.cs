using Microsoft.Maui.Devices.Sensors;
using Microsoft.Extensions.Logging;
using LocationTracker.Models;
using System.Collections.Concurrent;

namespace LocationTracker.Services;

/// <summary>
/// Service for tracking user location with continuous GPS monitoring.
/// </summary>
public class LocationService : ILocationService
{
    private readonly ILogger<LocationService> _logger;
    private readonly IDatabaseService _databaseService;
    private readonly ConcurrentQueue<LocationPoint> _locationBuffer;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isTracking;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the LocationService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="databaseService">The database service instance.</param>
    public LocationService(ILogger<LocationService> logger, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
        _locationBuffer = new ConcurrentQueue<LocationPoint>();
    }

    /// <summary>
    /// Gets a value indicating whether location tracking is currently active.
    /// </summary>
    public bool IsTracking
    {
        get
        {
            lock (_lockObject)
            {
                return _isTracking;
            }
        }
    }

    /// <summary>
    /// Event that is raised when a new location is received.
    /// </summary>
    public event EventHandler<LocationEventArgs>? LocationReceived;

    /// <summary>
    /// Event that is raised when location tracking status changes.
    /// </summary>
    public event EventHandler<TrackingStatusEventArgs>? TrackingStatusChanged;

    /// <summary>
    /// Starts continuous location tracking.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<bool> StartTrackingAsync()
    {
        lock (_lockObject)
        {
            if (_isTracking)
            {
                _logger.LogWarning("Location tracking is already active");
                return true;
            }
        }

        try
        {
            // Request location permissions
            var hasPermission = await RequestLocationPermissionAsync();
            if (!hasPermission)
            {
                _logger.LogError("Location permission denied");
                return false;
            }

            // Start tracking
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(async () => await TrackLocationAsync(_cancellationTokenSource.Token));

            lock (_lockObject)
            {
                _isTracking = true;
            }

            OnTrackingStatusChanged(true);
            _logger.LogInformation("Location tracking started");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start location tracking");
            OnTrackingStatusChanged(false);
            return false;
        }
    }

    /// <summary>
    /// Stops location tracking.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StopTrackingAsync()
    {
        lock (_lockObject)
        {
            if (!_isTracking)
            {
                _logger.LogWarning("Location tracking is not active");
                return;
            }

            _isTracking = false;
        }

        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            // Save any remaining buffered locations
            await SaveBufferedLocationsAsync();

            OnTrackingStatusChanged(false);
            _logger.LogInformation("Location tracking stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping location tracking");
        }
    }

    /// <summary>
    /// Gets the current location once.
    /// </summary>
    /// <returns>A task that returns the current location, or null if unavailable.</returns>
    public async Task<LocationPoint?> GetCurrentLocationAsync()
    {
        try
        {
            var hasPermission = await RequestLocationPermissionAsync();
            if (!hasPermission)
            {
                _logger.LogWarning("Location permission denied for current location request");
                return null;
            }

            var request = new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.High,
                Timeout = TimeSpan.FromSeconds(10)
            };

            var location = await Geolocation.GetLocationAsync(request);
            if (location != null)
            {
                var locationPoint = LocationPoint.FromLocation(location);
                _logger.LogDebug("Retrieved current location: {LocationPoint}", locationPoint);
                return locationPoint;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current location");
            return null;
        }
    }

    /// <summary>
    /// Requests location permissions from the user.
    /// </summary>
    /// <returns>A task that returns true if permission is granted.</returns>
    private async Task<bool> RequestLocationPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted)
            {
                return true;
            }

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // On iOS, a user can deny permission but not be permanently denied.
                // We can try to request again.
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                return true;
            }

            // Request background location permission if needed
            if (status == PermissionStatus.Granted)
            {
                var backgroundStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
                if (backgroundStatus != PermissionStatus.Granted)
                {
                    backgroundStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
                }
            }

            return status == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting location permission");
            return false;
        }
    }

    /// <summary>
    /// Continuously tracks location while the cancellation token is not cancelled.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task TrackLocationAsync(CancellationToken cancellationToken)
    {
        var request = new GeolocationRequest
        {
            DesiredAccuracy = GeolocationAccuracy.High,
            Timeout = TimeSpan.FromSeconds(10)
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var location = await Geolocation.GetLocationAsync(request, cancellationToken);
                if (location != null)
                {
                    var locationPoint = LocationPoint.FromLocation(location);
                    await ProcessLocationUpdateAsync(locationPoint);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping tracking
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during location tracking");
            }

            // Wait before next location request
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
    }

    /// <summary>
    /// Processes a location update by adding it to the buffer and raising events.
    /// </summary>
    /// <param name="locationPoint">The location point to process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ProcessLocationUpdateAsync(LocationPoint locationPoint)
    {
        try
        {
            // Add to buffer for batch saving
            _locationBuffer.Enqueue(locationPoint);

            // Raise event
            OnLocationReceived(locationPoint);

            // Save to database periodically (every 10 locations or every 30 seconds)
            if (_locationBuffer.Count >= 10)
            {
                await SaveBufferedLocationsAsync();
            }

            _logger.LogDebug("Processed location update: {LocationPoint}", locationPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing location update");
        }
    }

    /// <summary>
    /// Saves all buffered locations to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SaveBufferedLocationsAsync()
    {
        try
        {
            if (_locationBuffer.IsEmpty)
            {
                return;
            }

            var locationsToSave = new List<LocationPoint>();
            while (_locationBuffer.TryDequeue(out var location))
            {
                locationsToSave.Add(location);
            }

            if (locationsToSave.Any())
            {
                await _databaseService.SaveLocationsAsync(locationsToSave);
                _logger.LogDebug("Saved {Count} buffered locations to database", locationsToSave.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving buffered locations");
        }
    }

    /// <summary>
    /// Raises the LocationReceived event.
    /// </summary>
    /// <param name="locationPoint">The location point.</param>
    private void OnLocationReceived(LocationPoint locationPoint)
    {
        LocationReceived?.Invoke(this, new LocationEventArgs(locationPoint));
    }

    /// <summary>
    /// Raises the TrackingStatusChanged event.
    /// </summary>
    /// <param name="isTracking">Whether tracking is active.</param>
    private void OnTrackingStatusChanged(bool isTracking)
    {
        TrackingStatusChanged?.Invoke(this, new TrackingStatusEventArgs(isTracking));
    }

    /// <summary>
    /// Disposes the location service.
    /// </summary>
    public void Dispose()
    {
        _ = Task.Run(async () => await StopTrackingAsync());
        _cancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// Interface for location tracking operations.
/// </summary>
public interface ILocationService : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether location tracking is currently active.
    /// </summary>
    bool IsTracking { get; }

    /// <summary>
    /// Event that is raised when a new location is received.
    /// </summary>
    event EventHandler<LocationEventArgs>? LocationReceived;

    /// <summary>
    /// Event that is raised when location tracking status changes.
    /// </summary>
    event EventHandler<TrackingStatusEventArgs>? TrackingStatusChanged;

    /// <summary>
    /// Starts continuous location tracking.
    /// </summary>
    /// <returns>A task that returns true if tracking started successfully.</returns>
    Task<bool> StartTrackingAsync();

    /// <summary>
    /// Stops location tracking.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopTrackingAsync();

    /// <summary>
    /// Gets the current location once.
    /// </summary>
    /// <returns>A task that returns the current location, or null if unavailable.</returns>
    Task<LocationPoint?> GetCurrentLocationAsync();
}

/// <summary>
/// Event arguments for location received events.
/// </summary>
public class LocationEventArgs : EventArgs
{
    /// <summary>
    /// Gets the location point.
    /// </summary>
    public LocationPoint LocationPoint { get; }

    /// <summary>
    /// Initializes a new instance of the LocationEventArgs class.
    /// </summary>
    /// <param name="locationPoint">The location point.</param>
    public LocationEventArgs(LocationPoint locationPoint)
    {
        LocationPoint = locationPoint;
    }
}

/// <summary>
/// Event arguments for tracking status changed events.
/// </summary>
public class TrackingStatusEventArgs : EventArgs
{
    /// <summary>
    /// Gets a value indicating whether tracking is active.
    /// </summary>
    public bool IsTracking { get; }

    /// <summary>
    /// Initializes a new instance of the TrackingStatusEventArgs class.
    /// </summary>
    /// <param name="isTracking">Whether tracking is active.</param>
    public TrackingStatusEventArgs(bool isTracking)
    {
        IsTracking = isTracking;
    }
}
