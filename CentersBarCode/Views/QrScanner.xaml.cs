#if ANDROID
using Android.Views.InputMethods;
using Android.Content;
#endif
using CentersBarCode.ViewModels;
using Microsoft.Maui.Platform;
using System.ComponentModel;
using Microsoft.Maui.Graphics;
using Plugin.Maui.Audio;
using BarcodeScanning;

namespace CentersBarCode.Views;

public partial class QrScanner : ContentPage, INotifyPropertyChanged
{
    private readonly object _viewModel; // Generic object to hold either ViewModel
    private readonly MainViewModel? _mainViewModel;
    private readonly AttachCardViewModel? _attachCardViewModel;
    private bool _isFlashOn = false;
    private bool _isProcessingBarcode = false; // Flag to prevent multiple processing
    
    private int _scanCount = 0;
    
    // Public property for binding
    public int ScanCount
    {
        get => _scanCount;
        set
        {
            if (_scanCount != value)
            {
                _scanCount = value;
                OnPropertyChanged(nameof(ScanCount));
            }
        }
    }

    public QrScanner(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _mainViewModel = viewModel;

        // Set the binding context for the page to handle our local properties
        this.BindingContext = this;
        
        // Set the rest of the controls to use the view model
        qrScannerGrid.BindingContext = _mainViewModel;
        
        // Set auto scan related elements binding context explicitly
        autoScanFrame.BindingContext = _mainViewModel;
        lastScannedLabel.BindingContext = _mainViewModel;

        System.Diagnostics.Debug.WriteLine("QrScanner constructed with MainViewModel");
        
        // Reset the scan counter
        ScanCount = 0;
        
        // Update visibility of auto scan elements based on current mode
        UpdateAutoScanElementsVisibility();

        RequestCameraPermissions();
    }

    public QrScanner(AttachCardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _attachCardViewModel = viewModel;
        
        // Set grid binding context
        qrScannerGrid.BindingContext = _attachCardViewModel;
        
        // Hide auto scan elements when using AttachCardViewModel
        autoScanFrame.IsVisible = false;
        lastScannedLabel.IsVisible = false;

        System.Diagnostics.Debug.WriteLine("QrScanner constructed with AttachCardViewModel");

        RequestCameraPermissions();
    }
    
    private void UpdateAutoScanElementsVisibility()
    {
        if (_mainViewModel != null)
        {
            bool isAutoMode = _mainViewModel.IsAutoScanMode;
            
            // Ensure the elements visibility matches the current mode
            autoScanFrame.IsVisible = isAutoMode;
            lastScannedLabel.IsVisible = isAutoMode;
            
            System.Diagnostics.Debug.WriteLine($"Auto scan elements visibility set to: {isAutoMode}");
        }
    }

    private async void OnCloseQrScannerCommandExecuted()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await Navigation.PopAsync();
                System.Diagnostics.Debug.WriteLine($"Navigated back to {(_mainViewModel != null ? "MainPage" : "AttachCardPage")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                await DisplayAlert("Navigation Error", $"Failed to return to previous page: {ex.Message}", "OK");
            }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("QrScanner OnAppearing called");
        _isProcessingBarcode = false; // Reset processing flag
        
        // Ensure elements visibility is correct
        UpdateAutoScanElementsVisibility();
        
        await CheckCameraPermissionAndInitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        System.Diagnostics.Debug.WriteLine("QrScanner OnDisappearing called");

