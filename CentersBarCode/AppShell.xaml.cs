using CentersBarCode.Services;
using CentersBarCode.ViewModels;

namespace CentersBarCode;

public partial class AppShell : Shell
{
    private readonly AppShellViewModel _viewModel;

    public AppShell(AppShellViewModel viewModel)
    {
        InitializeComponent();
        
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        // Register routes for navigation - Use absolute routes for better reliability
        Routing.RegisterRoute("MainPage", typeof(MainPage));
        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("RecordsPage", typeof(RecordsPage));
        Routing.RegisterRoute("AttachCardPage", typeof(AttachCardPage));
        Routing.RegisterRoute("ManualAddPage", typeof(ManualAddPage));
        
        // Subscribe to property changes to handle authentication state changes
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        System.Diagnostics.Debug.WriteLine($"AppShell initialized. ShowFlyoutItems: {_viewModel.ShowFlyoutItems}");
        System.Diagnostics.Debug.WriteLine($"Registered routes: MainPage, LoginPage, RecordsPage, AttachCardPage, ManualAddPage");
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppShellViewModel.ShowFlyoutItems))
        {
            System.Diagnostics.Debug.WriteLine($"AppShell: ShowFlyoutItems changed to {_viewModel.ShowFlyoutItems}");
            
            // Force update the flyout behavior
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Trigger property change notification for FlyoutBehavior
                OnPropertyChanged(nameof(FlyoutBehavior));
            });
        }
        else if (e.PropertyName == nameof(AppShellViewModel.IsAuthenticated))
        {
            System.Diagnostics.Debug.WriteLine($"AppShell: IsAuthenticated changed to {_viewModel.IsAuthenticated}");
            
            // Handle shell item visibility changes
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Force update of FlyoutItem visibility
                    OnPropertyChanged(nameof(AppShellViewModel.IsAuthenticated));
                    
                    // Ensure proper navigation after authentication state change
                    if (_viewModel.IsAuthenticated)
                    {
                        // User logged in - navigate to main page
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(200); // Slightly longer delay to ensure items are updated
                            await NavigateToPageSafely("//MainPage");
                        });
                    }
                    else
                    {
                        // User logged out - navigate to login page
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(200); // Slightly longer delay to ensure items are updated
                            await NavigateToPageSafely("//LoginPage");
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling authentication state change: {ex.Message}");
                }
            });
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.UpdateRecordsCountAsync();
        await _viewModel.UpdateStudentsCountAsync();
        await _viewModel.UpdateCentersCountAsync();
        
        // Ensure we have a proper current item set
        EnsureCurrentItemIsSet();
    }

    private void EnsureCurrentItemIsSet()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"EnsureCurrentItemIsSet called. CurrentItem: {CurrentItem?.Route}, Items count: {Items.Count}");
            
            // If CurrentItem is null, set it based on authentication state
            if (CurrentItem == null)
            {
                if (_viewModel.IsAuthenticated)
                {
                    // Find and set Main page as current
                    var mainItem = Items.FirstOrDefault(item => item.Route == "MainPage");
                    if (mainItem != null)
                    {
                        CurrentItem = mainItem;
                        System.Diagnostics.Debug.WriteLine("Set MainPage as current item");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("MainPage item not found in Items collection");
                    }
                }
                else
                {
                    // Find and set Login page as current
                    var loginItem = Items.FirstOrDefault(item => item.Route == "LoginPage");
                    if (loginItem != null)
                    {
                        CurrentItem = loginItem;
                        System.Diagnostics.Debug.WriteLine("Set LoginPage as current item");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("LoginPage item not found in Items collection");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"CurrentItem already set to: {CurrentItem.Route}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error ensuring current item is set: {ex.Message}");
        }
    }

    private async Task NavigateToPageSafely(string route)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"NavigateToPageSafely called with route: {route}");
            await Shell.Current.GoToAsync(route);
            System.Diagnostics.Debug.WriteLine($"Successfully navigated to: {route}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in NavigateToPageSafely for route {route}: {ex.Message}");
        }
    }

    // Public method to refresh the badge when records are added/deleted
    public async Task RefreshRecordsBadgeAsync()
    {
        await _viewModel.UpdateRecordsCountAsync();
        await _viewModel.UpdateStudentsCountAsync();
        await _viewModel.UpdateCentersCountAsync();
    }

    // Navigation event handlers for custom flyout buttons
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OnLoginClicked called");
            await NavigateToPageSafely("//LoginPage");
            FlyoutIsPresented = false; // Close the flyout
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnLoginClicked: {ex.Message}");
        }
    }

    private async void OnMainClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OnMainClicked called");
            await NavigateToPageSafely("//MainPage");
            FlyoutIsPresented = false; // Close the flyout
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnMainClicked: {ex.Message}");
        }
    }

    private async void OnAttachCardClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OnAttachCardClicked called");
            await NavigateToPageSafely("//AttachCardPage");
            FlyoutIsPresented = false; // Close the flyout
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnAttachCardClicked: {ex.Message}");
        }
    }
    
    private async void OnManualAddClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OnManualAddClicked called");
            await NavigateToPageSafely("//ManualAddPage");
            FlyoutIsPresented = false; // Close the flyout
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnManualAddClicked: {ex.Message}");
        }
    }

    private async void OnRecordsClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("OnRecordsClicked called");
            await NavigateToPageSafely("//RecordsPage");
            FlyoutIsPresented = false; // Close the flyout
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnRecordsClicked: {ex.Message}");
        }
    }
}
