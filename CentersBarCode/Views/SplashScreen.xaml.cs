using Microsoft.Maui.Controls;
using System.Diagnostics;
using CentersBarCode.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CentersBarCode.Views;

public partial class SplashScreen : ContentPage
{
    public SplashScreen()
    {
        InitializeComponent();

        // Navigate based on authentication state after 3 seconds
        Dispatcher.DispatchAsync(async () =>
        {
            try
            {
                await Task.Delay(3000); // 3 seconds splash
                await InitializeShellAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during splash initialization: {ex.Message}");
                await InitializeShellFallbackAsync();
            }
        });
    }

    private async Task InitializeShellAsync()
    {
        try
        {
            if (Application.Current == null) return;

            // Get services from the service provider
            var services = Handler?.MauiContext?.Services;
            if (services != null)
            {
                // Use dependency injection
                var appShellViewModel = services.GetRequiredService<ViewModels.AppShellViewModel>();
                var authService = services.GetRequiredService<IAuthenticationService>();

                // Create and set AppShell
                var appShell = new AppShell(appShellViewModel);
                Application.Current.MainPage = appShell;

                // Wait a moment for shell to be ready
                await Task.Delay(100);

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
                await InitializeShellFallbackAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in InitializeShellAsync: {ex.Message}");
            await InitializeShellFallbackAsync();
        }
    }

    private async Task InitializeShellFallbackAsync()
    {
        try
        {
            Debug.WriteLine("Using fallback shell initialization");

            if (Application.Current == null) return;

            // Create services manually for fallback
            var databaseService = new Services.DatabaseService();

            // Create loggers
            var apiLogger = NullLogger<ApiService>.Instance;
            var studentLogger = NullLogger<StudentService>.Instance;
            var centerLogger = NullLogger<CenterService>.Instance;

            var httpClient = new HttpClient();
            var apiService = new Services.ApiService(httpClient, apiLogger);
            var authService = new Services.AuthenticationService(apiService);
            var studentService = new Services.StudentService(databaseService, apiService, studentLogger);
            var centerService = new Services.CenterService(databaseService, apiService, centerLogger);
            var googleAuthService = new Services.GoogleAuthService();

            var appShellViewModel = new ViewModels.AppShellViewModel(databaseService, authService, studentService, centerService, googleAuthService);
            var appShell = new AppShell(appShellViewModel);

            Application.Current.MainPage = appShell;

            // Wait a moment for shell to be ready
            await Task.Delay(100);

            // Navigate to login page as fallback
            await Shell.Current.GoToAsync("//LoginPage");
            Debug.WriteLine("Fallback navigation to LoginPage completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Even fallback initialization failed: {ex.Message}");

            // Last resort - try to show a basic page
            if (Application.Current != null)
            {
                Application.Current.MainPage = new ContentPage
                {
                    Content = new Label
                    {
                        Text = "Application failed to start properly. Please restart the app.",
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                };
            }
        }
    }
}