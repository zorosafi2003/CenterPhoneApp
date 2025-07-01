using CentersBarCode.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel.Communication;
using System.Net.Http.Headers;
using System.Text.Json;
using ZXing.Aztec.Internal;

namespace CentersBarCode.Services;

public interface IApiService
{
    Task<List<StudentApiResponse>> GetStudentsAsync(string bearerToken);
    Task<List<CenterApiResponse>> GetCentersAsync(string bearerToken);
    Task<ApiConfiguration> LoadApiConfigurationAsync();
    Task<Result<ValidateAuthenticationResponse>> ValidateAuthenticationAsync(string email, string token);
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
                BaseUrl = "https://e9c0-41-237-212-199.ngrok-free.app",
                StudentsEndpoint = "/centers-api/students/list",
                CentersEndpoint = "/centers-api/centers/list",
                AuthenticationEndpoint = "/auth/login"
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
            var authRequest = new
            {
                Skip = 0,
                Take = 1000
            };

            var jsonContent = JsonSerializer.Serialize(authRequest);
            using var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            // Add common headers
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.PostAsync(requestUri, content);
            if (response.IsSuccessStatusCode)
            {

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Students API response received successfully");

                var studentsResult = JsonSerializer.Deserialize<Result<DataAndCountDto<StudentApiResponse>>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (studentsResult.IsSuccess)
                {
                    return studentsResult.Value.Data;
                }
                else
                {
                    throw new Exception(studentsResult.Error.Description);
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Students API call failed with status: {StatusCode}, Content: {ErrorContent}",
                    response.StatusCode, errorContent);

                throw new HttpRequestException($"Students API call failed with status: {response.StatusCode}. {errorContent}");
            }
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
            // Create request payload
            var authRequest = new
            {
                Skip = 0,
                Take = 1000
            };

            var jsonContent = JsonSerializer.Serialize(authRequest);
            using var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            // Add common headers
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.PostAsync(requestUri, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Centers API response received successfully");

                var centersResult = JsonSerializer.Deserialize<Result<DataAndCountDto<CenterApiResponse>>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (centersResult.IsSuccess)
                {
                    return centersResult.Value.Data;
                }
                else
                {
                    throw new Exception(centersResult.Error.Description);
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Centers API call failed with status: {StatusCode}, Content: {ErrorContent}",
                    response.StatusCode, errorContent);

                throw new HttpRequestException($"Centers API call failed with status: {response.StatusCode}. {errorContent}");
            }
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

    public async Task<Result<ValidateAuthenticationResponse>> ValidateAuthenticationAsync(string email, string token)
    {
        try
        {
            var config = await LoadApiConfigurationAsync();
            var requestUri = $"{config.BaseUrl.TrimEnd('/')}{config.AuthenticationEndpoint}";

            _logger.LogInformation("Making Authentication validation API call to: {RequestUri}", requestUri);

            // Create request payload
            var authRequest = new
            {
                UserName = email,
                Password = token,
                LoginType = 1
            };

            var jsonContent = JsonSerializer.Serialize(authRequest);
            using var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Add common headers
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.PostAsync(requestUri, content);

            var responseContent = await response.Content.ReadAsStringAsync();

            var validateResponse = JsonSerializer.Deserialize<Result<ValidateAuthenticationResponse>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });


            return validateResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating authentication");
            return new Result<ValidateAuthenticationResponse>
            {
                IsSuccess = false,
                Error = new Error
                {
                    Description = $"Failed to validate authentication: {ex.Message}",
                    Code = "ApiValidationError"
                }
            };
        }
    }
}