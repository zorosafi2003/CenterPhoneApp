using CentersBarCode.Services;
using CentersBarCode.ViewModels;

namespace CentersBarCode;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        
        // Initialize the AppShell with proper dependency injection
        InitializeAppShell();
    }

    private void InitializeAppShell()
    {
        try
        {
            // Get services from the service provider
            var services = Handler?.MauiContext?.Services ?? IPlatformApplication.Current?.Services;
            
            if (services != null)
            {
                var appShellViewModel = services.GetRequiredService<AppShellViewModel>();
                var authService = services.GetRequiredService<IAuthenticationService>();
                
                // Create AppShell instance
                var appShell = new AppShell(appShellViewModel);
                MainPage = appShell;
                
                // Navigate to appropriate page based on authentication state
                Dispatcher.DispatchAsync(async () =>
                {
                    await Task.Delay(100); // Small delay to ensure shell is ready
                    
                    if (authService.IsAuthenticated)
                    {
                        System.Diagnostics.Debug.WriteLine("User is authenticated, navigating to MainPage");
                        await Shell.Current.GoToAsync("//MainPage");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("User is not authenticated, navigating to LoginPage");
                        await Shell.Current.GoToAsync("//LoginPage");
                    }
                });
            }
            else
            {
                // Fallback: start with splash screen
                System.Diagnostics.Debug.WriteLine("Services not available, falling back to SplashScreen");
                MainPage = new Views.SplashScreen();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing AppShell: {ex.Message}");
            // Fallback: start with splash screen
            MainPage = new Views.SplashScreen();
        }
    }
}
