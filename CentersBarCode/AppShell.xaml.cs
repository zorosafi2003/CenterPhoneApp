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
        
        // Register routes for navigation
        Routing.RegisterRoute("MainPage", typeof(Views.MainPage));
        Routing.RegisterRoute("LoginPage", typeof(Views.LoginPage));
        Routing.RegisterRoute("RecordsPage", typeof(Views.RecordsPage));
        Routing.RegisterRoute("AttachCardPage", typeof(Views.AttachCardPage));
        
        // Subscribe to property changes to handle authentication state changes
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        System.Diagnostics.Debug.WriteLine($"AppShell initialized. ShowFlyoutItems: {_viewModel.ShowFlyoutItems}");
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
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.UpdateRecordsCountAsync();
    }

    // Public method to refresh the badge when records are added/deleted
    public async Task RefreshRecordsBadgeAsync()
    {
        await _viewModel.UpdateRecordsCountAsync();
    }
}
