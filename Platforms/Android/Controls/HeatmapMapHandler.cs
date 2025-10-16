using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using LocationTracker.Controls;
using LocationTracker.Services;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace LocationTracker.Platforms.Android.Controls;

/// <summary>
/// Android-specific handler for the HeatmapMapControl.
/// </summary>
public class HeatmapMapHandler : MapHandler
{
    private GoogleMap? _googleMap;
    private HeatmapTileProvider? _heatmapTileProvider;
    private TileOverlay? _heatmapOverlay;
    private HeatmapMapControl? _heatmapControl;

    /// <summary>
    /// Creates the platform view for the map.
    /// </summary>
    /// <returns>The platform view.</returns>
    protected override PlatformView CreatePlatformView()
    {
        var platformView = base.CreatePlatformView();
        
        // Get the GoogleMap instance when it's ready
        if (platformView is MapFragment mapFragment)
        {
            mapFragment.GetMapAsync(new MapReadyCallback(this));
        }
        
        return platformView;
    }

    /// <summary>
    /// Connects the handler to the virtual view.
    /// </summary>
    /// <param name="virtualView">The virtual view.</param>
    protected override void ConnectHandler(PlatformView platformView)
    {
        base.ConnectHandler(platformView);
        
        if (VirtualView is HeatmapMapControl heatmapControl)
        {
            _heatmapControl = heatmapControl;
            _heatmapControl.PropertyChanged += OnHeatmapControlPropertyChanged;
        }
    }

    /// <summary>
    /// Disconnects the handler from the virtual view.
    /// </summary>
    /// <param name="platformView">The platform view.</param>
    protected override void DisconnectHandler(PlatformView platformView)
    {
        if (_heatmapControl != null)
        {
            _heatmapControl.PropertyChanged -= OnHeatmapControlPropertyChanged;
            _heatmapControl = null;
        }
        
        base.DisconnectHandler(platformView);
    }

    /// <summary>
    /// Called when the GoogleMap is ready.
    /// </summary>
    /// <param name="googleMap">The GoogleMap instance.</param>
    public void OnMapReady(GoogleMap googleMap)
    {
        _googleMap = googleMap;
        
        // Configure map settings
        _googleMap.UiSettings.ZoomControlsEnabled = true;
        _googleMap.UiSettings.MyLocationButtonEnabled = true;
        _googleMap.UiSettings.CompassEnabled = true;
        
        // Update heatmap if control is ready
        UpdateHeatmap();
    }

    /// <summary>
    /// Handles property changes on the heatmap control.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnHeatmapControlPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HeatmapMapControl.LocationPoints) ||
            e.PropertyName == nameof(HeatmapMapControl.ShowHeatmap) ||
            e.PropertyName == nameof(HeatmapMapControl.HeatmapRadius))
        {
            UpdateHeatmap();
        }
    }

    /// <summary>
    /// Updates the heatmap overlay on the map.
    /// </summary>
    private void UpdateHeatmap()
    {
        if (_googleMap == null || _heatmapControl == null)
        {
            return;
        }

        // Remove existing heatmap overlay
        if (_heatmapOverlay != null)
        {
            _heatmapOverlay.Remove();
            _heatmapOverlay = null;
        }

        if (!_heatmapControl.ShowHeatmap || 
            _heatmapControl.LocationPoints == null || 
            !_heatmapControl.LocationPoints.Any())
        {
            return;
        }

        try
        {
            // Create heatmap data points
            var heatmapData = CreateHeatmapData(_heatmapControl.LocationPoints);
            
            if (heatmapData.Any())
            {
                // Create heatmap tile provider
                _heatmapTileProvider = new HeatmapTileProvider.Builder()
                    .Data(heatmapData)
                    .Radius(50) // Heatmap radius in pixels
                    .Gradient(CreateHeatmapGradient())
                    .Build();

                // Create tile overlay
                var tileOverlayOptions = new TileOverlayOptions()
                    .TileProvider(_heatmapTileProvider)
                    .Transparency(0.4f);

                _heatmapOverlay = _googleMap.AddTileOverlay(tileOverlayOptions);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating heatmap: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates heatmap data points from location points.
    /// </summary>
    /// <param name="locationPoints">The location points.</param>
    /// <returns>A collection of LatLng points.</returns>
    private static IEnumerable<LatLng> CreateHeatmapData(IEnumerable<LocationTracker.Models.LocationPoint> locationPoints)
    {
        return locationPoints.Select(lp => new LatLng(lp.Latitude, lp.Longitude)).ToList();
    }

    /// <summary>
    /// Creates a gradient for the heatmap.
    /// </summary>
    /// <returns>A gradient configuration.</returns>
    private static Gradient CreateHeatmapGradient()
    {
        var colors = new int[]
        {
            Android.Graphics.Color.Argb(0, 0, 0, 255),     // Blue (transparent)
            Android.Graphics.Color.Argb(128, 0, 255, 255), // Cyan
            Android.Graphics.Color.Argb(128, 0, 255, 0),   // Green
            Android.Graphics.Color.Argb(128, 255, 255, 0), // Yellow
            Android.Graphics.Color.Argb(128, 255, 0, 0)    // Red
        };

        var startPoints = new float[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };

        return new Gradient(colors, startPoints);
    }
}

/// <summary>
/// Callback for when the GoogleMap is ready.
/// </summary>
public class MapReadyCallback : Java.Lang.Object, IOnMapReadyCallback
{
    private readonly HeatmapMapHandler _handler;

    /// <summary>
    /// Initializes a new instance of the MapReadyCallback class.
    /// </summary>
    /// <param name="handler">The map handler.</param>
    public MapReadyCallback(HeatmapMapHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Called when the map is ready.
    /// </summary>
    /// <param name="googleMap">The GoogleMap instance.</param>
    public void OnMapReady(GoogleMap googleMap)
    {
        _handler.OnMapReady(googleMap);
    }
}
