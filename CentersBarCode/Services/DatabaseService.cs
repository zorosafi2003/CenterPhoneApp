using SQLite;
using CentersBarCode.Models;

namespace CentersBarCode.Services;

public interface IDatabaseService
{
    Task InitializeAsync();

    // Student operations
    Task SaveAllStudentsAsync(List<Student> students);
    Task<List<Student>> GetAllStudentsAsync();
    Task<Student?> GetStudentByCodeAsync(string studentCode);
    Task<Student?> GetStudentByIdAsync(Guid studentId);
    Task<Student?> GetStudentByPhoneAsync(string phone);
    Task ClearAllStudentsAsync();

    // Center operations
    Task<int> SaveAllCenterAsync(List<Center> centers);
    Task<List<Center>> GetAllCentersAsync();
    Task<Center?> GetCenterByIdAsync(Guid centerId);
    Task ClearAllCentersAsync();

    // Group operations
    Task<int> SaveAllGroupsAsync(List<Group> groups);
    Task<List<Group>> GetAllGroupsAsync();
    Task<Group?> GetGroupByIdAsync(Guid groupId);
    Task ClearAllGroupsAsync();

    //Attendance
    Task<int> SaveQrCodeRecordAsync(QrCodeRecord record);
    Task<List<QrCodeRecord>> GetQrCodeRecordsAsync();
    Task DeleteQrCodeRecordsAsync(List<QrCodeRecord> records);
    Task<int> DeleteQrCodeRecordAsync(QrCodeRecord record);

    // Exam operations
    Task<int> SaveExamAsync(Exam exam);
    Task<int> UpdateExamAsync(Exam exam);
    Task<int> DeleteExamAsync(Exam exam);
    Task DeleteExamsAsync(List<Exam> records);
    Task<Exam?> GetExamByStudentIdAsync(Guid studentId);
    Task<List<Exam>> GetAllExamsAsync();
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
            await _database.CreateTableAsync<Group>();
            await _database.CreateTableAsync<Exam>();
        }
        return _database;
    }

    public async Task InitializeAsync()
    {
        var database = await GetDatabaseAsync();
        await database.CreateTableAsync<QrCodeRecord>();
        await database.CreateTableAsync<Student>();
        await database.CreateTableAsync<Center>();
        await database.CreateTableAsync<Group>();
        await database.CreateTableAsync<Exam>();
    }

    #region QrCodeRecord Operations

    public async Task<int> SaveQrCodeRecordAsync(QrCodeRecord record)
    {
        try
        {
            var database = await GetDatabaseAsync();

            var qrCodes = await database.Table<QrCodeRecord>().Where(r => r.Code == record.Code).ToListAsync();
            var isExist = qrCodes.FirstOrDefault(r => r.CreatedDateUtc.Date == DateTime.UtcNow.Date);

            if (isExist == null)
            {
                record.Id = Guid.NewGuid();
                return await database.InsertAsync(record);
            }
            else
            {
                return 0; // Record already exists for today
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

    public async Task DeleteQrCodeRecordsAsync(List<QrCodeRecord> records)
    {
        try
        {
            var database = await GetDatabaseAsync();
            foreach (var item in records)
            {
                await database.DeleteAsync(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting QR code records: {ex.Message}");
            throw;
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

    public async Task SaveAllStudentsAsync(List<Student> students)
    {
        try
        {
            var database = await GetDatabaseAsync();

            foreach (var item in students)
            {
                await database.InsertAsync(item);
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
    public async Task<Student?> GetStudentByPhoneAsync(string phone)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<Student>()
                               .Where(s => s.PhoneNumber == phone)
                               .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting student by code: {ex.Message}");
            return null;
        }
    }

    public async Task<Student?> GetStudentByIdAsync(Guid studentId)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<Student>()
                               .Where(s => s.Id == studentId)
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

    #endregion

    #region Center Operations

    public async Task<Center?> GetCenterByIdAsync(Guid centerId)
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

    public async Task<int> SaveAllCenterAsync(List<Center> centers)
    {
        try
        {
            var database = await GetDatabaseAsync();

            return await database.InsertAllAsync(centers);
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

    #endregion

    #region Group Operations

    public async Task<Group?> GetGroupByIdAsync(Guid groupId)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<Group>()
                               .Where(g => g.Id == groupId)
                               .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting group by ID: {ex.Message}");
            return null;
        }
    }

    public async Task<int> SaveAllGroupsAsync(List<Group> groups)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.InsertAllAsync(groups);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving groups: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Group>> GetAllGroupsAsync()
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<Group>()
                               .OrderBy(g => g.Name)
                               .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting all groups: {ex.Message}");
            return new List<Group>();
        }
    }

    public async Task ClearAllGroupsAsync()
    {
        try
        {
            var database = await GetDatabaseAsync();
            await database.DeleteAllAsync<Group>();
            System.Diagnostics.Debug.WriteLine("All groups cleared from database");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing all groups: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Exam Operations

    public async Task<int> SaveExamAsync(Exam exam)
    {
        try
        {
            var database = await GetDatabaseAsync();
            exam.Id = Guid.NewGuid();
            exam.CreatedDate = DateTime.Now;
            return await database.InsertAsync(exam);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving exam: {ex.Message}");
            throw;
        }
    }

    public async Task<int> UpdateExamAsync(Exam exam)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.UpdateAsync(exam);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating exam: {ex.Message}");
            throw;
        }
    }

    public async Task<int> DeleteExamAsync(Exam exam)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.DeleteAsync(exam);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting exam: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteExamsAsync(List<Exam> records)
    {
        try
        {
            var database = await GetDatabaseAsync();
            foreach (var item in records)
            {
                await database.DeleteAsync(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting QR code records: {ex.Message}");
            throw;
        }
    }

    public async Task<Exam?> GetExamByStudentIdAsync(Guid studentId)
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<Exam>()
                               .Where(e => e.StudentId == studentId)
                               .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting exam by student ID: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Exam>> GetAllExamsAsync()
    {
        try
        {
            var database = await GetDatabaseAsync();
            return await database.Table<Exam>()
                               .OrderByDescending(e => e.CreatedDate)
                               .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting all exams: {ex.Message}");
            return new List<Exam>();
        }
    }

    #endregion
}