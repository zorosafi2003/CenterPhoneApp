using SQLite;
using CentersBarCode.Models;

namespace CentersBarCode.Services;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<int> SaveQrCodeRecordAsync(QrCodeRecord record);
    Task<List<QrCodeRecord>> GetQrCodeRecordsAsync();
    Task<List<QrCodeRecord>> GetQrCodeRecordsByCenterIdAsync(Guid centerId);
    Task<int> DeleteQrCodeRecordAsync(QrCodeRecord record);
    
    // Student operations
    Task<int> SaveStudentAsync(Student student);
    Task<List<Student>> GetAllStudentsAsync();
    Task<Student?> GetStudentByCodeAsync(string studentCode);
    Task<Student?> GetStudentByIdAsync(string studentId);
    Task ClearAllStudentsAsync();
    Task<int> DeleteStudentAsync(Student student);
    
    // Center operations
    Task<int> SaveCenterAsync(Center center);
    Task<List<Center>> GetAllCentersAsync();
    Task<Center?> GetCenterByIdAsync(string centerId);
    Task ClearAllCentersAsync();
    Task<int> DeleteCenterAsync(Center center);
}

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _databasePath;

    public DatabaseService()
    {
        _databasePath = Path.Combine(FileSystem.AppDataDirectory, "CentersBarCode.db3");
    }

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database == null)
        {
            _database = new SQLiteAsyncConnection(_databasePath);
            await _database.CreateTableAsync<QrCodeRecord>();
            await _database.CreateTableAsync<Student>();
            await _database.CreateTableAsync<Center>();
        }
        return _database;
    }

    public async Task InitializeAsync()
    {
        var database = await GetDatabaseAsync();
        await database.CreateTableAsync<QrCodeRecord>();
        await database.CreateTableAsync<Student>();
        await database.CreateTableAsync<Center>();
    }

    #region QrCodeRecord Operations

    public async Task<int> SaveQrCodeRecordAsync(QrCodeRecord record)
    {
        try
        {
            var database = await GetDatabaseAsync();
            
            if (record.Id == 0)
            {
                // Insert new record
                return await database.InsertAsync(record);
            }
            else
            {
                // Update existing record
                return await database.UpdateAsync(record);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QR code record: {ex.Message}");
            throw;
        }
    }

    public async Task<List<QrCodeRecord>> GetQrCodeRecordsAsync()
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<QrCodeRecord>()
                               .OrderByDescending(r => r.CreatedDateUtc)
                               .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting QR code records: {ex.Message}");
            return new List<QrCodeRecord>();
        }
    }

    public async Task<List<QrCodeRecord>> GetQrCodeRecordsByCenterIdAsync(Guid centerId)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<QrCodeRecord>()
                               .Where(r => r.CenterId == centerId)
                               .OrderByDescending(r => r.CreatedDateUtc)
                               .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting QR code records by center: {ex.Message}");
            return new List<QrCodeRecord>();
        }
    }

    public async Task<int> DeleteQrCodeRecordAsync(QrCodeRecord record)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.DeleteAsync(record);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting QR code record: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Student Operations

    public async Task<int> SaveStudentAsync(Student student)
    {
        try
        {
            var database = await GetDatabaseAsync();
            
            if (student.Id == 0)
            {
                // Insert new student
                return await database.InsertAsync(student);
            }
            else
            {
                // Update existing student
                return await database.UpdateAsync(student);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving student: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Student>> GetAllStudentsAsync()
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<Student>()
                               .OrderBy(s => s.StudentName)
                               .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting all students: {ex.Message}");
            return new List<Student>();
        }
    }

    public async Task<Student?> GetStudentByCodeAsync(string studentCode)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<Student>()
                               .Where(s => s.StudentCode == studentCode)
                               .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting student by code: {ex.Message}");
            return null;
        }
    }

    public async Task<Student?> GetStudentByIdAsync(string studentId)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<Student>()
                               .Where(s => s.StudentId == studentId)
                               .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting student by ID: {ex.Message}");
            return null;
        }
    }

    public async Task ClearAllStudentsAsync()
    {
        try
        {
            var database = await GetDatabaseAsync();
            await database.DeleteAllAsync<Student>();
            System.Diagnostics.Debug.WriteLine("All students cleared from database");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing all students: {ex.Message}");
            throw;
        }
    }

    public async Task<int> DeleteStudentAsync(Student student)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.DeleteAsync(student);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting student: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Center Operations

    public async Task<int> SaveCenterAsync(Center center)
    {
        try
        {
            var database = await GetDatabaseAsync();
            
            // Check if center exists
            var existingCenter = await GetCenterByIdAsync(center.Id);
            if (existingCenter != null)
            {
                // Update existing center
                return await database.UpdateAsync(center);
            }
            else
            {
                // Insert new center
                return await database.InsertAsync(center);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving center: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Center>> GetAllCentersAsync()
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<Center>()
                               .OrderBy(c => c.Name)
                               .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting all centers: {ex.Message}");
            return new List<Center>();
        }
    }

    public async Task<Center?> GetCenterByIdAsync(string centerId)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<Center>()
                               .Where(c => c.Id == centerId)
                               .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting center by ID: {ex.Message}");
            return null;
        }
    }

    public async Task ClearAllCentersAsync()
    {
        try
        {
            var database = await GetDatabaseAsync();
            await database.DeleteAllAsync<Center>();
            System.Diagnostics.Debug.WriteLine("All centers cleared from database");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing all centers: {ex.Message}");
            throw;
        }
    }

    public async Task<int> DeleteCenterAsync(Center center)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.DeleteAsync(center);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting center: {ex.Message}");
            throw;
        }
    }

    #endregion
}