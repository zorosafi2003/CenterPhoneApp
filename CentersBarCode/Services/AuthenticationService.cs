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

    public bool IsAuthenticated => _isAuthenticated;
    public string? UserEmail => _userEmail;
    public string? BearerToken => _bearerToken;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public AuthenticationService()
    {
        // Check if user is already logged in from secure storage
        _ = Task.Run(async () => await CheckStoredAuthenticationAsync());
    }

    private async Task CheckStoredAuthenticationAsync()
    {
        try
        {
            var storedEmail = await SecureStorage.Default.GetAsync("email");
            var storedToken = await SecureStorage.Default.GetAsync("token");

            if (!string.IsNullOrEmpty(storedEmail) && !string.IsNullOrEmpty(storedToken))
            {
                _userEmail = storedEmail;
                _bearerToken = storedToken;
                _isAuthenticated = true;
                AuthenticationStateChanged?.Invoke(this, true);
                System.Diagnostics.Debug.WriteLine($"User already authenticated: {storedEmail}");
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

            // Store credentials
            await SecureStorage.Default.SetAsync("email", email);
            await SecureStorage.Default.SetAsync("token", token);

            _userEmail = email;
            _bearerToken = token;
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

            _userEmail = null;
            _bearerToken = null;
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