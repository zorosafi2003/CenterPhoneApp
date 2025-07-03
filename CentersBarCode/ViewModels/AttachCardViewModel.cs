using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CentersBarCode.ViewModels;

public partial class AttachCardViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IApiService _apiService;

    private string _currentPhoneNumber = string.Empty;

    [ObservableProperty]
    private string _phoneNumber;

    [ObservableProperty]
    private bool _isSearchEnabled;

    [ObservableProperty]
    private bool _isQrScannerVisible;

    [ObservableProperty]
    private bool _isCameraInitialized;

    [ObservableProperty]
    private string _scannedQrText;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _studentName = string.Empty;

    [ObservableProperty]
    private string _teacherName = string.Empty;


    public AttachCardViewModel(IDatabaseService databaseService, IAuthenticationService authenticationService,
        IApiService apiService)
    {
        _databaseService = databaseService;
        _authenticationService = authenticationService;
        _apiService = apiService;

        PhoneNumber = string.Empty;
        IsSearchEnabled = false;
        IsQrScannerVisible = false;
        IsCameraInitialized = false;
        ScannedQrText = string.Empty;
        IsProcessing = false;

        StudentName =  string.Empty;
        TeacherName = _authenticationService.TeacherName ?? string.Empty;

        Title = "Attach Card";
    }

    // Command to search/open QR scanner
    [RelayCommand]
    private void Search()
    {
        if (IsSearchEnabled)
        {
            // Store the current phone number before opening scanner
            _currentPhoneNumber = PhoneNumber;
            _studentName = StudentName;
            IsQrScannerVisible = true;
            IsCameraInitialized = true;
            System.Diagnostics.Debug.WriteLine($"Opening QR scanner for phone: {_currentPhoneNumber}");
        }
    }

    // Command to close QR scanner
    [RelayCommand]
    private void CloseQrScanner()
    {
        IsQrScannerVisible = false;
        IsCameraInitialized = false;
        System.Diagnostics.Debug.WriteLine("QR Scanner closed");
    }

    // Process scanned QR code
    public async Task ProcessScannedQrCodeAsync(string qrText)
    {
        try
        {
            IsProcessing = true;
            ScannedQrText = qrText;

            // Close QR scanner first
            IsQrScannerVisible = false;
            IsCameraInitialized = false;

            // Create a record with the phone number and QR code
            var qrRecord = new QrCodeRecord(
                centerId: Guid.NewGuid(), // You might want to use a specific center ID for card attachments
                code: $"CARD_ATTACH|{_currentPhoneNumber}|{qrText}"
            );

            // Save to database
            await _databaseService.SaveQrCodeRecordAsync(qrRecord);

            // Refresh the records badge
            await RefreshRecordsBadgeAsync();

            // Clear the phone number input after successful attachment
            PhoneNumber = string.Empty;
            _currentPhoneNumber = string.Empty;

            // Show success notification
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Success",
                    "Card successfully attached!", "OK");
            }

            System.Diagnostics.Debug.WriteLine($"Card attached: Phone={_currentPhoneNumber}, QR={qrText}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing card attachment: {ex.Message}");
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Failed to attach card: {ex.Message}", "OK");
            }
        }
        finally
        {
            IsProcessing = false;
        }
    }

    // Handle phone number changes to validate and enable/disable search button
    partial void OnPhoneNumberChanged(string value)
    {
        ValidatePhoneNumber();
    }

    private async Task ValidatePhoneNumber()
    {
        // Check if phone number has exactly 11 digits
        var digitsOnly = Regex.Replace(PhoneNumber ?? string.Empty, @"\D", "");

        if (digitsOnly.Length == 11)
        {
            var student = await _databaseService.GetStudentByPhoneAsync(digitsOnly);

            if (student != null)
            {
                IsSearchEnabled = digitsOnly.Length == 11;
                StudentName = student.StudentName;
            }
            else
            {
               var  studentFromApi = await _apiService.GetStudentByPhoneAsync(_authenticationService.BearerToken, digitsOnly);
                if (studentFromApi != null)
                { 
                    IsSearchEnabled = true;
                    StudentName = studentFromApi.FullName;
                }
                else
                {
                    IsSearchEnabled = false;
                    StudentName =string.Empty;
                    await Application.Current.MainPage.DisplayAlert("Result", "this number is not exist.", "OK");
                }
            }
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