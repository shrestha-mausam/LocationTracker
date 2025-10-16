using System.Globalization;

namespace LocationTracker;

/// <summary>
/// Converts boolean value to tracking button text.
/// </summary>
public class BoolToTrackingTextConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to tracking button text.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The converted text.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isTracking)
        {
            return isTracking ? "Stop Tracking" : "Start Tracking";
        }
        return "Start Tracking";
    }

    /// <summary>
    /// Converts back from text to boolean (not implemented).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The converted value.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean value to tracking button color.
/// </summary>
public class BoolToTrackingColorConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to tracking button color.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The converted color.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isTracking)
        {
            return isTracking ? Color.FromArgb("#F44336") : Color.FromArgb("#4CAF50");
        }
        return Color.FromArgb("#4CAF50");
    }

    /// <summary>
    /// Converts back from color to boolean (not implemented).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The converted value.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean value to heatmap button text.
/// </summary>
public class BoolToHeatmapTextConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to heatmap button text.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The converted text.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool showHeatmap)
        {
            return showHeatmap ? "Hide" : "Show";
        }
        return "Show";
    }

    /// <summary>
    /// Converts back from text to boolean (not implemented).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The converted value.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean value to heatmap button color.
/// </summary>
public class BoolToHeatmapColorConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to heatmap button color.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The converted color.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool showHeatmap)
        {
            return showHeatmap ? Color.FromArgb("#FF9800") : Color.FromArgb("#2196F3");
        }
        return Color.FromArgb("#2196F3");
    }

    /// <summary>
    /// Converts back from color to boolean (not implemented).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The converted value.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    /// <summary>
    /// Inverts a boolean value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The inverted boolean value.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    /// <summary>
    /// Inverts a boolean value back.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The inverted boolean value.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }
}

/// <summary>
/// Passes through a boolean value (used for IsEnabled bindings).
/// </summary>
public class BoolToBoolConverter : IValueConverter
{
    /// <summary>
    /// Passes through a boolean value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The same boolean value.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue ? boolValue : false;
    }

    /// <summary>
    /// Passes through a boolean value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The same boolean value.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue ? boolValue : false;
    }
}
