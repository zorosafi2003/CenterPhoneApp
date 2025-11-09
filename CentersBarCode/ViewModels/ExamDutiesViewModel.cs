using System.Text.RegularExpressions;

namespace CentersBarCode.ViewModels;

public partial class ExamDutiesViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IAuthenticationService _authenticationService;
    
    // Event to notify when search command is executed
    public event Action? SearchCommandExecuted;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _studentName = string.Empty;

    [ObservableProperty]
    private Guid? _studentId = null;

    [ObservableProperty]
    private string _studentCode = string.Empty;

    [ObservableProperty]
    private string _totalDegree = string.Empty;

    [ObservableProperty]
    private string _degree = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private bool _notHaveDuties;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _teacherName = string.Empty;

    [ObservableProperty]
    private bool _isStudentFound;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private Exam? _currentExam;

    [ObservableProperty]
    private bool _canSave;

    public ExamDutiesViewModel(IDatabaseService databaseService, IAuthenticationService authenticationService)
    {
        _databaseService = databaseService;
        _authenticationService = authenticationService;

        // Initialize properties
        SearchText = string.Empty;
        StudentName = string.Empty;
        StudentId = null;
        StudentCode = string.Empty;
        TotalDegree = string.Empty;
        Degree = string.Empty;
        Notes = string.Empty;
        NotHaveDuties = false;
        IsSearching = false;
        IsSaving = false;
        IsStudentFound = false;
        IsEditMode = false;
        CanSave = false;
        TeacherName = _authenticationService.TeacherName ?? string.Empty;
        Title = "Exam and Duties";
        
        // Initialize database
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("Database initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing: {ex.Message}");
        }
    }

    // Handle changes to degree or notHaveDuties to update CanSave
    partial void OnDegreeChanged(string value)
    {
        UpdateCanSave();
    }

    partial void OnNotHaveDutiesChanged(bool value)
    {
        UpdateCanSave();
    }

    private void UpdateCanSave()
    {
        CanSave = IsStudentFound &&
           ( (!string.IsNullOrWhiteSpace(TotalDegree) && !string.IsNullOrWhiteSpace(Degree) ) || NotHaveDuties == true);
    }

    // Command to perform the search
    [RelayCommand]
    private async Task SearchAsync()
    {
        // Notify that the search command is executed (for keyboard dismissal)
        SearchCommandExecuted?.Invoke();
        
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", "Please enter a student code or phone number.", "OK");
            return;
        }

        try
        {
            IsSearching = true;
            Student? student = null;

            // Determine if searching by phone or code
            var digitsOnly = Regex.Replace(SearchText, @"\D", "");
            var isSearchingByPhone = digitsOnly.Length == 11;

            if (isSearchingByPhone)
            {
                student = await _databaseService.GetStudentByPhoneAsync(digitsOnly);
            }
            else
            {
                student = await _databaseService.GetStudentByCodeAsync(SearchText);
            }

            if (student != null)
            {
                StudentId = student.Id;
                StudentName = student.StudentName;
                StudentCode = student.StudentCode;
                IsStudentFound = true;

                // Check if student already has an exam record
                var existingExam = await _databaseService.GetExamByStudentIdAsync(student.Id);
                if (existingExam != null)
                {
                    // Load existing data
                    CurrentExam = existingExam;
                    Degree = existingExam.Degree?.ToString() ?? string.Empty;
                    Notes = existingExam.Notes;
                    NotHaveDuties = existingExam.NotHaveDuties;
                    TotalDegree = existingExam.TotalDegree.ToString();
                    IsEditMode = true;
                }
                else
                {
                    // Clear form for new entry
                    CurrentExam = null;
                    Degree = string.Empty;
                    Notes = string.Empty;
                    NotHaveDuties = false;
                    IsEditMode = false;
                }

                UpdateCanSave();
            }
            else
            {
                IsStudentFound = false;
                IsEditMode = false;
                StudentName = string.Empty;
                StudentId = null;
                StudentCode = string.Empty;
                Degree = string.Empty;
                Notes = string.Empty;
                NotHaveDuties = false;
                CanSave = false;
                
                await Application.Current!.MainPage!.DisplayAlert("Not Found", 
                    "Student not found. Please check the code or phone number.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during search: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlert("Error", 
                $"Failed to search: {ex.Message}", "OK");
        }
        finally
        {
            IsSearching = false;
        }
    }

    // Command to save exam data
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!StudentId.HasValue && CanSave == true  )
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", 
                "يجب اختيار الطالب اولا", "OK");
            return;
        }

        decimal? degreeValue = null;
        decimal? totalDegreeValue = null;

        if (NotHaveDuties == false)
        {

            if (!string.IsNullOrWhiteSpace(Degree))
            {
                if (!decimal.TryParse(TotalDegree, out decimal parsedTotalDegree))
                {
                    await Application.Current!.MainPage!.DisplayAlert("Error",
                        "يرجى ادخال الدرجه الكليه للاختبار", "OK");
                    return;
                }
                totalDegreeValue = parsedTotalDegree;
            }

            if (!string.IsNullOrWhiteSpace(Degree))
            {
                if (!decimal.TryParse(Degree, out decimal parsedDegree))
                {
                    await Application.Current!.MainPage!.DisplayAlert("Error",
                        "يرجى ادخال درجه الطالب بصورة صحيحه", "OK");
                    return;
                }
                degreeValue = parsedDegree;
            }
        }

        try
        {
            IsSaving = true;

            var exam = new Exam
            {
                StudentId = StudentId.Value,
                StudentCode = StudentCode,
                StudentName = StudentName,
                Degree = degreeValue,
                TotalDegree = totalDegreeValue,
                Notes = Notes ?? string.Empty,
                NotHaveDuties = NotHaveDuties
            };

            await _databaseService.SaveExamAsync(exam);

            // Refresh the exam badge
            await RefreshExamBadgeAsync();

            await Application.Current!.MainPage!.DisplayAlert("Success", 
                "Exam data saved successfully!", "OK");

            // Clear form but keep TotalDegree
            var savedTotalDegree = TotalDegree;
            ClearForm();
            TotalDegree = savedTotalDegree;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving exam: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlert("Error", 
                $"Failed to save: {ex.Message}", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }

    // Command to update exam data
    [RelayCommand]
    private async Task UpdateAsync()
    {
        if (CurrentExam == null || !StudentId.HasValue || string.IsNullOrWhiteSpace(TotalDegree))
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", 
                "Please fill in all required fields.", "OK");
            return;
        }

        if (!decimal.TryParse(TotalDegree, out decimal totalDegreeValue))
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", 
                "Please enter a valid total degree.", "OK");
            return;
        }

        decimal? degreeValue = null;
        if (!string.IsNullOrWhiteSpace(Degree))
        {
            if (!decimal.TryParse(Degree, out decimal parsedDegree))
            {
                await Application.Current!.MainPage!.DisplayAlert("Error", 
                    "Please enter a valid degree.", "OK");
                return;
            }
            degreeValue = parsedDegree;
        }

        try
        {
            IsSaving = true;

            CurrentExam.Degree = degreeValue;
            CurrentExam.TotalDegree = totalDegreeValue;
            CurrentExam.Notes = Notes ?? string.Empty;
            CurrentExam.NotHaveDuties = NotHaveDuties;

            await _databaseService.UpdateExamAsync(CurrentExam);

            // Refresh the exam badge
            await RefreshExamBadgeAsync();

            await Application.Current!.MainPage!.DisplayAlert("Success", 
                "Exam data updated successfully!", "OK");

            // Clear form but keep TotalDegree
            var savedTotalDegree = TotalDegree;
            ClearForm();
            TotalDegree = savedTotalDegree;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating exam: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlert("Error", 
                $"Failed to update: {ex.Message}", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }

    // Command to delete exam data
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (CurrentExam == null)
        {
            return;
        }

        try
        {
            bool confirm = await Application.Current!.MainPage!.DisplayAlert("Confirm", 
                "Are you sure you want to delete this exam record?", "Yes", "No");

            if (!confirm)
            {
                return;
            }

            IsSaving = true;

            await _databaseService.DeleteExamAsync(CurrentExam);

            // Refresh the exam badge
            await RefreshExamBadgeAsync();

            await Application.Current!.MainPage!.DisplayAlert("Success", 
                "Exam data deleted successfully!", "OK");

            // Clear form but keep TotalDegree
            var savedTotalDegree = TotalDegree;
            ClearForm();
            TotalDegree = savedTotalDegree;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting exam: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlert("Error", 
                $"Failed to delete: {ex.Message}", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task RefreshExamBadgeAsync()
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
            System.Diagnostics.Debug.WriteLine($"Error refreshing exam badge: {ex.Message}");
        }
    }

    private void ClearForm()
    {
        SearchText = string.Empty;
        StudentName = string.Empty;
        StudentId = null;
        StudentCode = string.Empty;
        Degree = string.Empty;
        Notes = string.Empty;
        NotHaveDuties = false;
        IsStudentFound = false;
        IsEditMode = false;
        CurrentExam = null;
        CanSave = false;
    }
}
