using CentersBarCode.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CentersBarCode.Views;

public partial class LoginPage : ContentPage
{
    private readonly IGoogleAuthService _authService;
    private readonly IAuthenticationService _authenticationService;
    private bool _isAuthenticating = false;
    private static bool _hasInitialized = false;


    public LoginPage(IGoogleAuthService authService, IAuthenticationService authenticationService)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        Debug.WriteLine("LoginPage initialized with IGoogleAuthService and IAuthenticationService");
        // Run splash logic only once
        if (!_hasInitialized)
        {
            _hasInitialized = true;

            Dispatcher.DispatchAsync(async () =>
            {
                try
                {
                    await Task.Delay(3000); // splash delay
                    await InitializeShellAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during splash initialization: {ex.Message}");
                    await InitializeShellFallbackAsync();
                }
            });
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine("LoginPage.OnAppearing called");
        
        try
        {
            // Check if user is already authenticated
            if (_authenticationService.IsAuthenticated)
            {
                Debug.WriteLine("User is already authenticated, navigating to MainPage");
                // User is already logged in, navigate to main page
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                // Reset button states when page appears
                ResetButtonStates();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
        }
    }

    private void ResetButtonStates()
    {
        GoogleLoginButton.IsEnabled = true;
        GoogleLoginButton.Text = "Sign in with Google";
        IsBusy = false;
        _isAuthenticating = false;
    }

    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        // Prevent multiple clicks during authentication
        if (_isAuthenticating) 
        {
            Debug.WriteLine("Authentication already in progress, ignoring click");
            return;
        }
        
        try
        {
            // Check connectivity first
            var current = Connectivity.Current;
            if (current.NetworkAccess != NetworkAccess.Internet)
            {
                Debug.WriteLine("No internet connection detected on login button click");
                await DisplayAlert("Network Error", "No internet connection. Please check your network settings and try again.", "OK");
                return;
            }
            
            _isAuthenticating = true;
            
            // Disable the button to prevent multiple clicks
            GoogleLoginButton.IsEnabled = false;
            GoogleLoginButton.Text = "Signing in...";
            IsBusy = true;
            
            Debug.WriteLine("Starting Google authentication");
            
            // Use the GoogleAuthService to authenticate with Google
            var authResult = await _authService.SignInWithGoogleAsync();
            
            Debug.WriteLine($"Authentication result: Success={authResult?.IsSuccessful ?? false}, Email={authResult?.UserEmail ?? "null"}");
            
            if (authResult?.IsSuccessful == true && !string.IsNullOrEmpty(authResult.UserEmail))
            {
                // Login to our authentication service
                var loginSuccess = await _authenticationService.LoginAsync(
                    authResult.UserEmail, 
                    authResult.IdToken 
                );
                
                if (loginSuccess)
                {
                    Debug.WriteLine("Login successful, navigating to MainPage");
                    // Navigate to MainPage after successful authentication
                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    await DisplayAlert("Login Failed", "Failed to complete login process.", "OK");
                }
            }
            else
            {
                var errorMessage = authResult?.ErrorMessage ?? "Unknown error occurred";
                Debug.WriteLine($"Authentication failed: {errorMessage}");
                
                // Handle network errors specifically with a friendlier message
                if (!string.IsNullOrEmpty(errorMessage) && 
                    (errorMessage.Contains("Network error", StringComparison.OrdinalIgnoreCase) || 
                     errorMessage.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                     errorMessage.Contains("internet", StringComparison.OrdinalIgnoreCase)))
                {
                    await DisplayAlert("Connection Error", 
                        "Unable to connect to Google servers. Please check your internet connection and try again.", 
                        "OK");
                }
                // Handle the cancellation case specifically
                else if (!string.IsNullOrEmpty(errorMessage) &&
                    (errorMessage.Contains("cancelled", StringComparison.OrdinalIgnoreCase) || 
                     errorMessage.Contains("canceled", StringComparison.OrdinalIgnoreCase) ||
                     errorMessage.Contains("Result.Canceled", StringComparison.OrdinalIgnoreCase)))
                {
                    // User cancelled the sign-in, don't show an error dialog as this is expected behavior
                    Debug.WriteLine("User cancelled sign-in, not displaying error dialog");
                }
                else
                {
                    // Show an error dialog for other failures
                    await DisplayAlert("Login Failed", errorMessage, "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception during login: {ex}");
            await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
        }
        finally
        {
            // Re-enable the button only if still on login page
            if (!_authenticationService.IsAuthenticated)
            {
                ResetButtonStates();
            }
            Debug.WriteLine("Login process completed");
        }
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

            var appShellViewModel = new ViewModels.AppShellViewModel(databaseService, authService, studentService, centerService);
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
