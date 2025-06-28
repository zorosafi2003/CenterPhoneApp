using Microsoft.Maui.Controls;
using System.Diagnostics;
using CentersBarCode.Services;

namespace CentersBarCode.Views;

public partial class SplashScreen : ContentPage
{
    public SplashScreen()
    {
        InitializeComponent();
        
        // Navigate based on authentication state after 5 seconds
        Dispatcher.DispatchAsync(async () => 
        {
            try
            {
                await Task.Delay(3000); // 3 seconds splash
                
                // Set the main page to AppShell from service provider
                if (Application.Current != null)
                {
                    // Get services from the service provider to ensure proper dependency injection
                    var services = Handler?.MauiContext?.Services;
                    if (services != null)
                    {
                        var appShellViewModel = services.GetRequiredService<ViewModels.AppShellViewModel>();
                        var authService = services.GetRequiredService<IAuthenticationService>();
                        var appShell = new AppShell(appShellViewModel);
                        Application.Current.MainPage = appShell;
                        
                        // Navigate based on authentication state
                        if (authService.IsAuthenticated)
                        {
                            Debug.WriteLine("User is authenticated, navigating to MainPage");
                            await Shell.Current.GoToAsync("//MainPage");
                        }
                        else
                        {
                            Debug.WriteLine("User is not authenticated, navigating to LoginPage");
                            await Shell.Current.GoToAsync("//LoginPage");
                        }
                    }
                    else
                    {
                        // Fallback if services are not available
                        var databaseService = new Services.DatabaseService();
                        var authService = new Services.AuthenticationService();
                        var appShellViewModel = new ViewModels.AppShellViewModel(databaseService, authService);
                        var appShell = new AppShell(appShellViewModel);
                        Application.Current.MainPage = appShell;
                        
                        // Navigate to login page as fallback
                        await Shell.Current.GoToAsync("//LoginPage");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during splash navigation: {ex.Message}");
            }
        });
    }
}