using System.Collections.ObjectModel;

namespace CentersBarCode.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IAuthenticationService _authenticationService;

    [ObservableProperty]
    private ObservableCollection<Group> _groups;

    [ObservableProperty]
    private string _studentName = string.Empty;

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private string _teacherName = string.Empty;

    [ObservableProperty]
    private DateTime? _lastAttendance;

    [ObservableProperty]
    private decimal? _paymentValue;

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

    public MainViewModel(IDatabaseService databaseService, IAuthenticationService authenticationService)
    {
        _databaseService = databaseService;
        _authenticationService = authenticationService;

        Groups = new ObservableCollection<Group>();

        StudentName = string.Empty;
        TeacherName = _authenticationService.TeacherName ?? string.Empty;
        IsQrScannerVisible = false;
        IsPopupVisible = false;
        ScannedQrText = string.Empty;
        ScannedCode = string.Empty;
        ScannedName = string.Empty;
        ScannedGroup = string.Empty;
        IsCameraInitialized = false;
        IsSaving = false;
        IsAutoScanMode = false;
        AutoScanCount = 0;
        ShowAutoScanCounter = false;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await _databaseService.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("Database initialized successfully");

            await LoadGroupsAsync();
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

    [RelayCommand]
    private void OpenQrScanner()
    {
        IsQrScannerVisible = true;
        IsCameraInitialized = true;

        System.Diagnostics.Debug.WriteLine("QR Scanner opened");
    }

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

    [RelayCommand]
    private void OpenAutoQrScanner()
    {
        IsAutoScanMode = true;
        ShowAutoScanCounter = true;
        AutoScanCount = 0;

        IsQrScannerVisible = true;
        IsCameraInitialized = true;

        System.Diagnostics.Debug.WriteLine("Auto QR Scanner opened");
    }

    public async Task<int?> SaveQrCodeDirectly(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            System.Diagnostics.Debug.WriteLine("Cannot save QR code: code is empty");
            return null;
        }

        try
        {
            var student = await _databaseService.GetStudentByCodeAsync(code);

            if (SelectedGroup != null && SelectedGroup.Id != Guid.Empty && student != null)
            {
                if (student.StudentGroupId != SelectedGroup.Id)
                {
                    System.Diagnostics.Debug.WriteLine($"Not Allowed To Add {SelectedGroup.Name}.");

                    await Application.Current.MainPage.DisplayAlert("Save Error", $" مسموح باضافه طلاب مجموعه  {SelectedGroup.Name} فقط .", "OK");

                    return -1;
                }
            }

            var groupId = SelectedGroup != null && SelectedGroup.Id != Guid.Empty
                ? SelectedGroup.Id
                : student?.StudentGroupId ?? Guid.Empty;

            var qrRecord = new QrCodeRecord(
                centerId: groupId,
                code: code
            );

            if (student != null)
            {
                qrRecord.StudentId = student.Id;
                qrRecord.StudentName = student.StudentName;
            }

            var saveQrCodeRecordAsyncResult = await _databaseService.SaveQrCodeRecordAsync(qrRecord);

            if (saveQrCodeRecordAsyncResult != 0)
            {
                AutoScanCount++;
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

    public async Task<Student?> GetStudentInfo(string code)
    {
        try
        {
            var student = await _databaseService.GetStudentByCodeAsync(code);

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

    [RelayCommand]
    private async Task SaveQrCode()
    {
        if (string.IsNullOrEmpty(ScannedCode))
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Please ensure a QR code is scanned.", "OK");
            }
            return;
        }

        try
        {
            IsSaving = true;

            var student = await _databaseService.GetStudentByCodeAsync(ScannedCode);

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

            var groupId = SelectedGroup != null && SelectedGroup.Id != Guid.Empty
                ? SelectedGroup.Id
                : student?.StudentGroupId ?? Guid.Empty;

            var qrRecord = new QrCodeRecord(
                centerId: groupId,
                code: ScannedCode
            );

            if (student != null)
            {
                qrRecord.StudentId = student.Id;
                qrRecord.StudentName = student.StudentName;
            }

            await _databaseService.SaveQrCodeRecordAsync(qrRecord);

            await RefreshRecordsBadgeAsync();

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

    [RelayCommand]
    private void CancelQrCode()
    {
        IsPopupVisible = false;
        ResetScannedData();
    }

    [RelayCommand]
    private void CloseQrScanner()
    {
        IsQrScannerVisible = false;
        IsCameraInitialized = false;
        IsAutoScanMode = false;
        ShowAutoScanCounter = false;
        System.Diagnostics.Debug.WriteLine("QR Scanner closed");
    }

    partial void OnSelectedGroupChanged(Group? value)
    {
        if (value != null && value.Id == Guid.Empty)
        {
            GroupName = string.Empty;
            System.Diagnostics.Debug.WriteLine("'None' option selected - group filter cleared");
        }
        else if (value != null)
        {
            GroupName = value.Name;
            System.Diagnostics.Debug.WriteLine($"Group selected: {value.Name} (ID: {value.Id})");
        }
        else
        {
            GroupName = string.Empty;
        }
    }

    partial void OnIsCameraInitializedChanged(bool value)
    {
        System.Diagnostics.Debug.WriteLine($"Camera initialized: {value}");
    }

    partial void OnIsAutoScanModeChanged(bool value)
    {
        ShowAutoScanCounter = value;
        if (value)
        {
            AutoScanCount = 0;
        }
    }

    public async Task ProcessScannedQrCode(string qrText)
    {
        ScannedQrText = qrText;
        ScannedCode = qrText;

        var student = await _databaseService.GetStudentByCodeAsync(qrText);
        if (student != null)
        {
            ScannedName = student.StudentName;
            ScannedGroup = student.StudentGroupName;
            LastAttendance = student.LastAttendance;
            PaymentValue = student.PaymentValue;
        }
        else
        {
            ScannedName = string.Empty;
            ScannedGroup = string.Empty;
            LastAttendance = null;
            PaymentValue = null;
        }
    }

    private void ResetScannedData()
    {
        ScannedQrText = string.Empty;
        ScannedCode = string.Empty;
        ScannedName = string.Empty;
        ScannedGroup = string.Empty;
        LastAttendance = null;
        PaymentValue = null;
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

    [RelayCommand]
    private async Task RefreshCentersAsync()
    {
        await LoadGroupsAsync();
    }
}
