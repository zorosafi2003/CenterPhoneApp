namespace CentersBarCode.ViewModels;

public partial class AppShellViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IStudentService _studentService;
    private readonly ICenterService _centerService;
    private readonly IGoogleAuthService _googleAuthService; // Added
    private readonly IApiService _apiService;

    [ObservableProperty]
    private int _recordsCount;

    [ObservableProperty]
    private bool _hasBadge;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _userEmail = string.Empty;

    [ObservableProperty]
    private string _teacherName = string.Empty;

    [ObservableProperty]
    private string _studentName = string.Empty;

    [ObservableProperty]
    private bool _showFlyoutItems;

    [ObservableProperty]
    private int _studentsCount;

    [ObservableProperty]
    private bool _hasStudentsBadge;

    [ObservableProperty]
    private bool _isAutoExporting;

    [ObservableProperty]
    private int _centersCount;

    [ObservableProperty]
    private bool _hasCentersBadge;

    [ObservableProperty]
    private bool _isAutoImporting;

    public AppShellViewModel(IDatabaseService databaseService, IAuthenticationService authenticationService,
        IStudentService studentService, ICenterService centerService, IGoogleAuthService googleAuthService, IApiService apiService)
    {
        _databaseService = databaseService;
        _authenticationService = authenticationService;
        _studentService = studentService;
        _centerService = centerService;
        _googleAuthService = googleAuthService; // Added
        _apiService = apiService;

        RecordsCount = 0;
        HasBadge = false;
        StudentsCount = 0;
        HasStudentsBadge = false;
        CentersCount = 0;
        HasCentersBadge = false;
        IsAuthenticated = _authenticationService.IsAuthenticated;
        UserEmail = _authenticationService.UserEmail ?? string.Empty;
        TeacherName = _authenticationService.TeacherName ?? string.Empty;
        StudentName = string.Empty;
        ShowFlyoutItems = _authenticationService.IsAuthenticated;
        Title = "Centers Barcode App";

        // Subscribe to authentication state changes
        _authenticationService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        // Initialize the badge counts
        _ = Task.Run(async () =>
        {
            await UpdateRecordsCountAsync();
            await UpdateStudentsCountAsync();
            await UpdateCentersCountAsync();
        });
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsAuthenticated = isAuthenticated;
            UserEmail = _authenticationService.UserEmail ?? string.Empty;
            TeacherName = _authenticationService.TeacherName ?? string.Empty;
            StudentName = string.Empty;
            ShowFlyoutItems = isAuthenticated;

            System.Diagnostics.Debug.WriteLine($"Authentication state changed: {isAuthenticated}, ShowFlyoutItems: {ShowFlyoutItems}");

            // Auto-import data after successful authentication
            if (isAuthenticated)
            {
                _ = Task.Run(async () => await AutoImportDataAsync());
            }
        });
    }

    public async Task UpdateRecordsCountAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            var records = await _databaseService.GetQrCodeRecordsAsync();
            var newCount = records.Count;

            // Only update if the count has changed or if it's the first time
            if (RecordsCount != newCount)
            {
                RecordsCount = newCount;
                HasBadge = RecordsCount > 0;
                System.Diagnostics.Debug.WriteLine($"AppShell Badge updated: {RecordsCount} records");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating AppShell records count: {ex.Message}");
            RecordsCount = 0;
            HasBadge = false;
        }
    }

    public async Task UpdateStudentsCountAsync()
    {
        try
        {
            var count = await _studentService.GetStudentsCountAsync();

            if (StudentsCount != count)
            {
                StudentsCount = count;
                HasStudentsBadge = StudentsCount > 0;
                System.Diagnostics.Debug.WriteLine($"AppShell Students Badge updated: {StudentsCount} students");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating AppShell students count: {ex.Message}");
            StudentsCount = 0;
            HasStudentsBadge = false;
        }
    }

    public async Task UpdateCentersCountAsync()
    {
        try
        {
            var count = await _centerService.GetCentersCountAsync();

            if (CentersCount != count)
            {
                CentersCount = count;
                HasCentersBadge = CentersCount > 0;
                System.Diagnostics.Debug.WriteLine($"AppShell Centers Badge updated: {CentersCount} centers");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating AppShell centers count: {ex.Message}");
            CentersCount = 0;
            HasCentersBadge = false;
        }
    }

    // Command to import students  
    private async Task ImportStudentsAsync()
    {
        if (IsAutoImporting)
        {
            System.Diagnostics.Debug.WriteLine("Student import already in progress, ignoring request");
            return;
        }

        try
        {
            IsAutoImporting = true;

            var bearerToken = _authenticationService.BearerToken;
            if (string.IsNullOrEmpty(bearerToken))
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error",
                        "You must be logged in to import students.", "OK");
                }
                return;
            }

            System.Diagnostics.Debug.WriteLine("Starting student import process");

            var success = await _studentService.ImportStudentsAsync(bearerToken);

            if (success)
            {
                await UpdateStudentsCountAsync();

                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Success",
                        $"Successfully imported {StudentsCount} students!", "OK");
                }
            }
            else
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Warning",
                        "No students were imported. Please check your connection and try again.", "OK");
                }
            }
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error during student import: {httpEx.Message}");

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Network Error",
                    "Failed to connect to the server. Please check your internet connection and try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during student import: {ex.Message}");

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Failed to import students: {ex.Message}", "OK");
            }
        }
        finally
        {
            IsAutoImporting = false;
            System.Diagnostics.Debug.WriteLine("Student import process completed");
        }
    }

    // Command to import centers   
    private async Task ImportCentersAsync()
    {
        if (IsAutoImporting)
        {
            System.Diagnostics.Debug.WriteLine("Centers import already in progress, ignoring request");
            return;
        }

        try
        {
            IsAutoImporting = true;

            var bearerToken = _authenticationService.BearerToken;
            if (string.IsNullOrEmpty(bearerToken))
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error",
                        "You must be logged in to import centers.", "OK");
                }
                return;
            }

            System.Diagnostics.Debug.WriteLine("Starting centers import process");

            var success = await _centerService.ImportCentersAsync(bearerToken);

            if (success)
            {
                await UpdateCentersCountAsync();

                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Success",
                        $"Successfully imported {CentersCount} centers!", "OK");
                }
            }
            else
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Warning",
                        "No centers were imported. Please check your connection and try again.", "OK");
                }
            }
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error during centers import: {httpEx.Message}");

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Network Error",
                    "Failed to connect to the server. Please check your internet connection and try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during centers import: {ex.Message}");

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Failed to import centers: {ex.Message}", "OK");
            }
        }
        finally
        {
            IsAutoImporting = false;
            System.Diagnostics.Debug.WriteLine("Centers import process completed");
        }
    }



    [RelayCommand]
    private async Task AutoImportDataAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_authenticationService.BearerToken))
            {
                System.Diagnostics.Debug.WriteLine("Auto-importing centers and students after successful login");

                // Import centers first, then students
                await ImportCentersAsync();
                await ImportStudentsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during auto-import: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AutoExportDataAsync()
    {

        if (IsAutoExporting)
        {
            System.Diagnostics.Debug.WriteLine("Centers import already in progress, ignoring request");
            return;
        }

        try
        {


            var bearerToken = _authenticationService.BearerToken;
            if (string.IsNullOrEmpty(bearerToken))
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error",
                        "You must be logged in to import centers.", "OK");
                }
                return;
            }

            System.Diagnostics.Debug.WriteLine("Starting centers import process");
            var list = await _databaseService.GetQrCodeRecordsAsync();
            if (list.Count > 0)
            {
                IsAutoExporting = true;

                double result = (double)list.Count / 100; // Cast to double for floating-point division
                int round = (int)Math.Ceiling(result);

                for (global::System.Int32 i = 0; i < round; i++)
                {
                    try
                    {
                        var sendedItems = list.Skip(i * 100).Take(100).ToList();

                        var model = new CreateStudentAttendanceRequest
                        {
                            Data = sendedItems.Select(record => new DataChildOfCreateStudentAttendanceRequest
                            {
                                CenterId = record.CenterId,
                                LocalId = record.Id,
                                StudentCode = record.Code,
                                StudentId = record.StudentId,
                                CreatedDate = record.CreatedDateUtc
                            }).ToList()
                        };

                        var success = await _apiService.ExportStudentAttendanceAsync(_authenticationService.BearerToken, model);

                        if (success.IsSuccess)
                        {
                            sendedItems = sendedItems.Where(x => success.Value.InsertedLocalIdArr.Contains(x.Id)).ToList();

                            await _databaseService.DeleteQrCodeRecordsAsync(sendedItems);

                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                await UpdateRecordsCountAsync();

                IsAutoExporting = false;
            }

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during centers import: {ex.Message}");

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Failed to import centers: {ex.Message}", "OK");
            }
        }
        finally
        {
            IsAutoExporting = false;
            System.Diagnostics.Debug.WriteLine("Centers import process completed");
        }
    }

    // Command to refresh the badge manually
    [RelayCommand]
    public async Task RefreshBadgeAsync()
    {
        await UpdateRecordsCountAsync();
        await UpdateStudentsCountAsync();
        await UpdateCentersCountAsync();
    }

    // Command to navigate to main page
    [RelayCommand]
    private async Task NavigateToMainAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("NavigateToMainAsync command called");
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in NavigateToMainAsync: {ex.Message}");
        }
    }

    // Command to navigate to attach card page
    [RelayCommand]
    private async Task NavigateToAttachCardAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("NavigateToAttachCardAsync command called");
            await Shell.Current.GoToAsync("//AttachCardPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in NavigateToAttachCardAsync: {ex.Message}");
        }
    }

    // Command to navigate to manual add page
    [RelayCommand]
    private async Task NavigateToManualAddAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("NavigateToManualAddAsync command called");
            await Shell.Current.GoToAsync("//ManualAddPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in NavigateToManualAddAsync: {ex.Message}");
        }
    }

    // Command to navigate to records page
    [RelayCommand]
    private async Task NavigateToRecordsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("NavigateToRecordsAsync command called");
            await Shell.Current.GoToAsync("//RecordsPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in NavigateToRecordsAsync: {ex.Message}");
        }
    }

    // Command to logout
    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Logout command initiated");
            await _authenticationService.LogoutAsync();
            await _googleAuthService.SignOutAsync(); // Added to clear Google account

            // Navigate to login page
            if (Shell.Current != null)
            {
                System.Diagnostics.Debug.WriteLine("Navigating to LoginPage after logout");
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
        }
    }
}