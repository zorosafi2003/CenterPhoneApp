using Microsoft.Maui.ApplicationModel.Communication;

namespace CentersBarCode.Services;

public interface IAuthenticationService
{
    bool IsAuthenticated { get; }
    string? UserEmail { get; }
    string? BearerToken { get; }
    Task<bool> LoginAsync(string email, string token);
    Task LogoutAsync();
    event EventHandler<bool> AuthenticationStateChanged;
}

public class AuthenticationService : IAuthenticationService
{
    private bool _isAuthenticated;
    private string? _userEmail;
    private string? _bearerToken;
    private string? _teacherName;
    public string? _fullName;

    private readonly IApiService _apiService;

    public bool IsAuthenticated => _isAuthenticated;
    public string? UserEmail => _userEmail;
    public string? BearerToken => _bearerToken;
    public string? TeacherName => _teacherName;

    public string? FullName => _fullName;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public AuthenticationService(IApiService apiService)
    {
        _apiService = apiService;
        // Check if user is already logged in from secure storage
        _ = Task.Run(async () => await CheckStoredAuthenticationAsync());
    }

    private async Task CheckStoredAuthenticationAsync()
    {
        try
        {
            var storedEmail = await SecureStorage.Default.GetAsync("email");
            var storedToken = await SecureStorage.Default.GetAsync("token");
            var teacherName = await SecureStorage.Default.GetAsync("teachername");
            var fullName = await SecureStorage.Default.GetAsync("fullname");

            if (!string.IsNullOrEmpty(storedEmail) && !string.IsNullOrEmpty(storedToken))
            {
                // Validate stored credentials with API
                System.Diagnostics.Debug.WriteLine($"Validating stored credentials with API for user: {storedEmail}");

                _userEmail = storedEmail;
                _bearerToken = storedToken;
                _teacherName = teacherName;
                _fullName = fullName;
                _isAuthenticated = true;
                AuthenticationStateChanged?.Invoke(this, true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking stored authentication: {ex.Message}");
        }
    }

    public async Task<bool> LoginAsync(string email, string token)
    {
        try
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return false;

            // Call API to validate authentication with email and token parameters
            System.Diagnostics.Debug.WriteLine($"Validating authentication with API for user: {email}");
            var validateAuthenticationResult = await _apiService.ValidateAuthenticationAsync(email, token);

            if (validateAuthenticationResult.IsSuccess == false)
            {
                System.Diagnostics.Debug.WriteLine($"API validation failed for user: {email}");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"API validation successful for user: {email}");

            // Store credentials only after successful API validation
            await SecureStorage.Default.SetAsync("email", email);
            await SecureStorage.Default.SetAsync("token", validateAuthenticationResult.Value.Token);
            await SecureStorage.Default.SetAsync("fullname", validateAuthenticationResult.Value.FullName);
            await SecureStorage.Default.SetAsync("teachername", validateAuthenticationResult.Value.TeacherName);

            _userEmail = email;
            _bearerToken = validateAuthenticationResult.Value.Token;
            _teacherName = validateAuthenticationResult.Value.TeacherName;
            _fullName = validateAuthenticationResult.Value.FullName;    
            _isAuthenticated = true;

            // Notify authentication state changed
            AuthenticationStateChanged?.Invoke(this, true);

            System.Diagnostics.Debug.WriteLine($"User logged in: {email}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during login: {ex.Message}");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Clear stored credentials
            SecureStorage.Default.Remove("email");
            SecureStorage.Default.Remove("token");
            SecureStorage.Default.Remove("fullname");
            SecureStorage.Default.Remove("teachername");

            _userEmail = null;
            _bearerToken = null;
            _fullName= null;
            _teacherName= null;
            _isAuthenticated = false;

            // Notify authentication state changed
            AuthenticationStateChanged?.Invoke(this, false);

            System.Diagnostics.Debug.WriteLine("User logged out");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
        }

        await Task.CompletedTask;
    }
}