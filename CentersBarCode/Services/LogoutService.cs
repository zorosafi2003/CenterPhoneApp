using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using CentersBarCode.Services;
using Microsoft.Extensions.Logging;

namespace CentersBarCode.Services
{
    public class LogoutService : ILogoutService
    {
        private readonly IAuthenticationService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<LogoutService> _logger;

        public LogoutService(IAuthenticationService authService, IDatabaseService databaseService, ILogger<LogoutService> logger)
        {
            _authService = authService;
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task LogoutAsync()
        {
            try
            {
                _logger.LogInformation("Logging out...");

                // 1. Clear authentication token
                Preferences.Remove("AccessToken");

                // 2. Perform logout logic in authentication service
                await _authService.LogoutAsync();

                // 3. Optionally clear user-specific data (centers/students)
                await _databaseService.ClearAllCentersAsync();
                await _databaseService.ClearAllStudentsAsync();

                // 4. Navigate to LoginPage
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Shell.Current == null)
                    {
                        _logger.LogWarning("Shell is null. Cannot navigate.");
                        return;
                    }

                    // Clear navigation stack and navigate to login
                    await Shell.Current.GoToAsync("//LoginPage", true);
                    _logger.LogInformation("Navigated to login page");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }
        }
    }
}
