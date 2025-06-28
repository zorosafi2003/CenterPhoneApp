using CentersBarCode.Services;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CentersBarCode.Views;

public partial class LoginPage : ContentPage
{
    private readonly IGoogleAuthService _authService;
    private readonly IAuthenticationService _authenticationService;
    private bool _isAuthenticating = false;
    
    public LoginPage(IGoogleAuthService authService, IAuthenticationService authenticationService)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        Debug.WriteLine("LoginPage initialized with IGoogleAuthService and IAuthenticationService");
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
        TestLoginButton.IsEnabled = true;
        TestLoginButton.Text = "Test Login (Demo)";
        IsBusy = false;
        _isAuthenticating = false;
    }

    private async void OnTestLoginClicked(object sender, EventArgs e)
    {
        if (_isAuthenticating) 
        {
            Debug.WriteLine("Authentication already in progress, ignoring test login click");
            return;
        }
        
        try
        {
            _isAuthenticating = true;
            
            // Disable both buttons
            GoogleLoginButton.IsEnabled = false;
            TestLoginButton.IsEnabled = false;
            TestLoginButton.Text = "Logging in...";
            IsBusy = true;
            
            Debug.WriteLine("Starting test authentication");
            
            // Simulate a test login
            var loginSuccess = await _authenticationService.LoginAsync("test@example.com", "test_token");
            
            if (loginSuccess)
            {
                Debug.WriteLine("Test login successful, navigating to MainPage");
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                await DisplayAlert("Login Failed", "Test login failed.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception during test login: {ex}");
            await DisplayAlert("Error", $"Test login failed: {ex.Message}", "OK");
        }
        finally
        {
            // Re-enable buttons only if still on this page
            if (!_authenticationService.IsAuthenticated)
            {
                ResetButtonStates();
            }
            Debug.WriteLine("Test login process completed");
        }
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
            TestLoginButton.IsEnabled = false;
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
                    authResult.IdToken ?? "demo_token"
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
}
