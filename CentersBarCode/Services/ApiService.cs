using System.Net.Http.Headers;
using System.Text.Json;
using CentersBarCode.Models;
using Microsoft.Extensions.Logging;

namespace CentersBarCode.Services;

public interface IApiService
{
    Task<List<StudentApiResponse>> GetStudentsAsync(string bearerToken);
    Task<List<CenterApiResponse>> GetCentersAsync(string bearerToken);
    Task<ApiConfiguration> LoadApiConfigurationAsync();
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private ApiConfiguration? _apiConfig;
    private readonly ILogoutService _logoutService;


    public ApiService(HttpClient httpClient, ILogger<ApiService> logger, ILogoutService logoutService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _logoutService = logoutService;
    }

    public async Task<ApiConfiguration> LoadApiConfigurationAsync()
    {
        if (_apiConfig != null)
            return _apiConfig;

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("api-config.json");
            using var reader = new StreamReader(stream);
            var jsonContent = await reader.ReadToEndAsync();
            
            _apiConfig = JsonSerializer.Deserialize<ApiConfiguration>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (_apiConfig == null)
            {
                throw new InvalidOperationException("Failed to deserialize API configuration");
            }

            return _apiConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API configuration");
            
            // Return default configuration if file loading fails
            _apiConfig = new ApiConfiguration
            {
                BaseUrl = "https://your-api-domain.com",
                StudentsEndpoint = "/api/students",
                CentersEndpoint = "/api/centers"
            };
            
            return _apiConfig;
        }
    }
    private async Task<T?> GetAsync<T>(string url, string bearerToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized access detected (401), triggering logout.");
                await _logoutService.LogoutAsync();
                return default;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API call failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"API call failed with status: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
    public async Task<List<StudentApiResponse>> GetStudentsAsync(string bearerToken)
    {
        var config = await LoadApiConfigurationAsync();
        var url = $"{config.BaseUrl.TrimEnd('/')}{config.StudentsEndpoint}";
        var result = await GetAsync<List<StudentApiResponse>>(url, bearerToken);
        return result ?? new List<StudentApiResponse>();
    }
    public async Task<List<CenterApiResponse>> GetCentersAsync(string bearerToken)
    {
        var config = await LoadApiConfigurationAsync();
        var url = $"{config.BaseUrl.TrimEnd('/')}{config.CentersEndpoint}";
        var result = await GetAsync<List<CenterApiResponse>>(url, bearerToken);
        return result ?? new List<CenterApiResponse>();
    }
}