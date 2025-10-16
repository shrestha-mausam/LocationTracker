using SQLite;
using LocationTracker.Models;
using Microsoft.Extensions.Logging;

namespace LocationTracker.Services;

/// <summary>
/// Service for managing SQLite database operations for location data.
/// </summary>
public class DatabaseService : IDatabaseService
{
    private readonly SQLiteAsyncConnection _database;
    private readonly ILogger<DatabaseService> _logger;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the DatabaseService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public DatabaseService(ILogger<DatabaseService> logger)
    {
        _logger = logger;
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "locationtracker.db3");
        _database = new SQLiteAsyncConnection(databasePath);
    }

    /// <summary>
    /// Initializes the database and creates tables if they don't exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        try
        {
            await _semaphore.WaitAsync();
            await _database.CreateTableAsync<LocationPoint>();
            
            // Create indexes for better query performance
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_location_timestamp ON LocationPoints(Timestamp)");
            await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_location_coordinates ON LocationPoints(Latitude, Longitude)");
            
            _logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Saves a location point to the database.
    /// </summary>
    /// <param name="locationPoint">The location point to save.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveLocationAsync(LocationPoint locationPoint)
    {
        try
        {
            await _database.InsertAsync(locationPoint);
            _logger.LogDebug("Saved location point: {LocationPoint}", locationPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save location point: {LocationPoint}", locationPoint);
            throw;
        }
    }

    /// <summary>
    /// Saves multiple location points to the database in a single transaction.
    /// </summary>
    /// <param name="locationPoints">The location points to save.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveLocationsAsync(IEnumerable<LocationPoint> locationPoints)
    {
        try
        {
            await _database.InsertAllAsync(locationPoints);
            _logger.LogDebug("Saved {Count} location points", locationPoints.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save location points");
            throw;
        }
    }

    /// <summary>
    /// Gets all location points from the database.
    /// </summary>
    /// <returns>A task that returns a collection of location points.</returns>
    public async Task<IEnumerable<LocationPoint>> GetAllLocationsAsync()
    {
        try
        {
            var locations = await _database.Table<LocationPoint>()
                .OrderBy(l => l.Timestamp)
                .ToListAsync();
            
            _logger.LogDebug("Retrieved {Count} location points", locations.Count);
            return locations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve location points");
            throw;
        }
    }

    /// <summary>
    /// Gets location points within a specified time range.
    /// </summary>
    /// <param name="startTime">The start time of the range.</param>
    /// <param name="endTime">The end time of the range.</param>
    /// <returns>A task that returns a collection of location points.</returns>
    public async Task<IEnumerable<LocationPoint>> GetLocationsInRangeAsync(DateTime startTime, DateTime endTime)
    {
        try
        {
            var locations = await _database.Table<LocationPoint>()
                .Where(l => l.Timestamp >= startTime && l.Timestamp <= endTime)
                .OrderBy(l => l.Timestamp)
                .ToListAsync();
            
            _logger.LogDebug("Retrieved {Count} location points in range {StartTime} to {EndTime}", 
                locations.Count, startTime, endTime);
            return locations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve location points in range");
            throw;
        }
    }

    /// <summary>
    /// Gets location points within a specified geographical area.
    /// </summary>
    /// <param name="minLatitude">The minimum latitude.</param>
    /// <param name="maxLatitude">The maximum latitude.</param>
    /// <param name="minLongitude">The minimum longitude.</param>
    /// <param name="maxLongitude">The maximum longitude.</param>
    /// <returns>A task that returns a collection of location points.</returns>
    public async Task<IEnumerable<LocationPoint>> GetLocationsInAreaAsync(
        double minLatitude, double maxLatitude, double minLongitude, double maxLongitude)
    {
        try
        {
            var locations = await _database.Table<LocationPoint>()
                .Where(l => l.Latitude >= minLatitude && l.Latitude <= maxLatitude &&
                           l.Longitude >= minLongitude && l.Longitude <= maxLongitude)
                .OrderBy(l => l.Timestamp)
                .ToListAsync();
            
            _logger.LogDebug("Retrieved {Count} location points in area", locations.Count);
            return locations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve location points in area");
            throw;
        }
    }

    /// <summary>
    /// Gets the total count of location points in the database.
    /// </summary>
    /// <returns>A task that returns the count of location points.</returns>
    public async Task<int> GetLocationCountAsync()
    {
        try
        {
            var count = await _database.Table<LocationPoint>().CountAsync();
            _logger.LogDebug("Total location points count: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get location count");
            throw;
        }
    }

    /// <summary>
    /// Clears all location points from the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClearAllLocationsAsync()
    {
        try
        {
            var deletedCount = await _database.DeleteAllAsync<LocationPoint>();
            _logger.LogInformation("Cleared {Count} location points from database", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear location points");
            throw;
        }
    }

    /// <summary>
    /// Deletes location points older than the specified date.
    /// </summary>
    /// <param name="cutoffDate">The cutoff date.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteOldLocationsAsync(DateTime cutoffDate)
    {
        try
        {
            var deletedCount = await _database.Table<LocationPoint>()
                .Where(l => l.Timestamp < cutoffDate)
                .DeleteAsync();
            
            _logger.LogInformation("Deleted {Count} old location points before {CutoffDate}", 
                deletedCount, cutoffDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete old location points");
            throw;
        }
    }

    /// <summary>
    /// Gets database statistics.
    /// </summary>
    /// <returns>A task that returns database statistics.</returns>
    public async Task<DatabaseStatistics> GetStatisticsAsync()
    {
        try
        {
            var totalCount = await GetLocationCountAsync();
            var firstLocation = await _database.Table<LocationPoint>()
                .OrderBy(l => l.Timestamp)
                .FirstOrDefaultAsync();
            var lastLocation = await _database.Table<LocationPoint>()
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            return new DatabaseStatistics
            {
                TotalLocationPoints = totalCount,
                FirstLocationTime = firstLocation?.Timestamp,
                LastLocationTime = lastLocation?.Timestamp,
                DatabaseSize = await GetDatabaseSizeAsync()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database statistics");
            throw;
        }
    }

    /// <summary>
    /// Gets the size of the database file in bytes.
    /// </summary>
    /// <returns>A task that returns the database size.</returns>
    private async Task<long> GetDatabaseSizeAsync()
    {
        try
        {
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "locationtracker.db3");
            if (File.Exists(databasePath))
            {
                var fileInfo = new FileInfo(databasePath);
                return fileInfo.Length;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Disposes the database connection.
    /// </summary>
    public void Dispose()
    {
        _database?.CloseAsync();
        _semaphore?.Dispose();
    }
}

/// <summary>
/// Interface for database operations.
/// </summary>
public interface IDatabaseService : IDisposable
{
    Task InitializeAsync();
    Task SaveLocationAsync(LocationPoint locationPoint);
    Task SaveLocationsAsync(IEnumerable<LocationPoint> locationPoints);
    Task<IEnumerable<LocationPoint>> GetAllLocationsAsync();
    Task<IEnumerable<LocationPoint>> GetLocationsInRangeAsync(DateTime startTime, DateTime endTime);
    Task<IEnumerable<LocationPoint>> GetLocationsInAreaAsync(double minLatitude, double maxLatitude, double minLongitude, double maxLongitude);
    Task<int> GetLocationCountAsync();
    Task ClearAllLocationsAsync();
    Task DeleteOldLocationsAsync(DateTime cutoffDate);
    Task<DatabaseStatistics> GetStatisticsAsync();
}

/// <summary>
/// Represents database statistics.
/// </summary>
public class DatabaseStatistics
{
    /// <summary>
    /// Gets or sets the total number of location points.
    /// </summary>
    public int TotalLocationPoints { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the first location point.
    /// </summary>
    public DateTime? FirstLocationTime { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last location point.
    /// </summary>
    public DateTime? LastLocationTime { get; set; }

    /// <summary>
    /// Gets or sets the database file size in bytes.
    /// </summary>
    public long DatabaseSize { get; set; }
}
