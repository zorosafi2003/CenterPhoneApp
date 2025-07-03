using System.Collections.ObjectModel;

namespace CentersBarCode.ViewModels;

public partial class RecordsViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private ObservableCollection<QrCodeRecordDisplay> _records;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasRecords;

    [ObservableProperty]
    private bool _hasNoRecords;

    [ObservableProperty]
    private int _recordsCount;

    public RecordsViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        Records = new ObservableCollection<QrCodeRecordDisplay>();
        IsLoading = false;
        HasRecords = false;
        HasNoRecords = true;
        RecordsCount = 0;
        Title = "QR Code Records";
    }

    [RelayCommand]
    private async Task LoadRecordsAsync()
    {
        try
        {
            IsLoading = true;

            var qrRecords = await _databaseService.GetQrCodeRecordsAsync();

            Records.Clear();

            foreach (var record in qrRecords)
            {
                var displayRecord = new QrCodeRecordDisplay
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = ExtractNameFromCode(record.Code), // Extract name from the QR code if available
                    Date = record.CreatedDateUtc.ToString("dd/MM/yyyy HH:mm"),
                    CenterId = record.CenterId
                };

                Records.Add(displayRecord);
            }

            RecordsCount = Records.Count;
            HasRecords = Records.Count > 0;
            HasNoRecords = Records.Count == 0;

            // Refresh the records badge in AppShell when records are loaded
            await RefreshRecordsBadgeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading records: {ex.Message}");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Failed to load records: {ex.Message}", "OK");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteRecordAsync(QrCodeRecordDisplay record)
    {
        try
        {
            if (Application.Current?.MainPage != null)
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert("Confirm Delete",
                    $"Are you sure you want to delete the record with code '{record.Code}'?",
                    "Yes", "No");

                if (!confirm) return;
            }

            var qrRecord = new QrCodeRecord
            {
                Id = record.Id,
                Code = record.Code,
                CenterId = record.CenterId
            };

           // await _databaseService.DeleteQrCodeRecordAsync(qrRecord);
            
            Records.Remove(record);
            RecordsCount = Records.Count;
            HasRecords = Records.Count > 0;
            HasNoRecords = Records.Count == 0;

            // Refresh the records badge in AppShell
            await RefreshRecordsBadgeAsync();

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Success",
                    "Record deleted successfully", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting record: {ex.Message}");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Failed to delete record: {ex.Message}", "OK");
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadRecordsAsync();
    }

    private string ExtractNameFromCode(string code)
    {
        // Extract name from QR code if it contains delimiters
        // Assuming format like "CODE|NAME|CENTER" or similar
        var parts = code.Split('|', ';', ',');
        return parts.Length >= 2 ? parts[1].Trim() : "N/A";
    }

    // Method to get records count for badge
    public async Task<int> GetRecordsCountAsync()
    {
        try
        {
            var records = await _databaseService.GetQrCodeRecordsAsync();
            return records.Count;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting records count: {ex.Message}");
            return 0;
        }
    }

    private async Task RefreshRecordsBadgeAsync()
    {
        try
        {
            if (Shell.Current is AppShell appShell)
            {
                await appShell.RefreshRecordsBadgeAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing records badge: {ex.Message}");
        }
    }
}

// Display model for records in the UI
public class QrCodeRecordDisplay
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public Guid CenterId { get; set; }
}