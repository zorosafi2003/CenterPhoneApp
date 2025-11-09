using System.Collections.ObjectModel;

namespace CentersBarCode.ViewModels;

public partial class ExamRecordsViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private ObservableCollection<ExamRecordDisplay> _examRecords;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasRecords;

    [ObservableProperty]
    private bool _hasNoRecords;

    [ObservableProperty]
    private int _recordsCount;

    public ExamRecordsViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        ExamRecords = new ObservableCollection<ExamRecordDisplay>();
        IsLoading = false;
        HasRecords = false;
        HasNoRecords = true;
        RecordsCount = 0;
        Title = "Exam Records";
    }

    [RelayCommand]
    private async Task LoadRecordsAsync()
    {
        try
        {
            IsLoading = true;

            // Load exam records
            var examRecordsFromDb = await _databaseService.GetAllExamsAsync();

            ExamRecords.Clear();

            foreach (var exam in examRecordsFromDb)
            {
                var displayRecord = new ExamRecordDisplay
                {
                    Id = exam.Id,
                    StudentCode = exam.StudentCode,
                    StudentName = exam.StudentName,
                    Degree = exam.Degree?.ToString() ?? "N/A",
                    TotalDegree = exam.TotalDegree?.ToString() ?? "N/A",
                    NotHaveDuties = exam.NotHaveDuties,
                    Notes = exam.Notes,
                    Date = exam.CreatedDate.ToString("dd/MM/yyyy HH:mm")
                };

                ExamRecords.Add(displayRecord);
            }

            RecordsCount = ExamRecords.Count;
            HasRecords = ExamRecords.Count > 0;
            HasNoRecords = ExamRecords.Count == 0;

            // Refresh the records badge in AppShell when records are loaded
            await RefreshRecordsBadgeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading exam records: {ex.Message}");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Failed to load exam records: {ex.Message}", "OK");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteRecordAsync(ExamRecordDisplay record)
    {
        try
        {
            if (Application.Current?.MainPage != null)
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert("Confirm Delete",
                    $"Are you sure you want to delete the exam record for '{record.StudentName}'?",
                    "Yes", "No");

                if (!confirm) return;
            }

            var exam = new Exam
            {
                Id = record.Id
            };

            await _databaseService.DeleteExamAsync(exam);
            
            ExamRecords.Remove(record);
            RecordsCount = ExamRecords.Count;
            HasRecords = ExamRecords.Count > 0;
            HasNoRecords = ExamRecords.Count == 0;

            // Refresh the records badge in AppShell
            await RefreshRecordsBadgeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting exam record: {ex.Message}");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Failed to delete exam record: {ex.Message}", "OK");
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadRecordsAsync();
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

// Display model for exam records in the UI
public class ExamRecordDisplay
{
    public Guid Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string TotalDegree { get; set; } = string.Empty;
    public bool NotHaveDuties { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}
