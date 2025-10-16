using Microsoft.Extensions.Logging;
using LocationTracker.Models;
using Microsoft.Maui.Maps;

namespace LocationTracker.Services;

/// <summary>
/// Service for generating heatmap data from location points.
/// </summary>
public class HeatmapService : IHeatmapService
{
    private readonly ILogger<HeatmapService> _logger;
    private const double DefaultRadius = 50.0; // meters
    private const int DefaultGridSize = 256;

    /// <summary>
    /// Initializes a new instance of the HeatmapService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public HeatmapService(ILogger<HeatmapService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates heatmap data from a collection of location points.
    /// </summary>
    /// <param name="locationPoints">The location points to process.</param>
    /// <param name="radius">The radius for density calculation in meters.</param>
    /// <returns>A collection of heatmap polygons.</returns>
    public IEnumerable<HeatmapPolygon> GenerateHeatmapAsync(IEnumerable<LocationPoint> locationPoints, double radius = DefaultRadius)
    {
        try
        {
            var locations = locationPoints.ToList();
            if (!locations.Any())
            {
                _logger.LogWarning("No location points provided for heatmap generation");
                return Enumerable.Empty<HeatmapPolygon>();
            }

            _logger.LogInformation("Generating heatmap from {Count} location points with radius {Radius}m", 
                locations.Count, radius);

            // Calculate bounds
            var bounds = CalculateBounds(locations);
            
            // Create grid
            var grid = CreateDensityGrid(locations, bounds, radius);
            
            // Generate polygons
            var polygons = GeneratePolygons(grid, bounds, radius);
            
            _logger.LogInformation("Generated {Count} heatmap polygons", polygons.Count());
            return polygons;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating heatmap");
            return Enumerable.Empty<HeatmapPolygon>();
        }
    }

    /// <summary>
    /// Calculates the geographical bounds of the location points.
    /// </summary>
    /// <param name="locations">The location points.</param>
    /// <returns>The geographical bounds.</returns>
    private static Bounds CalculateBounds(IEnumerable<LocationPoint> locations)
    {
        var locationList = locations.ToList();
        var minLat = locationList.Min(l => l.Latitude);
        var maxLat = locationList.Max(l => l.Latitude);
        var minLng = locationList.Min(l => l.Longitude);
        var maxLng = locationList.Max(l => l.Longitude);

        // Add padding to bounds
        var latPadding = (maxLat - minLat) * 0.1;
        var lngPadding = (maxLng - minLng) * 0.1;

        return new Bounds
        {
            MinLatitude = minLat - latPadding,
            MaxLatitude = maxLat + latPadding,
            MinLongitude = minLng - lngPadding,
            MaxLongitude = maxLng + lngPadding
        };
    }

    /// <summary>
    /// Creates a density grid from location points.
    /// </summary>
    /// <param name="locations">The location points.</param>
    /// <param name="bounds">The geographical bounds.</param>
    /// <param name="radius">The radius for density calculation.</param>
    /// <returns>A 2D array representing density values.</returns>
    private double[,] CreateDensityGrid(IEnumerable<LocationPoint> locations, Bounds bounds, double radius)
    {
        var locationList = locations.ToList();
        var grid = new double[DefaultGridSize, DefaultGridSize];
        
        var latStep = (bounds.MaxLatitude - bounds.MinLatitude) / DefaultGridSize;
        var lngStep = (bounds.MaxLongitude - bounds.MinLongitude) / DefaultGridSize;

        // Calculate density for each grid cell
        for (var i = 0; i < DefaultGridSize; i++)
        {
            for (var j = 0; j < DefaultGridSize; j++)
            {
                var cellLat = bounds.MinLatitude + (i * latStep);
                var cellLng = bounds.MinLongitude + (j * lngStep);
                
                var density = CalculateDensityAtPoint(locationList, cellLat, cellLng, radius);
                grid[i, j] = density;
            }
        }

        return grid;
    }

