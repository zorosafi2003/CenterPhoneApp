using CentersBarCode.Models;
using Microsoft.Extensions.Logging;

namespace CentersBarCode.Services;

public interface ICenterService
{
    Task<bool> ImportCentersAsync(string bearerToken);
    Task<List<Center>> GetAllCentersAsync();
    Task<int> GetCentersCountAsync();
    Task ClearAllCentersAsync();
}

public class CenterService : ICenterService
{
    private readonly IDatabaseService _databaseService;
    private readonly IApiService _apiService;
    private readonly ILogger<CenterService> _logger;

    public CenterService(IDatabaseService databaseService, IApiService apiService, ILogger<CenterService> logger)
    {
        _databaseService = databaseService;
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<bool> ImportCentersAsync(string bearerToken)
    {
        try
        {
            _logger.LogInformation("Starting centers import process");

            // 1. Fetch centers from API
            var centersFromApi = await _apiService.GetCentersAsync(bearerToken);
            
            if (centersFromApi == null || !centersFromApi.Any())
            {
                _logger.LogWarning("No centers received from API");
                return false;
            }

            _logger.LogInformation("Received {Count} centers from API", centersFromApi.Count);

            // 2. Clear existing centers table
            await ClearAllCentersAsync();
            _logger.LogInformation("Cleared existing centers from database");

            // 3. Convert API response to Center entities
            var centers = centersFromApi.Select(apiCenter => new Center
            {
                Id = apiCenter.Id,
                Name = apiCenter.Name,
                CreatedOn = DateTime.UtcNow
            }).ToList();

            // 4. Save all centers to database
            int savedCount = 0;
            foreach (var center in centers)
            {
                try
                {
                    await _databaseService.SaveCenterAsync(center);
                    savedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save center: {CenterId}", center.Id);
                }
            }

            _logger.LogInformation("Successfully imported {SavedCount} out of {TotalCount} centers", 
                savedCount, centers.Count);

            return savedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during centers import process");
            throw;
        }
    }

    public async Task<List<Center>> GetAllCentersAsync()
    {
        try
        {
            return await _databaseService.GetAllCentersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all centers");
            return new List<Center>();
        }
    }

    public async Task<int> GetCentersCountAsync()
    {
        try
        {
            var centers = await GetAllCentersAsync();
            return centers.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting centers count");
            return 0;
        }
    }

    public async Task ClearAllCentersAsync()
    {
        try
        {
            await _databaseService.ClearAllCentersAsync();
            _logger.LogInformation("Cleared all centers from database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing centers");
            throw;
        }
    }

}