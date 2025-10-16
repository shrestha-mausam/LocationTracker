using MapKit;
using CoreLocation;
using CoreGraphics;
using LocationTracker.Controls;
using LocationTracker.Services;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace LocationTracker.Platforms.iOS.Controls;

/// <summary>
/// iOS-specific handler for the HeatmapMapControl.
/// </summary>
public class HeatmapMapHandler : MapHandler
{
    private MKMapView? _mapView;
    private HeatmapMapControl? _heatmapControl;
    private readonly List<MKPolygon> _heatmapPolygons = new();

    /// <summary>
    /// Creates the platform view for the map.
    /// </summary>
    /// <returns>The platform view.</returns>
    protected override PlatformView CreatePlatformView()
    {
        var platformView = base.CreatePlatformView();
        
        if (platformView is MKMapView mapView)
        {
            _mapView = mapView;
            ConfigureMapView();
        }
        
        return platformView;
    }

    /// <summary>
    /// Connects the handler to the virtual view.
    /// </summary>
    /// <param name="platformView">The platform view.</param>
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
    /// Configures the map view with initial settings.
    /// </summary>
    private void ConfigureMapView()
    {
        if (_mapView == null)
        {
            return;
        }

        _mapView.ShowsUserLocation = true;
        _mapView.ShowsCompass = true;
        _mapView.ShowsScale = true;
        _mapView.Delegate = new HeatmapMapDelegate(this);
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
        if (_mapView == null || _heatmapControl == null)
        {
            return;
        }

        // Remove existing heatmap polygons
        _mapView.RemoveOverlays(_heatmapPolygons.ToArray());
        _heatmapPolygons.Clear();

        if (!_heatmapControl.ShowHeatmap || 
            _heatmapControl.LocationPoints == null || 
            !_heatmapControl.LocationPoints.Any())
        {
            return;
        }

        try
        {
            // Generate heatmap polygons using the heatmap service
            var heatmapService = new HeatmapService(Microsoft.Extensions.Logging.Abstractions.NullLogger<HeatmapService>.Instance);
            var polygons = heatmapService.GenerateHeatmapAsync(_heatmapControl.LocationPoints, _heatmapControl.HeatmapRadius);
            
            foreach (var polygon in polygons)
            {
                var mkPolygon = CreateMKPolygon(polygon);
                if (mkPolygon != null)
                {
                    _heatmapPolygons.Add(mkPolygon);
                    _mapView.AddOverlay(mkPolygon);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating heatmap: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an MKPolygon from a heatmap polygon.
    /// </summary>
    /// <param name="heatmapPolygon">The heatmap polygon.</param>
    /// <returns>An MKPolygon or null if creation fails.</returns>
    private static MKPolygon? CreateMKPolygon(HeatmapPolygon heatmapPolygon)
    {
        try
        {
            var coordinates = heatmapPolygon.Points.Select(p => new CLLocationCoordinate2D(p.Latitude, p.Longitude)).ToArray();
            return MKPolygon.FromCoordinates(coordinates);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating MKPolygon: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the renderer for a polygon overlay.
    /// </summary>
    /// <param name="mapView">The map view.</param>
    /// <param name="overlay">The overlay.</param>
    /// <returns>The renderer for the overlay.</returns>
    public MKOverlayRenderer GetRendererForOverlay(MKMapView mapView, IMKOverlay overlay)
    {
        if (overlay is MKPolygon polygon)
        {
            var renderer = new MKPolygonRenderer(polygon)
            {
                FillColor = UIColor.FromRGBA(255, 0, 0, 128), // Semi-transparent red
                StrokeColor = UIColor.Red,
                LineWidth = 1.0f
            };
            return renderer;
        }

        return new MKOverlayRenderer(overlay);
    }
}

/// <summary>
/// Delegate for the heatmap map view.
/// </summary>
public class HeatmapMapDelegate : MKMapViewDelegate
{
    private readonly HeatmapMapHandler _handler;

    /// <summary>
    /// Initializes a new instance of the HeatmapMapDelegate class.
    /// </summary>
    /// <param name="handler">The map handler.</param>
    public HeatmapMapDelegate(HeatmapMapHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Called when the map view needs a renderer for an overlay.
    /// </summary>
    /// <param name="mapView">The map view.</param>
    /// <param name="overlay">The overlay.</param>
    /// <returns>The renderer for the overlay.</returns>
    public override MKOverlayRenderer GetViewForOverlay(MKMapView mapView, IMKOverlay overlay)
    {
        return _handler.GetRendererForOverlay(mapView, overlay);
    }
}
