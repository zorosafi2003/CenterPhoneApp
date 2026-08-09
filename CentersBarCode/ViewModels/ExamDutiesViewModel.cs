using System.Collections.ObjectModel;

namespace CentersBarCode.ViewModels;

public partial class ExamDutiesViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IAuthenticationService _authenticationService;

    // Event to notify when search command is executed
    public event Action? SearchCommandExecuted;

    // Event to notify when camera should be opened
    public event Func<Task>? OpenCameraRequested;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _studentName = string.Empty;

    [ObservableProperty]
    private Guid? _studentId = null;

    [ObservableProperty]
    private string _studentCode = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Group> _groups = new();

    [ObservableProperty]
    private Group? _selectedGroup;

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

        SearchText = string.Empty;
        StudentName = string.Empty;
        StudentId = null;
        StudentCode = string.Empty;
        SelectedGroup = null;
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

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            await LoadGroupsAsync();
            System.Diagnostics.Debug.WriteLine("Database initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing: {ex.Message}");
        }
    }

    private async Task LoadGroupsAsync()
    {
        try
        {
            var groups = await _databaseService.GetAllGroupsAsync();
            Groups.Clear();
            foreach (var group in groups)
            {
                Groups.Add(group);
            }
            System.Diagnostics.Debug.WriteLine($"Loaded {groups.Count} groups");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading groups: {ex.Message}");
        }
    }

    partial void OnDegreeChanged(string value)
    {
        UpdateCanSave();
    }

    partial void OnNotHaveDutiesChanged(bool value)
    {
        UpdateCanSave();
    }

    partial void OnSelectedGroupChanged(Group? value)
    {
        UpdateCanSave();
    }

    private void UpdateCanSave()
    {
        CanSave = IsStudentFound && SelectedGroup != null &&
           ((!string.IsNullOrWhiteSpace(TotalDegree) && !string.IsNullOrWhiteSpace(Degree)) || NotHaveDuties == true);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        SearchCommandExecuted?.Invoke();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", "Please enter a student code or phone number.", "OK");
            return;
        }

        await PerformSearchAsync(SearchText);
    }

    [RelayCommand]
    private async Task OpenCameraAsync()
    {
        try
        {
            SearchCommandExecuted?.Invoke();

            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status != PermissionStatus.Granted)
            {
                await Application.Current!.MainPage!.DisplayAlert("Permission Denied",
                    "Camera permission is required to scan barcodes.", "OK");
                return;
            }

            if (OpenCameraRequested != null)
            {
                await OpenCameraRequested.Invoke();
            }

            System.Diagnostics.Debug.WriteLine("Camera opened for barcode scanning in ExamDuties");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening camera: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlert("Error",
                $"Failed to open camera: {ex.Message}", "OK");
        }
    }

    private async Task PerformSearchAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", "Please enter a student code or phone number.", "OK");
            return;
        }

        try
        {
            IsSearching = true;
            Student? student = null;

            var digitsOnly = System.Text.RegularExpressions.Regex.Replace(searchText, @"\D", "");
            var isSearchingByPhone = digitsOnly.Length == 11;

            if (isSearchingByPhone)
            {
                student = await _databaseService.GetStudentByPhoneAsync(digitsOnly);
            }
            else
            {
                student = await _databaseService.GetStudentByCodeAsync(searchText);
            }

            if (student != null)
            {
                StudentId = student.Id;
                StudentName = student.StudentName;
                StudentCode = student.StudentCode;
                IsStudentFound = true;

                var existingExam = await _databaseService.GetExamByStudentIdAsync(student.Id);
                if (existingExam != null)
                {
                    CurrentExam = existingExam;
                    Degree = existingExam.Degree?.ToString() ?? string.Empty;
                    Notes = existingExam.Notes;
                    NotHaveDuties = existingExam.NotHaveDuties;
                    TotalDegree = existingExam.TotalDegree.ToString() ?? string.Empty;

                    if (existingExam.GroupId != Guid.Empty)
                    {
                        SelectedGroup = Groups.FirstOrDefault(g => g.Id == existingExam.GroupId);
                    }

                    IsEditMode = true;
                }
                else
                {
                    CurrentExam = null;
                    Degree = string.Empty;
                    Notes = string.Empty;
                    NotHaveDuties = false;
                    IsEditMode = false;

                    // Prefill group from student when creating a new exam
                    if (student.StudentGroupId.HasValue && student.StudentGroupId.Value != Guid.Empty)
                    {
                        SelectedGroup ??= Groups.FirstOrDefault(g => g.Id == student.StudentGroupId.Value);
                    }
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

    public async Task HandleBarcodeScannedAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return;
        }

        SearchText = barcode;
        await PerformSearchAsync(barcode);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!StudentId.HasValue && CanSave == true)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error",
                "يجب اختيار الطالب اولا", "OK");
            return;
        }

        if (SelectedGroup == null)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error",
                "يرجى اختيار المجموعه", "OK");
            return;
        }

        decimal? degreeValue = null;
        decimal? totalDegreeValue = null;

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

        try
        {
            IsSaving = true;

            var exam = new Exam
            {
                StudentId = StudentId.Value,
                StudentCode = StudentCode,
                StudentName = StudentName,
                GroupId = SelectedGroup.Id,
                Degree = degreeValue,
                TotalDegree = totalDegreeValue,
                Notes = Notes ?? string.Empty,
                NotHaveDuties = NotHaveDuties
            };

            await _databaseService.SaveExamAsync(exam);

            await RefreshExamBadgeAsync();

            await Application.Current!.MainPage!.DisplayAlert("Success",
                "Exam data saved successfully!", "OK");

            var savedTotalDegree = TotalDegree;
            var savedGroup = SelectedGroup;
            ClearForm();
            TotalDegree = savedTotalDegree;
            SelectedGroup = savedGroup;
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

    [RelayCommand]
    private async Task UpdateAsync()
    {
        if (CurrentExam == null)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error",
               "يجب اختيار الطالب اولا", "OK");

            return;
        }

        if (SelectedGroup == null)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error",
                "يرجى اختيار المجموعه", "OK");
            return;
        }

        decimal? degreeValue = null;
        decimal? totalDegreeValue = null;

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

        try
        {
            IsSaving = true;

            CurrentExam.GroupId = SelectedGroup.Id;
            CurrentExam.Degree = degreeValue;
            CurrentExam.TotalDegree = totalDegreeValue;
            CurrentExam.Notes = Notes ?? string.Empty;
            CurrentExam.NotHaveDuties = NotHaveDuties;

            await _databaseService.UpdateExamAsync(CurrentExam);

            await RefreshExamBadgeAsync();

            await Application.Current!.MainPage!.DisplayAlert("Success",
                "Exam data updated successfully!", "OK");

            var savedTotalDegree = TotalDegree;
            var savedGroup = SelectedGroup;
            ClearForm();
            TotalDegree = savedTotalDegree;
            SelectedGroup = savedGroup;
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

            await RefreshExamBadgeAsync();

            await Application.Current!.MainPage!.DisplayAlert("Success",
                "Exam data deleted successfully!", "OK");

            var savedTotalDegree = TotalDegree;
            var savedGroup = SelectedGroup;
            ClearForm();
            TotalDegree = savedTotalDegree;
            SelectedGroup = savedGroup;
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
