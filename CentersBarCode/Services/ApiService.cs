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

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
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

    public async Task<List<StudentApiResponse>> GetStudentsAsync(string bearerToken)
    {
        try
        {
            var config = await LoadApiConfigurationAsync();
            var requestUri = $"{config.BaseUrl.TrimEnd('/')}{config.StudentsEndpoint}";

            _logger.LogInformation("Making Students API call to: {RequestUri}", requestUri);

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            
            // Add common headers
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Students API response received successfully");

                var students = JsonSerializer.Deserialize<List<StudentApiResponse>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return students ?? new List<StudentApiResponse>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Students API call failed with status: {StatusCode}, Content: {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                throw new HttpRequestException($"Students API call failed with status: {response.StatusCode}. {errorContent}");
            }
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HTTP exceptions as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling students API");
            throw new InvalidOperationException($"Failed to fetch students: {ex.Message}", ex);
        }
        finally
        {
            // Clear authorization header for security
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<List<CenterApiResponse>> GetCentersAsync(string bearerToken)
    {
        try
        {
            var config = await LoadApiConfigurationAsync();
            var requestUri = $"{config.BaseUrl.TrimEnd('/')}{config.CentersEndpoint}";

            _logger.LogInformation("Making Centers API call to: {RequestUri}", requestUri);

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            
            // Add common headers
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Centers API response received successfully");

                var centers = JsonSerializer.Deserialize<List<CenterApiResponse>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return centers ?? new List<CenterApiResponse>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Centers API call failed with status: {StatusCode}, Content: {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                throw new HttpRequestException($"Centers API call failed with status: {response.StatusCode}. {errorContent}");
            }
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HTTP exceptions as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling centers API");
            throw new InvalidOperationException($"Failed to fetch centers: {ex.Message}", ex);
        }
        finally
        {
            // Clear authorization header for security
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}