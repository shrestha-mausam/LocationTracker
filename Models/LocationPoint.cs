using SQLite;

namespace LocationTracker.Models;

/// <summary>
/// Represents a location point with GPS coordinates and metadata.
/// </summary>
[Table("LocationPoints")]
public class LocationPoint
{
    /// <summary>
    /// Gets or sets the unique identifier for the location point.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the latitude coordinate in decimal degrees.
    /// </summary>
    [Indexed]
    public double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude coordinate in decimal degrees.
    /// </summary>
    [Indexed]
    public double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the location was recorded.
    /// </summary>
    [Indexed]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the accuracy of the location reading in meters.
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Gets or sets the altitude in meters above sea level.
    /// </summary>
    public double? Altitude { get; set; }

    /// <summary>
    /// Gets or sets the speed in meters per second.
    /// </summary>
    public double? Speed { get; set; }

    /// <summary>
    /// Gets or sets the heading in degrees (0-360).
    /// </summary>
    public double? Heading { get; set; }

    /// <summary>
    /// Creates a LocationPoint from a Microsoft.Maui.Devices.Sensors.Location object.
    /// </summary>
    /// <param name="location">The location object to convert.</param>
    /// <returns>A new LocationPoint instance.</returns>
    public static LocationPoint FromLocation(Microsoft.Maui.Devices.Sensors.Location location)
    {
        return new LocationPoint
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Timestamp = location.Timestamp.DateTime,
            Accuracy = location.Accuracy ?? 0,
            Altitude = location.Altitude,
            Speed = location.Speed,
            Heading = location.Course
        };
    }

    /// <summary>
    /// Converts this LocationPoint to a Microsoft.Maui.Devices.Sensors.Location object.
    /// </summary>
    /// <returns>A new Location instance.</returns>
    public Microsoft.Maui.Devices.Sensors.Location ToLocation()
    {
        return new Microsoft.Maui.Devices.Sensors.Location
        {
            Latitude = Latitude,
            Longitude = Longitude,
            Timestamp = new DateTimeOffset(Timestamp),
            Accuracy = Accuracy,
            Altitude = Altitude,
            Speed = Speed,
            Course = Heading
        };
    }

    /// <summary>
    /// Calculates the distance between this location and another location in meters.
    /// </summary>
    /// <param name="other">The other location point.</param>
    /// <returns>The distance in meters.</returns>
    public double DistanceTo(LocationPoint other)
    {
        return ToLocation().CalculateDistance(other.ToLocation(), DistanceUnits.Kilometers) * 1000;
    }

    /// <summary>
    /// Returns a string representation of the location point.
    /// </summary>
    /// <returns>A formatted string with coordinates and timestamp.</returns>
    public override string ToString()
    {
        return $"Lat: {Latitude:F6}, Lng: {Longitude:F6}, Time: {Timestamp:yyyy-MM-dd HH:mm:ss}, Accuracy: {Accuracy:F1}m";
    }
}
