using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace CentersBarCode.ViewModels;

public partial class ManualAddViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ICenterService _centerService;
    private readonly IAuthenticationService _authenticationService;
    
    // Event to notify when search command is executed
    public event Action SearchCommandExecuted;

    [ObservableProperty]
    private ObservableCollection<Center> _centers;

    [ObservableProperty]
    private Center? _selectedCenter;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Student> _searchResults;

    [ObservableProperty]
    private Student? _selectedStudent;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _teacherName = string.Empty;

    [ObservableProperty]
    private bool _isSearchEnabled;

    [ObservableProperty]
    private bool _hasResults;

    public ManualAddViewModel(IDatabaseService databaseService, ICenterService centerService, IAuthenticationService authenticationService)
    {
        _databaseService = databaseService;
        _centerService = centerService;
        _authenticationService = authenticationService;

        // Initialize collections
        Centers = new ObservableCollection<Center>();
        SearchResults = new ObservableCollection<Student>();

        // Initialize properties
        SelectedCenter = null;
        SelectedStudent = null;
        SearchText = string.Empty;
        IsSearching = false;
        IsSaving = false;
        HasResults = false;
        TeacherName = _authenticationService.TeacherName ?? string.Empty;
        Title = "Manual Attendance";

        // Initialize database and load centers
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("Database initialized successfully");

            // Load centers from database
            await LoadCentersAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing: {ex.Message}");
        }
    }

    private async Task LoadCentersAsync()
    {
        try
        {
            var centersFromDb = await _centerService.GetAllCentersAsync();

            Centers.Clear();
            foreach (var center in centersFromDb)
            {
                Centers.Add(center);
            }

            System.Diagnostics.Debug.WriteLine($"Loaded {Centers.Count} centers");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading centers: {ex.Message}");
        }
    }

    // Handle search text changes - now just updates IsSearchEnabled
    partial void OnSearchTextChanged(string value)
    {
        // Enable search if we have at least 3 characters or a code pattern
        IsSearchEnabled = !string.IsNullOrWhiteSpace(value) && (value.Length >= 3 || Regex.IsMatch(value, @"^\d{3,}$"));

        if (string.IsNullOrWhiteSpace(value))
        {
            // Clear results if search box is cleared
            SearchResults.Clear();
            HasResults = false;
        }
    }

    // Property to determine if we can save attendance
    public bool CanSaveAttendance => SelectedCenter != null && SelectedStudent != null;

    partial void OnSelectedCenterChanged(Center? value)
    {
        OnPropertyChanged(nameof(CanSaveAttendance));
    }

    partial void OnSelectedStudentChanged(Student? value)
    {
        OnPropertyChanged(nameof(CanSaveAttendance));
    }

    // Command to perform the search - now only triggered by button
    [RelayCommand]
    private async Task SearchAsync()
    {
        // Notify that the search command is executed (for keyboard dismissal)
        SearchCommandExecuted?.Invoke();
        
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResults.Clear();
            HasResults = false;
            return;
        }

        try
        {
            IsSearching = true;
            SearchResults.Clear();


            var allStudents = await _databaseService.GetAllStudentsAsync();
            var matchingStudents = allStudents
                .Where(s => s.StudentCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           s.PhoneNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           s.ParentPhone1.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           s.ParentPhone2.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .Take(10) // Limit to 10 results
                .ToList();

            foreach (var matchingStudent in matchingStudents)
            {
                SearchResults.Add(matchingStudent);
            }

            HasResults = SearchResults.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during search: {ex.Message}");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Failed to search: {ex.Message}", "OK");
            }
        }
        finally
        {
            IsSearching = false;
        }
    }

    // Command to save attendance for the selected student
    [RelayCommand]
    private async Task SaveAttendanceAsync()
    {
        if (SelectedCenter == null || SelectedStudent == null)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Please select both a center and a student.", "OK");
            }
            return;
        }

        try
        {
            IsSaving = true;

            // Confirm before saving
            bool confirmResult = false;
            if (Application.Current?.MainPage != null)
            {
                confirmResult = await Application.Current.MainPage.DisplayAlert("Confirm",
                    $"Are you sure you want to add attendance ?",
                    "Yes", "No");
            }

            if (!confirmResult)
            {
                return;
            }

            // Create QR code record - need to convert center ID string to Guid
            Guid centerGuid;
            if (!Guid.TryParse(SelectedCenter.Id, out centerGuid))
            {
                // If the center ID is not a valid GUID, create a new one based on the string
                centerGuid = Guid.NewGuid();
                System.Diagnostics.Debug.WriteLine($"Created new GUID {centerGuid} for center ID {SelectedCenter.Id}");
            }

            var qrRecord = new QrCodeRecord(
                centerId: centerGuid,
                code: SelectedStudent.StudentCode
            )
            {
                StudentId = SelectedStudent.StudentId,
                StudentName = SelectedStudent.StudentName
            };

            // Save to database
            await _databaseService.SaveQrCodeRecordAsync(qrRecord);

            // Refresh the records badge in AppShell
            await RefreshRecordsBadgeAsync();

            // Reset selection
            SelectedStudent = null;
            SearchResults.Clear();
            HasResults = false;
            SearchText = string.Empty;

            System.Diagnostics.Debug.WriteLine($"Attendance saved: CenterId={qrRecord.CenterId}, Code={qrRecord.Code}, StudentName={qrRecord.StudentName}");
        }
        catch (Exception ex)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Failed to save attendance: {ex.Message}", "OK");
            }
            System.Diagnostics.Debug.WriteLine($"Error saving attendance: {ex}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    // Command to refresh centers from database
    [RelayCommand]
    private async Task RefreshCentersAsync()
    {
        await LoadCentersAsync();
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