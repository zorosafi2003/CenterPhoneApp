using CentersBarCode.Models;
using Microsoft.Extensions.Logging;

namespace CentersBarCode.Services;

public interface IStudentService
{
    Task<bool> ImportStudentsAsync(string bearerToken);
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

    public async Task<bool> ImportStudentsAsync(string bearerToken)
    {
        try
        {
            _logger.LogInformation("Starting student import process");

            // 1. Fetch students from API
            var studentsFromApi = await _apiService.GetStudentsAsync(bearerToken);
            
            if (studentsFromApi == null || !studentsFromApi.Any())
            {
                _logger.LogWarning("No students received from API");
                return false;
            }

            _logger.LogInformation("Received {Count} students from API", studentsFromApi.Count);

            // 2. Clear existing students table
            await ClearAllStudentsAsync();
            _logger.LogInformation("Cleared existing students from database");

            // 3. Convert API response to Student entities
            var students = studentsFromApi.Select(apiStudent => new Student
            {
                StudentId = apiStudent.Id,
                StudentCode = apiStudent.Code,
                StudentName = apiStudent.FullName,
                StudentGroup = apiStudent.GroupName,
                PhoneNumber = apiStudent.PhoneNumber,
                ParentPhone1 = apiStudent.ParentPhone1,
                ParentPhone2 = apiStudent.ParentPhone2,
                CreatedOn = DateTime.UtcNow
            }).ToList();

            // 4. Save all students to database
            int savedCount = 0;
            foreach (var student in students)
            {
                try
                {
                    await _databaseService.SaveStudentAsync(student);
                    savedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save student: {StudentId}", student.StudentId);
                }
            }

            _logger.LogInformation("Successfully imported {SavedCount} out of {TotalCount} students", 
                savedCount, students.Count);

            return savedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during student import process");
            throw;
        }
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