        // Update the view model's counter with our local counter
        if (_mainViewModel != null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Updating AutoScanCount to {ScanCount}");
                // Transfer our count to the view model if needed
                _mainViewModel.AutoScanCount = ScanCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating AutoScanCount: {ex.Message}");
            }
        }

        // Clean up code but avoid making API calls that might not be supported
        if (_mainViewModel != null) _mainViewModel.IsCameraInitialized = false;
        if (_attachCardViewModel != null) _attachCardViewModel.IsCameraInitialized = false;
    }

    private async void RequestCameraPermissions()
    {
        System.Diagnostics.Debug.WriteLine("Requesting camera permissions");
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            System.Diagnostics.Debug.WriteLine("Camera permission denied");
            await DisplayAlert("Permission Denied", "Camera permission is required to scan QR codes.", "OK");
            if (_mainViewModel != null) _mainViewModel.IsQrScannerVisible = false;
            if (_attachCardViewModel != null) _attachCardViewModel.IsQrScannerVisible = false;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Camera permission granted");
        }
    }

    private async Task CheckCameraPermissionAndInitializeAsync()
    {
        System.Diagnostics.Debug.WriteLine("Checking camera permissions and initializing");
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("Camera permission denied during initialization");
                    await DisplayAlert("Permission Denied", "Camera permission is required to scan QR codes.", "OK");
                    if (_mainViewModel != null) _mainViewModel.IsQrScannerVisible = false;
                    if (_attachCardViewModel != null) _attachCardViewModel.IsQrScannerVisible = false;
                    return;
                }
            }

            await InitializeCameraAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking permissions or initializing camera: {ex.Message}");
            await DisplayAlert("Camera Error", $"An error occurred initializing the camera: {ex.Message}", "OK");
            if (_mainViewModel != null) _mainViewModel.IsQrScannerVisible = false;
            if (_attachCardViewModel != null) _attachCardViewModel.IsQrScannerVisible = false;
        }
    }

    private async Task InitializeCameraAsync()
    {
        if (cameraView == null)
        {
            System.Diagnostics.Debug.WriteLine("Camera view is null");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"CameraView initializing");

            // Configure camera
            cameraView.CameraFacing = CameraFacing.Back;
            _isFlashOn = false;
            
            // The camera view will be initialized by the XAML declaration
            // and event handlers will be triggered when barcodes are detected
            
            // Set view model camera state
            if (_mainViewModel != null) _mainViewModel.IsCameraInitialized = true;
            if (_attachCardViewModel != null) _attachCardViewModel.IsCameraInitialized = true;

            System.Diagnostics.Debug.WriteLine("Camera initialized successfully for QrScanner");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Camera initialization error: {ex.Message}");
            if (_mainViewModel != null) _mainViewModel.IsCameraInitialized = false;
            if (_attachCardViewModel != null) _attachCardViewModel.IsCameraInitialized = false;
            await DisplayAlert("Camera Error", $"Failed to initialize camera: {ex.Message}", "OK");
            if (_mainViewModel != null) _mainViewModel.IsQrScannerVisible = false;
            if (_attachCardViewModel != null) _attachCardViewModel.IsQrScannerVisible = false;
        }
    }

    private void CameraView_BarCodeDetected(object sender, OnDetectionFinishedEventArg e)
    {
        // Prevent multiple processing of the same barcode
        if (_isProcessingBarcode)
        {
            System.Diagnostics.Debug.WriteLine("Barcode processing already in progress, skipping");
            return;
        }

        if ((_mainViewModel != null && !_mainViewModel.IsQrScannerVisible) ||
            (_attachCardViewModel != null && !_attachCardViewModel.IsQrScannerVisible))
        {
            System.Diagnostics.Debug.WriteLine("Barcode detection skipped: Scanner not visible");
            return;
        }

        // Set processing flag
        _isProcessingBarcode = true;

        Dispatcher.Dispatch(async () =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Barcode detected: {e.BarcodeResults?.Count ?? 0} results");

                // Process the detected barcode
                var currentBarcode = e.BarcodeResults?.Where(x => x.BarcodeFormat == BarcodeFormats.Code128).FirstOrDefault();
                if (currentBarcode!= null)
                {
                    var firstResult = currentBarcode;
                    var resultText = firstResult.DisplayValue;

                    if (!string.IsNullOrEmpty(resultText))
                    {
                        resultText = resultText?.Replace("-", "");

                        if (_mainViewModel != null)
                        {
                            // Check if we're in Auto Scan mode
                            bool isAutoScanMode = _mainViewModel.IsAutoScanMode;
                            
                            if (isAutoScanMode)
                            {
                                System.Diagnostics.Debug.WriteLine("Auto Scan Mode: Processing barcode directly");
                                
                                // Store the scanned code for display in the UI
                                _mainViewModel.ScannedCode = resultText;
                                
                                // Play a short vibration for feedback
                                try
                                {
                                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                                    await this.PlayBeepSoundAsync();
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
                                }
                                
                                // Save the code directly to database
                                bool saveResult = await _mainViewModel.SaveQrCodeDirectly(resultText ?? string.Empty);
                                
                                if (saveResult)
                                {
                                    // Increment our local counter
                                    ScanCount++;
                                    System.Diagnostics.Debug.WriteLine($"Scan count incremented to {ScanCount}");
                                    
                                    // Show a visual feedback briefly
                                    await ShowScanSuccessIndicatorAsync();
                                    
                                    // Allow processing next barcode after a short delay
                                    await Task.Delay(800); 
                                    _isProcessingBarcode = false;
                                }
                                else
                                {
                                    // Handle save failure
                                    System.Diagnostics.Debug.WriteLine("Failed to auto-save barcode");
                                    await DisplayAlert("Save Error", "Failed to save barcode", "OK");
                                    _isProcessingBarcode = false;
                                }
                            }
                            else
                            {
                                // Regular flow - show popup
                                await _mainViewModel.ProcessScannedQrCode(resultText ?? string.Empty);
                                _mainViewModel.IsPopupVisible = true;
                                _mainViewModel.IsQrScannerVisible = false;
                                
                                try
                                {
                                    Vibration.Default.Vibrate();
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
                                }

                                if (_mainViewModel != null)
                                {
                                    _mainViewModel.IsCameraInitialized = false;
                                }

                                // Navigate back to the calling page
                                try
                                {
                                    await Navigation.PopAsync();
                                    System.Diagnostics.Debug.WriteLine("Navigated back to MainPage after barcode scan");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Navigation error after barcode scan: {ex.Message}");
                                    await DisplayAlert("Navigation Error", $"Failed to return to previous page: {ex.Message}", "OK");
                                }
                            }
                        }
                        else if (_attachCardViewModel != null)
                        {
                            await _attachCardViewModel.ProcessScannedQrCodeAsync(resultText ?? string.Empty, _attachCardViewModel.StudentId ?? Guid.Empty);
                            _attachCardViewModel.IsQrScannerVisible = false;
                            
                            try
                            {
                                Vibration.Default.Vibrate();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
                            }

                            if (_attachCardViewModel != null)
                            {
                                _attachCardViewModel.IsCameraInitialized = false;
                            }

                            // Navigate back to the calling page
                            try
                            {
                                await Navigation.PopAsync();
                                System.Diagnostics.Debug.WriteLine("Navigated back to AttachCardPage after barcode scan");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Navigation error after barcode scan: {ex.Message}");
                                await DisplayAlert("Navigation Error", $"Failed to return to previous page: {ex.Message}", "OK");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Empty barcode result");
                        _isProcessingBarcode = false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No barcode results detected");
                    _isProcessingBarcode = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in barcode detection: {ex.Message}");
                _isProcessingBarcode = false;
            }
        });
    }

    // Visual feedback for successful scan
    private async Task ShowScanSuccessIndicatorAsync()
    {
        try
        {
            // Flash the counter or provide some visual feedback
            // This could be done with animations, color changes, etc.
            System.Diagnostics.Debug.WriteLine($"Scan successful - Counter: {ScanCount}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing scan success indicator: {ex.Message}");
        }
    }

    private async Task PlayBeepSoundAsync()
    {
        try
        {
            // Use Microsoft.Maui.Media namespace for audio playback
            var audioPlayer = AudioManager.Current.CreatePlayer(
                await FileSystem.OpenAppPackageFileAsync("beep.mp3"));

            // Play the sound
            audioPlayer.Play();

            await Task.Delay(1000);

            System.Diagnostics.Debug.WriteLine("Beep sound played successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing beep sound: {ex.Message}");
            // Continue execution even if sound fails - the vibration will still work
        }
    }

    private void ToggleFlash_Clicked(object sender, EventArgs e)
    {
        // Toggling flash is currently not implemented due to API compatibility issues
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert("Flashlight Error", $"Flashlight functionality is not available in this version.", "OK");
        });
    }

    private async void CloseQr(object sender, EventArgs e)
    {
        _isProcessingBarcode = false; // Reset processing flag
        await Navigation.PopAsync();
    }
}