using System.Collections.ObjectModel;

namespace CentersBarCode.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;

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

    public MainViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        
        // Initialize centers list with sample data
        Centers = new ObservableCollection<Center>
        {
            new Center("Center 1"),
            new Center("Center 2"), 
            new Center("Center 3"),
            new Center("Center 4"),
            new Center("Center 5")
        };

        // Initialize other properties
        IsQrScannerVisible = false;
        IsPopupVisible = false;
        ScannedQrText = string.Empty;
        ScannedCode = string.Empty;
        ScannedName = string.Empty;
        ScannedCenter = string.Empty;
        IsCameraInitialized = false;
        IsSaving = false;

        // Initialize database
        InitializeDatabaseAsync();
    }

    private async void InitializeDatabaseAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("Database initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing database: {ex.Message}");
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
            
            // Create QR code record with the three required parameters
            var qrRecord = new QrCodeRecord(
                centerId: SelectedCenter.Id,
                code: ScannedCode
            );
            // CreatedDateUtc is automatically set in the constructor

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
}
