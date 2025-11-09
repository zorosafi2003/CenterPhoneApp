using System.Collections.ObjectModel;

namespace CentersBarCode.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ICenterService _centerService;
    private readonly IAuthenticationService _authenticationService;

    [ObservableProperty]
    private ObservableCollection<Center> _centers;

    [ObservableProperty]
    private ObservableCollection<Group> _groups;

    [ObservableProperty]
    private string _studentName = string.Empty;

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private string _teacherName = string.Empty;

    [ObservableProperty]
    private Center? _selectedCenter;

    [ObservableProperty]
    private Group? _selectedGroup;

    [ObservableProperty]
    private bool _isQrScannerVisible;

    [ObservableProperty]
    private bool _isPopupVisible;

    [ObservableProperty]
    private string _scannedQrText;

    [ObservableProperty]
    private string _scannedCode;

    [ObservableProperty]
    private string _scannedName;

    [ObservableProperty]
    private string _scannedCenter;

    [ObservableProperty]
    private string _scannedGroup;

    [ObservableProperty]
    private bool _isCameraInitialized;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isAutoScanMode;

    [ObservableProperty]
    private int _autoScanCount = 0;

    [ObservableProperty]
    private bool _showAutoScanCounter;

    public MainViewModel(IDatabaseService databaseService, ICenterService centerService, IAuthenticationService authenticationService)
    {
        _databaseService = databaseService;
        _centerService = centerService;
        _authenticationService = authenticationService;

        // Initialize empty centers and groups list - will be populated from database
        Centers = new ObservableCollection<Center>();
        Groups = new ObservableCollection<Group>();

        StudentName = string.Empty;
        TeacherName = _authenticationService.TeacherName ?? string.Empty;
        // Initialize other properties
        IsQrScannerVisible = false;
        IsPopupVisible = false;
        ScannedQrText = string.Empty;
        ScannedCode = string.Empty;
        ScannedName = string.Empty;
        ScannedCenter = string.Empty;
        IsCameraInitialized = false;
        IsSaving = false;
        IsAutoScanMode = false;
        AutoScanCount = 0;
        ShowAutoScanCounter = false;

        // Initialize database and load centers and groups
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("Database initialized successfully");

            // Load centers and groups from database
            await LoadCentersAsync();
            await LoadGroupsAsync();
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

    private async Task LoadGroupsAsync()
    {
        try
        {
            var groupsFromDb = await _databaseService.GetAllGroupsAsync();

            Groups.Clear();

            // Add "None" option to clear group filter
            Groups.Add(new Group(Guid.Empty, "None"));

            foreach (var group in groupsFromDb)
            {
                Groups.Add(group);
            }

            System.Diagnostics.Debug.WriteLine($"Loaded {Groups.Count - 1} groups (plus None option)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading groups: {ex.Message}");
        }
    }

    // Command to open QR scanner when a center is selected
    [RelayCommand]
    private void OpenQrScanner()
    {
        // Reset camera state before showing scanner
        // Show scanner UI
        IsQrScannerVisible = true;
        IsCameraInitialized = true;

        // Camera initialization happens in the code-behind
        System.Diagnostics.Debug.WriteLine("QR Scanner opened");
    }

    // Command to toggle Auto Scan mode
    [RelayCommand]
    private void ToggleAutoScan()
    {
        IsAutoScanMode = !IsAutoScanMode;
        if (IsAutoScanMode)
        {
            ShowAutoScanCounter = true;
            AutoScanCount = 0;
        }
        else
        {
            ShowAutoScanCounter = false;
        }
        System.Diagnostics.Debug.WriteLine($"Auto Scan Mode: {IsAutoScanMode}");
    }

    // Command to open QR scanner in Auto Scan mode
    [RelayCommand]
    private void OpenAutoQrScanner()
    {
        if (SelectedCenter == null)
        {
            if (Application.Current?.MainPage != null)
            {
                Application.Current.MainPage.DisplayAlert("Error",
                    "Please select a center before starting Auto Scan.", "OK");
            }
            return;
        }

        // Set Auto Scan mode to true
        IsAutoScanMode = true;
        ShowAutoScanCounter = true;
        AutoScanCount = 0;

        // Show scanner UI
        IsQrScannerVisible = true;
        IsCameraInitialized = true;

        System.Diagnostics.Debug.WriteLine("Auto QR Scanner opened");
    }

    // Direct save QR code without showing popup (for Auto Scan mode)
    public async Task<int?> SaveQrCodeDirectly(string code)
    {
        if (SelectedCenter == null || string.IsNullOrEmpty(code))
        {
            System.Diagnostics.Debug.WriteLine("Cannot save QR code: Center not selected or code is empty");
            return null;
        }

        try
        {
            // Look up student info by code first
            var student = await _databaseService.GetStudentByCodeAsync(code);

            // If a group is selected (not None), check if student belongs to that group
            if (SelectedGroup != null && SelectedGroup.Id != Guid.Empty && student != null)
            {
                if (student.StudentGroupId != SelectedGroup.Id)
                {
                    System.Diagnostics.Debug.WriteLine($"Not Allowed To Add {SelectedGroup.Name}.");

                    await Application.Current.MainPage.DisplayAlert("Save Error", $" مسموح باضافه طلاب مجموعه  {SelectedGroup.Name} فقط .", "OK");

                    return -1; // Return -1 to indicate filtered out by group
                }
            }

            var qrRecord = new QrCodeRecord(
                centerId: SelectedCenter.Id,
                code: code
            );

            // Add student info if found
            if (student != null)
            {
                qrRecord.StudentId = student.Id;
                qrRecord.StudentName = student.StudentName;
            }

            // Save to database
            var saveQrCodeRecordAsyncResult = await _databaseService.SaveQrCodeRecordAsync(qrRecord);

            if (saveQrCodeRecordAsyncResult != 0)
            {
                // Increment auto scan counter
                AutoScanCount++;

                // Refresh the records badge in AppShell
                await RefreshRecordsBadgeAsync();

                System.Diagnostics.Debug.WriteLine($"QR Code saved directly: CenterId={qrRecord.CenterId}, Code={code}, CreatedDateUtc={qrRecord.CreatedDateUtc}, AutoScanCount={AutoScanCount}");
            }

            return saveQrCodeRecordAsyncResult;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QR code directly: {ex.Message}");
            return null;
        }
    }

    // Direct save QR code without showing popup (for Auto Scan mode)
    public async Task<Student?> GetStudentInfo(string code)
    {
        try
        {
            // Look up student info by code first
            var student = await _databaseService.GetStudentByCodeAsync(code);

            // If a group is selected (not None), check if student belongs to that group
            if (SelectedGroup != null && SelectedGroup.Id != Guid.Empty && student != null)
            {
                if (student.StudentGroupId != SelectedGroup.Id)
                {
                    System.Diagnostics.Debug.WriteLine($"Student {code} does not belong to selected group {SelectedGroup.Name}. Returning null.");
                    return null;
                }
            }

            return student;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Not Found Student");
            return null;
        }
    }

    // Command to save the scanned QR code
    [RelayCommand]
    private async Task SaveQrCode()
    {
        if (SelectedCenter == null || string.IsNullOrEmpty(ScannedCode))
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Please ensure a center is selected and QR code is scanned.", "OK");
            }
            return;
        }

        try
        {
            IsSaving = true;

            var student = await _databaseService.GetStudentByCodeAsync(ScannedCode);

            // If a group is selected (not None), check if student belongs to that group
            if (SelectedGroup != null && SelectedGroup.Id != Guid.Empty && student != null)
            {
                if (student.StudentGroupId != SelectedGroup.Id)
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Save Error", $" مسموح باضافه طلاب مجموعه  {SelectedGroup.Name} فقط .", "OK");
                    }

                    IsPopupVisible = false;
                    ResetScannedData();
                    return;
                }
            }

            var qrRecord = new QrCodeRecord(
                centerId: SelectedCenter.Id,
                code: ScannedCode
            );

            // Add student info if found
            if (student != null)
            {
                qrRecord.StudentId = student.Id;
                qrRecord.StudentName = student.StudentName;
            }

            // Save to database
            await _databaseService.SaveQrCodeRecordAsync(qrRecord);

            // Refresh the records badge in AppShell
            await RefreshRecordsBadgeAsync();

            // Close the popup and reset values
            IsPopupVisible = false;
            ResetScannedData();

            System.Diagnostics.Debug.WriteLine($"QR Code saved: CenterId={qrRecord.CenterId}, Code={qrRecord.Code}, CreatedDateUtc={qrRecord.CreatedDateUtc}");
        }
        catch (Exception ex)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Failed to save QR code: {ex.Message}", "OK");
            }
            System.Diagnostics.Debug.WriteLine($"Error saving QR code: {ex}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    // Command to cancel and close the popup
    [RelayCommand]
    private void CancelQrCode()
    {
        IsPopupVisible = false;
        ResetScannedData();
    }

    // Command to close the QR scanner view
    [RelayCommand]
    private void CloseQrScanner()
    {
        IsQrScannerVisible = false;
        IsCameraInitialized = false;
        IsAutoScanMode = false;
        ShowAutoScanCounter = false;
        System.Diagnostics.Debug.WriteLine("QR Scanner closed");
    }

    // Property to determine if the scan button should be enabled
    public bool CanScan => SelectedCenter != null;

    // Update command bindings when SelectedCenter changes
    partial void OnSelectedCenterChanged(Center? value)
    {
        OnPropertyChanged(nameof(CanScan));
    }

    // Handle group selection changes
    partial void OnSelectedGroupChanged(Group? value)
    {
        if (value != null && value.Id == Guid.Empty)
        {
            System.Diagnostics.Debug.WriteLine("'None' option selected - group filter cleared");
        }
        else if (value != null)
        {
            System.Diagnostics.Debug.WriteLine($"Group selected: {value.Name} (ID: {value.Id})");
        }
    }

    // Handle camera initialization state change
    partial void OnIsCameraInitializedChanged(bool value)
    {
        System.Diagnostics.Debug.WriteLine($"Camera initialized: {value}");
    }

    // Handle auto scan mode changes
    partial void OnIsAutoScanModeChanged(bool value)
    {
        ShowAutoScanCounter = value;
        if (value)
        {
            AutoScanCount = 0;
        }
    }

    // Helper method to parse scanned QR text into components
    public async Task ProcessScannedQrCode(string qrText)
    {
        ScannedQrText = qrText;
        ScannedCode = qrText;

        var student = await _databaseService.GetStudentByCodeAsync(qrText);
        if (student != null)
        {
            ScannedName = student.StudentName;
            ScannedGroup = student.StudentGroupName;
        }

        ScannedCenter = SelectedCenter?.Name ?? string.Empty;
    }

    private void ResetScannedData()
    {
        ScannedQrText = string.Empty;
        ScannedCode = string.Empty;
        ScannedName = string.Empty;
        ScannedCenter = string.Empty;
        ScannedGroup = string.Empty;
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

    // Command to refresh centers and groups from database
    [RelayCommand]
    private async Task RefreshCentersAsync()
    {
        await LoadCentersAsync();
        await LoadGroupsAsync();
    }
}
