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
        }
        return _database;
    }

    public async Task InitializeAsync()
    {
        var database = await GetDatabaseAsync();
        await database.CreateTableAsync<QrCodeRecord>();
    }

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
}