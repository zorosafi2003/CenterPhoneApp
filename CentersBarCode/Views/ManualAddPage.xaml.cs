using CentersBarCode.ViewModels;

namespace CentersBarCode.Views;

public partial class ManualAddPage : ContentPage
{
    private readonly ManualAddViewModel _viewModel;

    public ManualAddPage(ManualAddViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        // Subscribe to the SearchCommandExecuted event to dismiss keyboard when search button is clicked
        _viewModel.SearchCommandExecuted += () => {
            MainThread.BeginInvokeOnMainThread(DismissKeyboard);
            
#if ANDROID
            // Use platform-specific code for Android
            MainThread.BeginInvokeOnMainThread(() => {
                try {
                    // This ensures keyboard dismissal on Android through reflection
                    var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                    var method = activity?.GetType().GetMethod("HideKeyboard");
                    method?.Invoke(activity, null);
                    System.Diagnostics.Debug.WriteLine("Keyboard dismissed via platform-specific code");
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Error in platform-specific keyboard dismissal: {ex.Message}");
                }
            });
#endif
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Refresh centers data when the page appears
        await _viewModel.RefreshCentersCommand.ExecuteAsync(null);
    }
    
    private void DismissKeyboard()
    {
        // Multiple keyboard dismissal attempts for maximum compatibility
        
        // Method 1: Unfocus the entry
        if (SearchEntry != null)
        {
            SearchEntry.IsEnabled = false;
            SearchEntry.IsEnabled = true;
            SearchEntry.Unfocus();
        }
        
        // Method 2: Focus the page itself
        this.Focus();
        
        System.Diagnostics.Debug.WriteLine("Dismissed keyboard using multiple methods");
    }
}