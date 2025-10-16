# Location Tracker with Heatmap Visualization

A cross-platform .NET MAUI application that tracks user location and displays it as a heatmap overlay on a map. The app continuously tracks GPS coordinates, stores them in a local SQLite database, and visualizes the data as a gradient heatmap showing areas of high activity.

## Features

- **Continuous Location Tracking**: Tracks GPS coordinates every 3-5 seconds while the app is active
- **Background Location Support**: Can track location even when the app is minimized (with proper permissions)
- **Heatmap Visualization**: Displays location data as a gradient overlay (Blue → Green → Yellow → Red)
- **SQLite Database**: Stores all location data locally for persistence and analysis
- **Cross-Platform**: Runs on iOS, Android, and macOS (MacCatalyst)
- **Permission Management**: Handles location permissions gracefully across platforms
- **Data Management**: Clear all location data with confirmation dialog
- **Real-time Updates**: Live tracking status and location count display

## Screenshots

The app features:
- Full-screen map with heatmap overlay
- Floating control panel with tracking controls
- Status bar showing current tracking state
- Location count indicator
- Adjustable heatmap radius slider

## Prerequisites

### Development Environment
- **.NET 9.0 SDK** or later
- **Visual Studio Code** with C# extension
- **Xcode** (for iOS/macOS development)
- **Android Studio** (for Android development)

### Platform-Specific Requirements

#### iOS
- Xcode 15.0 or later
- iOS 15.0 or later
- Apple Developer Account (for device testing)

#### Android
- Android SDK API Level 21 or later
- Google Play Services (for maps)

#### macOS
- macOS 12.0 or later
- Xcode command line tools

## Installation & Setup

### 1. Clone the Repository
```bash
git clone <repository-url>
cd LocationTracker
```

### 2. Restore NuGet Packages
```bash
dotnet restore
```

### 3. Install .NET MAUI Workload
```bash
dotnet workload install maui
```

### 4. Platform-Specific Setup

#### Android Setup
1. **Google Maps API Key** (Required):
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Create a new project or select existing
   - Enable "Maps SDK for Android"
   - Create credentials (API Key)
   - Create file: `Platforms/Android/Resources/values/strings.xml`:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <resources>
       <string name="google_maps_key">YOUR_API_KEY_HERE</string>
   </resources>
   ```

2. **Android Permissions**: Already configured in `AndroidManifest.xml`

#### iOS Setup
1. **Maps Framework**: No additional setup required (uses Apple Maps)
2. **Permissions**: Already configured in `Info.plist`

#### macOS Setup
1. **Maps Framework**: No additional setup required (uses Apple Maps)
2. **Permissions**: Already configured in `Info.plist`

## Building and Running

### Build the Application
```bash
dotnet build
```

### Run on Specific Platforms

#### iOS Simulator
```bash
dotnet build -t:Run -f net9.0-ios
```

#### Android Emulator
```bash
dotnet build -t:Run -f net9.0-android
```

#### macOS
```bash
dotnet build -t:Run -f net9.0-maccatalyst
```

### Using Visual Studio Code
1. Install the **C#** extension
2. Install the **.NET MAUI** extension (if available)
3. Press `F5` to build and run

## Usage

### Starting Location Tracking
1. Launch the app
2. Grant location permissions when prompted
3. Tap "Start Tracking" to begin recording location data
4. The app will continuously track your location while active

### Viewing the Heatmap
- The heatmap automatically appears when location data is available
- Blue areas indicate low activity/density
- Red areas indicate high activity/density
- Use the slider to adjust heatmap radius (10m - 200m)

### Managing Data
- **Get Current Location**: Tap the 📍 button to center map on current location
- **Toggle Heatmap**: Tap "Show/Hide" to toggle heatmap visibility
- **Clear Data**: Tap 🗑️ to delete all location data (with confirmation)

### Permissions

The app requests the following permissions:

#### Android
- `ACCESS_FINE_LOCATION`: High-accuracy GPS location
- `ACCESS_COARSE_LOCATION`: Network-based location
- `ACCESS_BACKGROUND_LOCATION`: Location tracking when app is backgrounded

#### iOS/macOS
- `NSLocationWhenInUseUsageDescription`: Location while app is active
- `NSLocationAlwaysAndWhenInUseUsageDescription`: Location always (including background)
- `NSLocationAlwaysUsageDescription`: Background location access

## Architecture

The application follows the MVVM (Model-View-ViewModel) pattern:

### Models
- **LocationPoint**: Represents a GPS coordinate with metadata (timestamp, accuracy, etc.)

### Services
- **LocationService**: Handles GPS tracking and permission management
- **DatabaseService**: Manages SQLite database operations
- **HeatmapService**: Processes location data into heatmap visualization

### ViewModels
- **MainViewModel**: Coordinates between services and UI, handles commands

### Views
- **MainPage**: Main UI with map and control panel
- **HeatmapMapControl**: Custom map control with heatmap overlay

### Platform Handlers
- **Android**: Google Maps with HeatmapTileProvider
- **iOS**: MapKit with MKPolygon overlays

## Database Schema

The SQLite database stores location data in the `LocationPoints` table:

```sql
CREATE TABLE LocationPoints (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Latitude REAL NOT NULL,
    Longitude REAL NOT NULL,
    Timestamp DATETIME NOT NULL,
    Accuracy REAL NOT NULL,
    Altitude REAL,
    Speed REAL,
    Heading REAL
);

-- Indexes for performance
CREATE INDEX idx_location_timestamp ON LocationPoints(Timestamp);
CREATE INDEX idx_location_coordinates ON LocationPoints(Latitude, Longitude);
```

## Performance Considerations

- **Location Updates**: Default 3-second interval balances accuracy with battery life
- **Database Batching**: Locations are saved in batches to reduce I/O operations
- **Memory Management**: Heatmap data is generated on-demand to conserve memory
- **Background Processing**: Efficient background location tracking

## Troubleshooting

### Common Issues

1. **Location Permission Denied**
   - Go to device Settings → Privacy → Location Services
   - Enable location services and grant permission to the app

2. **Maps Not Loading (Android)**
   - Verify Google Maps API key is correctly configured
   - Check that Maps SDK for Android is enabled in Google Cloud Console

3. **App Crashes on Startup**
   - Ensure all NuGet packages are restored: `dotnet restore`
   - Check that .NET MAUI workload is installed: `dotnet workload list`

4. **No Location Data**
   - Verify GPS is enabled on the device
   - Check that the app has location permissions
   - Ensure you're in an area with GPS signal

### Debug Information

Enable debug logging by running in Debug mode. Check the debug output for:
- Database initialization status
- Location permission status
- GPS accuracy and updates
- Heatmap generation progress

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature-name`
3. Make your changes following C# coding conventions
4. Test on multiple platforms
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Privacy Notice

This application:
- Stores location data locally on your device only
- Does not transmit location data to external servers
- Requires explicit user permission for location access
- Allows users to clear all stored data at any time

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review the debug output for error messages
3. Open an issue on the project repository
4. Ensure you're using the latest version of .NET and MAUI

## Future Enhancements

Potential improvements for future versions:
- Export location data to various formats (GPX, KML)
- Advanced filtering and date range selection
- Multiple heatmap visualization styles
- Offline map support
- Location sharing capabilities
- Battery usage optimization
