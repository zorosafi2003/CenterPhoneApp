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
    private string _studentName = string.Empty;

    [ObservableProperty]
    private string _teacherName = string.Empty;

    public MainViewModel(IDatabaseService databaseService, ICenterService centerService, IAuthenticationService authenticationService)
    {
        _databaseService = databaseService;
        _centerService = centerService;
        _authenticationService = authenticationService;


        // Initialize empty centers list - will be populated from database
        Centers = new ObservableCollection<Center>();

        // Initialize other properties
        IsQrScannerVisible = false;
        IsPopupVisible = false;
        ScannedQrText = string.Empty;
        ScannedCode = string.Empty;
        ScannedName = string.Empty;
        ScannedCenter = string.Empty;
        IsCameraInitialized = false;
        IsSaving = false;
        StudentName = _authenticationService.FullName ?? string.Empty;
        TeacherName = _authenticationService.TeacherName ?? string.Empty;
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
            
            // If no centers in database, add some default ones
            if (Centers.Count == 0)
            {
                var defaultCenters = new[]
                {
                    new Center("1", "Center 1"),
                    new Center("2", "Center 2"), 
                    new Center("3", "Center 3"),
                    new Center("4", "Center 4"),
                    new Center("5", "Center 5")
                };
                
                foreach (var center in defaultCenters)
                {
                    await _databaseService.SaveCenterAsync(center);
                    Centers.Add(center);
                }
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
                code: ScannedCode
            );

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

    // Helper method to parse scanned QR text into components
    public void ProcessScannedQrCode(string qrText)
    {
        ScannedQrText = qrText;
        
        // Example parsing logic - adjust based on your QR code format
        // Assuming QR code format is something like "CODE|NAME|CENTER" or similar
        var parts = qrText.Split('|', ';', ',');
        
        if (parts.Length >= 1)
            ScannedCode = parts[0].Trim();
        if (parts.Length >= 2)
            ScannedName = parts[1].Trim();
        if (parts.Length >= 3)
            ScannedCenter = parts[2].Trim();
        else
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
