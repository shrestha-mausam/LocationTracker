using LocationTracker.ViewModels;

namespace LocationTracker;

/// <summary>
/// Main page for the location tracker application.
/// </summary>
public partial class MainPage : ContentPage
{
    private MainViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the MainPage class.
    /// </summary>
    public MainPage()
    {
        InitializeComponent();
        
        // Get the view model from the service provider
        _viewModel = Application.Current?.Handler?.MauiContext?.Services?.GetService<MainViewModel>();
        if (_viewModel != null)
        {
            BindingContext = _viewModel;
        }
    }

    /// <summary>
    /// Handles the page appearing event.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Center map on current location if available
        if (_viewModel?.CurrentLocation != null)
        {
            MapControl.CenterOnLocation(_viewModel.CurrentLocation);
        }
    }

    /// <summary>
    /// Handles the page disappearing event.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }
}
