using CentersBarCode.Models;
using Microsoft.Extensions.Logging;

namespace CentersBarCode.Services;

public interface ICenterService
{
    Task<bool> ImportDataAsync(string bearerToken);
    Task<List<Center>> GetAllCentersAsync();
    Task<int> GetCentersCountAsync();
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

    public async Task<bool> ImportDataAsync(string bearerToken)
    {
        try
        {
            _logger.LogInformation("Starting centers import process");

            // 1. Fetch centers from API
            var importDataAsyncResult = await _apiService.ImportDataAsync(bearerToken);
            
            if (importDataAsyncResult == null )
            {
                _logger.LogWarning("No data received from API");
                return false;
            }

            // 2. Clear existing centers table
            await _databaseService.ClearAllCentersAsync();
            await _databaseService.ClearAllStudentsAsync();
            await _databaseService.ClearAllGroupsAsync();

            // 3. Convert API response to Center entities
            var centers = importDataAsyncResult.Centers.Select(x => new Center(x.Id, x.Name)).ToList();
            await _databaseService.SaveAllCenterAsync(centers);

            var groups = importDataAsyncResult.Groups.Select(x => new Group(x.Id, x.Name)).ToList();
            await _databaseService.SaveAllGroupsAsync(groups);

            var students = importDataAsyncResult.Students.Select(x => new Student()
            {
                Id = x.Id,
                StudentName = x.FullName,
               PhoneNumber = x.Phone,
                ParentPhone1 = x.ParentPhone1,
                StudentGroupId = x.GroupId,
                StudentGroupName = x.GroupName,
                StudentCode = x.Code,
                LastAttendance = x.LastAttendance,
                PaymentValue = x.PaymentValue
            }).ToList();
             await _databaseService.SaveAllStudentsAsync(students);

            return true;
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


}