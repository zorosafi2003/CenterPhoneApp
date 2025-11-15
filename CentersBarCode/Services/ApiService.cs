using CentersBarCode.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel.Communication;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CentersBarCode.Services;

public interface IApiService
{
    Task<Result<ValidateAuthenticationResponse>> ValidateAuthenticationAsync(string email, string token);
    Task<GetDataPhoneAppQueryResult> ImportDataAsync(string bearerToken);
    Task<Result<SetDataPhoneAppCommandResult>> ExportDataAsync(string bearerToken, SetDataPhoneAppCommandRequest model);
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

            throw ex;
        }
    }

    public async Task<GetDataPhoneAppQueryResult> ImportDataAsync(string bearerToken)
    {
        try
        {
            var config = await LoadApiConfigurationAsync();
            var requestUri = $"{config.BaseUrl.TrimEnd('/')}{config.ImportDataEndpoint}";

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            // Add common headers
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.GetAsync(requestUri);
            if (response.IsSuccessStatusCode)
            {

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Students API response received successfully");

                var studentsResult = JsonSerializer.Deserialize<Result<GetDataPhoneAppQueryResult>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (studentsResult.IsSuccess)
                {
                    return studentsResult.Value ?? new GetDataPhoneAppQueryResult();
                }
                else
                {
                    throw new Exception(studentsResult.Error?.Description ?? "Unknown error");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Students API call failed with status: {StatusCode}, Content: {ErrorContent}",
                    response.StatusCode, errorContent);

                throw new HttpRequestException($"Import data API call failed with status: {response.StatusCode}. {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling Import data API");
            throw new InvalidOperationException($"Failed to fetch Import data: {ex.Message}", ex);
        }
        finally
        {
            // Clear authorization header for security
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<Result<SetDataPhoneAppCommandResult>> ExportDataAsync(string bearerToken, SetDataPhoneAppCommandRequest model)
    {
        try
        {
            var config = await LoadApiConfigurationAsync();
            var requestUri = $"{config.BaseUrl.TrimEnd('/')}{config.ExportDataEndPoint}";

            _logger.LogInformation("Making Export Student Attendance API call to: {RequestUri}", requestUri);


            var jsonContent = JsonSerializer.Serialize(model);
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
                _logger.LogInformation("Export data API response received successfully");

                var result = JsonSerializer.Deserialize<Result<SetDataPhoneAppCommandResult>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ;
            }
            else
            {
                return new Result<SetDataPhoneAppCommandResult>
                {
                    IsSuccess = false,
                    Error = new Error
                    {
                        Code = response.StatusCode.ToString(),
                        Description = $"Export data failed with status: {response.StatusCode}. {response.Content}"
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while exporting data");
            return new Result<SetDataPhoneAppCommandResult>
            {
                IsSuccess = false,
                Error = new Error
                {
                    Code = "ExportError",
                    Description = $"Failed to Export data : {ex.Message}"
                }
            };
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

            return validateResponse ?? new Result<ValidateAuthenticationResponse>
            {
                IsSuccess = false,
                Error = new Error
                {
                    Description = "Invalid response from authentication API",
                    Code = "InvalidResponse"
                }
            };
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