    /// <summary>
    /// Calculates density at a specific geographical point.
    /// </summary>
    /// <param name="locations">The location points.</param>
    /// <param name="lat">The latitude.</param>
    /// <param name="lng">The longitude.</param>
    /// <param name="radius">The radius for density calculation.</param>
    /// <returns>The density value.</returns>
    private static double CalculateDensityAtPoint(IEnumerable<LocationPoint> locations, double lat, double lng, double radius)
    {
        var density = 0.0;
        
        foreach (var location in locations)
        {
            var distance = CalculateDistance(lat, lng, location.Latitude, location.Longitude);
            if (distance <= radius)
            {
                // Use Gaussian kernel for smooth density distribution
                var weight = Math.Exp(-(distance * distance) / (2 * radius * radius / 4));
                density += weight;
            }
        }
        
        return density;
    }

    /// <summary>
    /// Calculates the distance between two geographical points using the Haversine formula.
    /// </summary>
    /// <param name="lat1">The first latitude.</param>
    /// <param name="lng1">The first longitude.</param>
    /// <param name="lat2">The second latitude.</param>
    /// <param name="lng2">The second longitude.</param>
    /// <returns>The distance in meters.</returns>
    private static double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadius = 6371000; // Earth's radius in meters
        
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return earthRadius * c;
    }

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    /// <summary>
    /// Generates heatmap polygons from the density grid.
    /// </summary>
    /// <param name="grid">The density grid.</param>
    /// <param name="bounds">The geographical bounds.</param>
    /// <param name="radius">The radius used for density calculation.</param>
    /// <returns>A collection of heatmap polygons.</returns>
    private IEnumerable<HeatmapPolygon> GeneratePolygons(double[,] grid, Bounds bounds, double radius)
    {
        var polygons = new List<HeatmapPolygon>();
        var maxDensity = FindMaxDensity(grid);
        
        if (maxDensity <= 0)
        {
            return polygons;
        }

        var latStep = (bounds.MaxLatitude - bounds.MinLatitude) / DefaultGridSize;
        var lngStep = (bounds.MaxLongitude - bounds.MinLongitude) / DefaultGridSize;

        // Generate polygons for cells with significant density
        for (var i = 0; i < DefaultGridSize - 1; i++)
        {
            for (var j = 0; j < DefaultGridSize - 1; j++)
            {
                var density = grid[i, j];
                if (density > maxDensity * 0.05) // Only show areas with >5% of max density
                {
                    var polygon = CreatePolygonFromGridCell(i, j, density, maxDensity, bounds, latStep, lngStep);
                    polygons.Add(polygon);
                }
            }
        }

        return polygons.OrderBy(p => p.Density).ToList();
    }

    /// <summary>
    /// Finds the maximum density value in the grid.
    /// </summary>
    /// <param name="grid">The density grid.</param>
    /// <returns>The maximum density value.</returns>
    private static double FindMaxDensity(double[,] grid)
    {
        var maxDensity = 0.0;
        for (var i = 0; i < DefaultGridSize; i++)
        {
            for (var j = 0; j < DefaultGridSize; j++)
            {
                if (grid[i, j] > maxDensity)
                {
                    maxDensity = grid[i, j];
                }
            }
        }
        return maxDensity;
    }

    /// <summary>
    /// Creates a heatmap polygon from a grid cell.
    /// </summary>
    /// <param name="i">The row index.</param>
    /// <param name="j">The column index.</param>
    /// <param name="density">The density value.</param>
    /// <param name="maxDensity">The maximum density value.</param>
    /// <param name="bounds">The geographical bounds.</param>
    /// <param name="latStep">The latitude step size.</param>
    /// <param name="lngStep">The longitude step size.</param>
    /// <returns>A heatmap polygon.</returns>
    private static HeatmapPolygon CreatePolygonFromGridCell(int i, int j, double density, double maxDensity, 
        Bounds bounds, double latStep, double lngStep)
    {
        var lat1 = bounds.MinLatitude + (i * latStep);
        var lng1 = bounds.MinLongitude + (j * lngStep);
        var lat2 = bounds.MinLatitude + ((i + 1) * latStep);
        var lng2 = bounds.MinLongitude + ((j + 1) * lngStep);

        var polygon = new HeatmapPolygon
        {
            Density = density,
            Intensity = density / maxDensity,
            Color = GetHeatmapColor(density / maxDensity),
            Points = new[]
            {
                new Location(lat1, lng1),
                new Location(lat1, lng2),
                new Location(lat2, lng2),
                new Location(lat2, lng1)
            }
        };

        return polygon;
    }

    /// <summary>
    /// Gets the color for a given intensity value.
    /// </summary>
    /// <param name="intensity">The intensity value (0.0 to 1.0).</param>
    /// <returns>A color representing the intensity.</returns>
    private static Color GetHeatmapColor(double intensity)
    {
        // Clamp intensity between 0 and 1
        intensity = Math.Max(0.0, Math.Min(1.0, intensity));

        // Color scale: Blue (low) -> Green -> Yellow -> Red (high)
        if (intensity < 0.25)
        {
            // Blue to Cyan
            var t = intensity / 0.25;
            return Color.FromRgba(0, (int)(255 * t), 255, (int)(128 + 127 * t));
        }
        else if (intensity < 0.5)
        {
            // Cyan to Green
            var t = (intensity - 0.25) / 0.25;
            return Color.FromRgba(0, 255, (int)(255 * (1 - t)), (int)(255 * (0.5 + 0.5 * t)));
        }
        else if (intensity < 0.75)
        {
            // Green to Yellow
            var t = (intensity - 0.5) / 0.25;
            return Color.FromRgba((int)(255 * t), 255, 0, (int)(255 * (0.75 + 0.25 * t)));
        }
        else
        {
            // Yellow to Red
            var t = (intensity - 0.75) / 0.25;
            return Color.FromRgba(255, (int)(255 * (1 - t)), 0, (int)(255 * (1.0 - 0.2 * t)));
        }
    }
}

