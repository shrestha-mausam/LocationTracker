using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using LocationTracker.Models;
using LocationTracker.Services;

namespace LocationTracker.Controls;

/// <summary>
/// A custom map control that extends the MAUI Map to display heatmap overlays.
/// </summary>
public class HeatmapMapControl : Microsoft.Maui.Controls.Maps.Map
{
    /// <summary>
    /// Bindable property for location points.
    /// </summary>
    public static readonly BindableProperty LocationPointsProperty = BindableProperty.Create(
        nameof(LocationPoints),
        typeof(IEnumerable<LocationPoint>),
        typeof(HeatmapMapControl),
        default(IEnumerable<LocationPoint>),
        propertyChanged: OnLocationPointsChanged);

    /// <summary>
    /// Bindable property for showing the heatmap overlay.
    /// </summary>
    public static readonly BindableProperty ShowHeatmapProperty = BindableProperty.Create(
        nameof(ShowHeatmap),
        typeof(bool),
        typeof(HeatmapMapControl),
        true,
        propertyChanged: OnShowHeatmapChanged);

    /// <summary>
    /// Bindable property for the heatmap radius.
    /// </summary>
    public static readonly BindableProperty HeatmapRadiusProperty = BindableProperty.Create(
        nameof(HeatmapRadius),
        typeof(double),
        typeof(HeatmapMapControl),
        50.0,
        propertyChanged: OnHeatmapRadiusChanged);

    /// <summary>
    /// Gets or sets the collection of location points to display on the heatmap.
    /// </summary>
    public IEnumerable<LocationPoint> LocationPoints
    {
        get => (IEnumerable<LocationPoint>)GetValue(LocationPointsProperty);
        set => SetValue(LocationPointsProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show the heatmap overlay.
    /// </summary>
    public bool ShowHeatmap
    {
        get => (bool)GetValue(ShowHeatmapProperty);
        set => SetValue(ShowHeatmapProperty, value);
    }

    /// <summary>
    /// Gets or sets the radius for heatmap density calculation in meters.
    /// </summary>
    public double HeatmapRadius
    {
        get => (double)GetValue(HeatmapRadiusProperty);
        set => SetValue(HeatmapRadiusProperty, value);
    }

    /// <summary>
    /// Event that is raised when the map is ready.
    /// </summary>
    public event EventHandler? MapReady;

    /// <summary>
    /// Initializes a new instance of the HeatmapMapControl class.
    /// </summary>
    public HeatmapMapControl()
    {
        // Set default map properties
        MapType = MapType.Street;
        IsShowingUser = true;
        
        // Add event handlers
        Loaded += OnMapLoaded;
    }

    /// <summary>
    /// Centers the map on the specified location.
    /// </summary>
    /// <param name="location">The location to center on.</param>
    /// <param name="zoomLevel">The zoom level (optional).</param>
    public void CenterOnLocation(LocationPoint location, double zoomLevel = 0.01)
    {
        var mapLocation = new Location(location.Latitude, location.Longitude);
        MoveToRegion(MapSpan.FromCenterAndRadius(mapLocation, Distance.FromKilometers(zoomLevel)));
    }

    /// <summary>
    /// Centers the map on the collection of location points.
    /// </summary>
    /// <param name="locations">The locations to center on.</param>
    /// <param name="padding">The padding around the locations.</param>
    public void CenterOnLocations(IEnumerable<LocationPoint> locations, double padding = 0.01)
    {
        var locationList = locations.ToList();
        if (!locationList.Any())
        {
            return;
        }

        var minLat = locationList.Min(l => l.Latitude);
        var maxLat = locationList.Max(l => l.Latitude);
        var minLng = locationList.Min(l => l.Longitude);
        var maxLng = locationList.Max(l => l.Longitude);

        var centerLat = (minLat + maxLat) / 2;
        var centerLng = (minLng + maxLng) / 2;
        
        var latSpan = Math.Max(maxLat - minLat, 0.001) + padding;
        var lngSpan = Math.Max(maxLng - minLng, 0.001) + padding;
        
        var center = new Location(centerLat, centerLng);
        var span = new MapSpan(center, latSpan, lngSpan);
        
        MoveToRegion(span);
    }

    /// <summary>
    /// Adds a pin to the map.
    /// </summary>
    /// <param name="location">The location for the pin.</param>
    /// <param name="label">The label for the pin.</param>
    /// <param name="address">The address for the pin.</param>
    /// <returns>The created pin.</returns>
    public Pin AddPin(LocationPoint location, string label = "", string address = "")
    {
        var pin = new Pin
        {
            Location = new Location(location.Latitude, location.Longitude),
            Label = string.IsNullOrEmpty(label) ? $"Location {location.Id}" : label,
            Address = string.IsNullOrEmpty(address) ? location.ToString() : address
        };

        Pins.Add(pin);
        return pin;
    }

    /// <summary>
    /// Clears all pins from the map.
    /// </summary>
    public void ClearPins()
    {
        Pins.Clear();
    }

    /// <summary>
    /// Updates the heatmap display based on current location points.
    /// </summary>
    public void UpdateHeatmap()
    {
        if (!ShowHeatmap || LocationPoints == null || !LocationPoints.Any())
        {
            return;
        }

        // This will be handled by the platform-specific renderers
        // The actual heatmap rendering is implemented in the handlers
        OnPropertyChanged(nameof(LocationPoints));
    }

    /// <summary>
    /// Handles the map loaded event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnMapLoaded(object? sender, EventArgs e)
    {
        MapReady?.Invoke(this, EventArgs.Empty);
        
        // Center on location points if available
        if (LocationPoints != null && LocationPoints.Any())
        {
            CenterOnLocations(LocationPoints);
        }
    }

    /// <summary>
    /// Handles changes to the LocationPoints property.
    /// </summary>
    /// <param name="bindable">The bindable object.</param>
    /// <param name="oldValue">The old value.</param>
    /// <param name="newValue">The new value.</param>
    private static void OnLocationPointsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is HeatmapMapControl control)
        {
            control.OnLocationPointsChanged();
        }
    }

    /// <summary>
    /// Handles changes to the ShowHeatmap property.
    /// </summary>
    /// <param name="bindable">The bindable object.</param>
    /// <param name="oldValue">The old value.</param>
    /// <param name="newValue">The new value.</param>
    private static void OnShowHeatmapChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is HeatmapMapControl control)
        {
            control.OnShowHeatmapChanged();
        }
    }

    /// <summary>
    /// Handles changes to the HeatmapRadius property.
    /// </summary>
    /// <param name="bindable">The bindable object.</param>
    /// <param name="oldValue">The old value.</param>
    /// <param name="newValue">The new value.</param>
    private static void OnHeatmapRadiusChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is HeatmapMapControl control)
        {
            control.OnHeatmapRadiusChanged();
        }
    }

    /// <summary>
    /// Called when the LocationPoints property changes.
    /// </summary>
    private void OnLocationPointsChanged()
    {
        UpdateHeatmap();
        
        // Center map on new locations
        if (LocationPoints != null && LocationPoints.Any())
        {
            CenterOnLocations(LocationPoints);
        }
    }

    /// <summary>
    /// Called when the ShowHeatmap property changes.
    /// </summary>
    private void OnShowHeatmapChanged()
    {
        UpdateHeatmap();
    }

    /// <summary>
    /// Called when the HeatmapRadius property changes.
    /// </summary>
    private void OnHeatmapRadiusChanged()
    {
        UpdateHeatmap();
    }
}
