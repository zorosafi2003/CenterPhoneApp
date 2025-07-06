using System.Text.RegularExpressions;

namespace CentersBarCode.ViewModels;

public partial class AttachCardViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IApiService _apiService;


    public event Action SearchCommandExecuted;
    public event Action CloseQrScannerCommandExecuted;

    private string _currentPhoneNumber = string.Empty;

    [ObservableProperty]
    private string _phoneNumber;

    [ObservableProperty]
    private string _studentName = string.Empty;

    [ObservableProperty]
    private Guid? _studentId = null;

    [ObservableProperty]
    private string _teacherName = string.Empty;

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

    public AttachCardViewModel(IDatabaseService databaseService, IAuthenticationService authenticationService, IApiService apiService)
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

        StudentName = string.Empty;
        StudentId = null;
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
            _studentId = StudentId;

            IsQrScannerVisible = true;
            IsCameraInitialized = true;
            System.Diagnostics.Debug.WriteLine($"Opening QR scanner for phone: {_currentPhoneNumber}");
            SearchCommandExecuted?.Invoke();

        }
    }

    // Command to close QR scanner
    [RelayCommand]
    private async void CloseQrScanner()
    {
        IsQrScannerVisible = false;
        IsCameraInitialized = false;
        System.Diagnostics.Debug.WriteLine("QR Scanner closed");
    }

    // Process scanned QR code
    public async Task ProcessScannedQrCodeAsync(string qrText, Guid studentId)
    {
        try
        {
            IsProcessing = true;
            ScannedQrText = qrText;

            // Close QR scanner first
            IsQrScannerVisible = false;
            IsCameraInitialized = false;

            var attachStudentWithCodeResult = await _apiService.AttachStudentWithCodeAsync(_authenticationService.BearerToken, studentId, qrText);

            if (attachStudentWithCodeResult != null)
            {
                // Clear the phone number input after successful attachment
                PhoneNumber = string.Empty;
                _currentPhoneNumber = string.Empty;

                // Show success notification
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Success",
                        $"Card {attachStudentWithCodeResult.Code} successfully attached with {attachStudentWithCodeResult.FullName}", "OK");
                }
            }
            else
            {
                throw new Exception("Failed to attach student with QR code.");
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

    private async void ValidatePhoneNumber()
    {
        // Check if phone number has exactly 11 digits
        var digitsOnly = Regex.Replace(PhoneNumber ?? string.Empty, @"\D", "");
        if (digitsOnly.Length == 11)
        {
            var student = await _databaseService.GetStudentByPhoneAsync(digitsOnly);

            if (student != null)
            {
                IsSearchEnabled = true;
                StudentName = student.StudentName;
                StudentId = student.StudentId;
            }
            else
            {
                var studentFromApi = await _apiService.GetStudentByPhoneAsync(_authenticationService.BearerToken, digitsOnly);
                if (studentFromApi != null)
                {
                    IsSearchEnabled = true;
                    StudentName = studentFromApi.FullName;
                    StudentId = studentFromApi.Id;
                }
                else
                {
                    IsSearchEnabled = false;
                    StudentName = string.Empty;
                    StudentId = null;
                    await Application.Current.MainPage.DisplayAlert("Result", "this number is not exist.", "OK");
                }
            }
        }
        else
        {
            IsSearchEnabled = false;
            StudentName = string.Empty;
            StudentId = null;
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