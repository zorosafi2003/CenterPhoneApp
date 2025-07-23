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
    private string _studentName = string.Empty;

    [ObservableProperty]
    private string _teacherName = string.Empty;

    [ObservableProperty]
    private Center? _selectedCenter;

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


        // Initialize empty centers list - will be populated from database
        Centers = new ObservableCollection<Center>();

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
    public async Task<bool> SaveQrCodeDirectly(string code)
    {
        if (SelectedCenter == null || string.IsNullOrEmpty(code))
        {
            System.Diagnostics.Debug.WriteLine("Cannot save QR code: Center not selected or code is empty");
            return false;
        }

        try
        {
            // Look up student info by code first
            var student = await _databaseService.GetStudentByCodeAsync(code);  

            var qrRecord = new QrCodeRecord(
                centerId: Guid.Parse(SelectedCenter.Id),
                code: code
            );

            // Add student info if found
            if (student != null)
            {
                qrRecord.StudentId = student.StudentId;
                qrRecord.StudentName = student.StudentName;
            }

            // Save to database
            await _databaseService.SaveQrCodeRecordAsync(qrRecord);

            // Increment auto scan counter
            AutoScanCount++;

            // Refresh the records badge in AppShell
            await RefreshRecordsBadgeAsync();

            System.Diagnostics.Debug.WriteLine($"QR Code saved directly: CenterId={qrRecord.CenterId}, Code={code}, CreatedDateUtc={qrRecord.CreatedDateUtc}, AutoScanCount={AutoScanCount}");
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving QR code directly: {ex.Message}");
            return false;
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
       
            var qrRecord = new QrCodeRecord(
                centerId: Guid.Parse(SelectedCenter.Id),
                code: ScannedCode
            );

            // Add student info if found
            if (student != null)
            {
                qrRecord.StudentId = student.StudentId;
                qrRecord.StudentName = student.StudentName;
            }

            // Save to database
            await _databaseService.SaveQrCodeRecordAsync(qrRecord);

            // Refresh the records badge in AppShell
            await RefreshRecordsBadgeAsync();

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Success",
                    $"QR Code saved successfully for {SelectedCenter.Name}", "OK");
            }

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
    public async Task  ProcessScannedQrCode(string qrText)
    {
        ScannedQrText = qrText;
        ScannedCode = qrText;

        var student = await _databaseService.GetStudentByCodeAsync(qrText);
        if (student != null)
        {
            ScannedName = student.StudentName;
        }

        ScannedCenter = SelectedCenter?.Name ?? string.Empty;
    }

    private void ResetScannedData()
    {
        ScannedQrText = string.Empty;
        ScannedCode = string.Empty;
        ScannedName = string.Empty;
        ScannedCenter = string.Empty;
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

    // Command to refresh centers from database
    [RelayCommand]
    private async Task RefreshCentersAsync()
    {
        await LoadCentersAsync();
    }
}
