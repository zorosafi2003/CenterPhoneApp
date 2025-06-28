namespace CentersBarCode.ViewModels;

public partial class AppShellViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IAuthenticationService _authenticationService;

    [ObservableProperty]
    private int _recordsCount;

    [ObservableProperty]
    private bool _hasBadge;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _userEmail = string.Empty;

    [ObservableProperty]
    private bool _showFlyoutItems;

    public AppShellViewModel(IDatabaseService databaseService, IAuthenticationService authenticationService)
    {
        _databaseService = databaseService;
        _authenticationService = authenticationService;
        
        RecordsCount = 0;
        HasBadge = false;
        IsAuthenticated = _authenticationService.IsAuthenticated;
        UserEmail = _authenticationService.UserEmail ?? string.Empty;
        ShowFlyoutItems = _authenticationService.IsAuthenticated;
        Title = "Centers Barcode App";
        
        // Subscribe to authentication state changes
        _authenticationService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        
        // Initialize the badge count
        _ = Task.Run(async () => await UpdateRecordsCountAsync());
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsAuthenticated = isAuthenticated;
            UserEmail = _authenticationService.UserEmail ?? string.Empty;
            ShowFlyoutItems = isAuthenticated;
            
            System.Diagnostics.Debug.WriteLine($"Authentication state changed: {isAuthenticated}, ShowFlyoutItems: {ShowFlyoutItems}");
        });
    }

    public async Task UpdateRecordsCountAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            var records = await _databaseService.GetQrCodeRecordsAsync();
            var newCount = records.Count;
            
            // Only update if the count has changed or if it's the first time
            if (RecordsCount != newCount)
            {
                RecordsCount = newCount;
                HasBadge = RecordsCount > 0;
                System.Diagnostics.Debug.WriteLine($"AppShell Badge updated: {RecordsCount} records");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating AppShell records count: {ex.Message}");
            RecordsCount = 0;
            HasBadge = false;
        }
    }

    // Command to refresh the badge manually
    [RelayCommand]
    public async Task RefreshBadgeAsync()
    {
        await UpdateRecordsCountAsync();
    }

    // Command to logout
    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Logout command initiated");
            await _authenticationService.LogoutAsync();
            
            // Navigate to login page
            if (Shell.Current != null)
            {
                System.Diagnostics.Debug.WriteLine("Navigating to LoginPage after logout");
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
        }
    }
}