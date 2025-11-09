using CentersBarCode.Models;
using Microsoft.Extensions.Logging;

namespace CentersBarCode.Services;

public interface IStudentService
{
    Task<List<Student>> GetAllStudentsAsync();
    Task<int> GetStudentsCountAsync();
    Task ClearAllStudentsAsync();
    Task<Student?> GetStudentByCodeAsync(string studentCode);
}

public class StudentService : IStudentService
{
    private readonly IDatabaseService _databaseService;
    private readonly IApiService _apiService;
    private readonly ILogger<StudentService> _logger;

    public StudentService(IDatabaseService databaseService, IApiService apiService, ILogger<StudentService> logger)
    {
        _databaseService = databaseService;
        _apiService = apiService;
        _logger = logger;
    }


    public async Task<List<Student>> GetAllStudentsAsync()
    {
        try
        {
            return await _databaseService.GetAllStudentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all students");
            return new List<Student>();
        }
    }

    public async Task<int> GetStudentsCountAsync()
    {
        try
        {
            var students = await GetAllStudentsAsync();
            return students.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students count");
            return 0;
        }
    }

    public async Task ClearAllStudentsAsync()
    {
        try
        {
            await _databaseService.ClearAllStudentsAsync();
            _logger.LogInformation("Cleared all students from database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing students");
            throw;
        }
    }

    public async Task<Student?> GetStudentByCodeAsync(string studentCode)
    {
        try
        {
            return await _databaseService.GetStudentByCodeAsync(studentCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student by code: {StudentCode}", studentCode);
            return null;
        }
    }
}