/// <summary>
/// Interface for heatmap generation operations.
/// </summary>
public interface IHeatmapService
{
    /// <summary>
    /// Generates heatmap data from a collection of location points.
    /// </summary>
    /// <param name="locationPoints">The location points to process.</param>
    /// <param name="radius">The radius for density calculation in meters.</param>
    /// <returns>A collection of heatmap polygons.</returns>
    IEnumerable<HeatmapPolygon> GenerateHeatmapAsync(IEnumerable<LocationPoint> locationPoints, double radius = 50.0);
}

/// <summary>
/// Represents a heatmap polygon with density information.
/// </summary>
public class HeatmapPolygon
{
    /// <summary>
    /// Gets or sets the density value.
    /// </summary>
    public double Density { get; set; }

    /// <summary>
    /// Gets or sets the intensity value (0.0 to 1.0).
    /// </summary>
    public double Intensity { get; set; }

    /// <summary>
    /// Gets or sets the color for this polygon.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Gets or sets the geographical points that define this polygon.
    /// </summary>
    public Location[] Points { get; set; } = Array.Empty<Location>();
}

/// <summary>
/// Represents geographical bounds.
/// </summary>
public class Bounds
{
    /// <summary>
    /// Gets or sets the minimum latitude.
    /// </summary>
    public double MinLatitude { get; set; }

    /// <summary>
    /// Gets or sets the maximum latitude.
    /// </summary>
    public double MaxLatitude { get; set; }

    /// <summary>
    /// Gets or sets the minimum longitude.
    /// </summary>
    public double MinLongitude { get; set; }

    /// <summary>
    /// Gets or sets the maximum longitude.
    /// </summary>
    public double MaxLongitude { get; set; }
}
