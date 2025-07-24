#if ANDROID
using Android.Views.InputMethods;
using Android.Content;
using CentersBarCode.Platforms.Android;

#endif
using CentersBarCode.ViewModels;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;
using Plugin.Maui.Audio;
using System.ComponentModel;

//using ZXing.Net.Maui;
//using ZXing.Net.Maui.Controls;

namespace CentersBarCode.Views;

public partial class QrScanner : ContentPage, INotifyPropertyChanged
{
    private readonly object _viewModel; // Generic object to hold either ViewModel
    private readonly MainViewModel _mainViewModel;
    private readonly AttachCardViewModel _attachCardViewModel;
    private bool _isFlashOn = false;
    private bool _isProcessingBarcode = false; // Flag to prevent multiple processing

    private int _scanCount = 0;
#if ANDROID
    private readonly MlKitBarcodeScanner _mlkitScanner = new MlKitBarcodeScanner();
#else
    private readonly object _mlkitScanner = null; // Null for non-Android platforms
#endif
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
                _mainViewModel.AutoScanCount = ScanCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating AutoScanCount: {ex.Message}");
            }
        }

#if ANDROID
        // Dispose ML Kit scanner
        try
        {
            _mlkitScanner?.Dispose();
            if (_mainViewModel != null) _mainViewModel.IsCameraInitialized = false;
            if (_attachCardViewModel != null) _attachCardViewModel.IsCameraInitialized = false;
            System.Diagnostics.Debug.WriteLine("ML Kit scanner disposed in OnDisappearing");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disposing ML Kit scanner: {ex.Message}");
        }
#endif
        // Stop and release camera
        /*
        if (cameraView != null)
        {
            try
            {
                cameraView.IsDetecting = false;
                cameraView.IsTorchOn = false;
                cameraView.BarcodesDetected -= CameraView_BarCodeDetected;
                if (_mainViewModel != null) _mainViewModel.IsCameraInitialized = false;
                if (_attachCardViewModel != null) _attachCardViewModel.IsCameraInitialized = false;
                System.Diagnostics.Debug.WriteLine("Camera stopped and released in OnDisappearing");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping camera in OnDisappearing: {ex.Message}");
            }
        }
        */
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
        /*
        if (cameraView == null)
        {
            System.Diagnostics.Debug.WriteLine("Camera view is null");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"CameraView state before init: IsEnabled={cameraView.IsEnabled}, IsDetecting={cameraView.IsDetecting}, IsTorchOn={cameraView.IsTorchOn}");

            // Reset camera state
            cameraView.IsDetecting = false;
            cameraView.IsTorchOn = false;
            _isFlashOn = false;

            // Configure barcode reader options based on ViewModel
            cameraView.Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormat.Ean8 | BarcodeFormat.Code128,
                AutoRotate = true,
                TryHarder = false,
                Multiple = false
            };

            cameraView.CameraLocation = CameraLocation.Rear;
          
            // Force UI refresh
            cameraView.IsEnabled = false;
            await Task.Delay(300);
            cameraView.IsEnabled = true;

            // Ensure event handler is subscribed only once
            cameraView.BarcodesDetected -= CameraView_BarCodeDetected;
            cameraView.BarcodesDetected += CameraView_BarCodeDetected;

            // Enable barcode detection
            cameraView.IsDetecting = true;
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
        */

        // Initialize ML Kit Barcode Scanner
        try
        {
            System.Diagnostics.Debug.WriteLine("Initializing ML Kit Barcode Scanner");
            if (_mainViewModel != null) _mainViewModel.IsCameraInitialized = true;
            if (_attachCardViewModel != null) _attachCardViewModel.IsCameraInitialized = true;

            // Start scanning
            await ScanWithMlKitAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ML Kit initialization error: {ex.Message}");
            if (_mainViewModel != null) _mainViewModel.IsCameraInitialized = false;
            if (_attachCardViewModel != null) _attachCardViewModel.IsCameraInitialized = false;
            await DisplayAlert("Scanner Error", $"Failed to initialize ML Kit scanner: {ex.Message}", "OK");
            if (_mainViewModel != null) _mainViewModel.IsQrScannerVisible = false;
            if (_attachCardViewModel != null) _attachCardViewModel.IsQrScannerVisible = false;
        }
    }

    /*
    private void CameraView_BarCodeDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
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
                System.Diagnostics.Debug.WriteLine($"Barcode detected: {e.Results?.Length} results");

                // Stop scanning temporarily
                if (cameraView != null)
                {
                    cameraView.IsDetecting = false;
                }

                // Process the detected barcode
                if (e.Results != null && e.Results.Length > 0)
                {
                    var firstResult = e.Results[0];
                    var resultText = firstResult.Value?.ToString();

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
                                bool saveResult = await _mainViewModel.SaveQrCodeDirectly(resultText);
                                
                                if (saveResult)
                                {
                                    // Increment our local counter
                                    ScanCount++;
                                    System.Diagnostics.Debug.WriteLine($"Scan count incremented to {ScanCount}");
                                    
                                    // Show a visual feedback briefly
                                    await ShowScanSuccessIndicatorAsync();
                                    
                                    // Reset the camera for next scan
                                    if (cameraView != null)
                                    {
                                        await Task.Delay(800); // Give user time to see the counter update
                                        cameraView.IsDetecting = true;
                                    }
                                }
                                else
                                {
                                    // Handle save failure
                                    System.Diagnostics.Debug.WriteLine("Failed to auto-save barcode");
                                    await DisplayAlert("Save Error", "Failed to save barcode", "OK");
                                    
                                    if (cameraView != null)
                                    {
                                        cameraView.IsDetecting = true;
                                    }
                                }
                                
                                // Reset processing flag
                                _isProcessingBarcode = false;
                            }
                            else
                            {
                                // Regular flow - show popup
                                //search about code in student table and add value to ScannedName to show in popup
                                await _mainViewModel.ProcessScannedQrCode(resultText);
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

                                if (cameraView != null)
                                {
                                    cameraView.IsDetecting = false;
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
                            await _attachCardViewModel.ProcessScannedQrCodeAsync(resultText, _attachCardViewModel.StudentId.Value);
                            _attachCardViewModel.IsQrScannerVisible = false;
                            
                            try
                            {
                                Vibration.Default.Vibrate();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
                            }

                            if (cameraView != null)
                            {
                                cameraView.IsDetecting = false;
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
                        if (cameraView != null)
                        {
                            cameraView.IsDetecting = true;
                        }
                        _isProcessingBarcode = false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No barcode results detected");
                    if (cameraView != null)
                    {
                        cameraView.IsDetecting = true;
                    }
                    _isProcessingBarcode = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in barcode detection: {ex.Message}");
                if (cameraView != null)
                {
                    cameraView.IsDetecting = true;
                }
                _isProcessingBarcode = false;
            }
        });
    }
    */

    private async Task ScanWithMlKitAsync()
    {
#if ANDROID
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

        _isProcessingBarcode = true;

        try
        {
            var barcode = await _mlkitScanner.ScanAsync();
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (barcode == null || string.IsNullOrEmpty(barcode.RawValue))
                {
                    System.Diagnostics.Debug.WriteLine("No barcode detected");
                    await DisplayAlert("Scan Error", "No barcode detected", "OK");
                    _isProcessingBarcode = false;
                    // Continue scanning in auto mode
                    if (_mainViewModel?.IsAutoScanMode == true)
                    {
                        await ScanWithMlKitAsync();
                    }
                    return;
                }

                var resultText = barcode.RawValue.Replace("-", "");
                System.Diagnostics.Debug.WriteLine($"ML Kit Barcode detected: {resultText}");

                if (_mainViewModel != null)
                {
                    bool isAutoScanMode = _mainViewModel.IsAutoScanMode;

                    if (isAutoScanMode)
                    {
                        System.Diagnostics.Debug.WriteLine("Auto Scan Mode: Processing barcode directly");

                        _mainViewModel.ScannedCode = resultText;

                        try
                        {
                            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                            await PlayBeepSoundAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
                        }

                        bool saveResult = await _mainViewModel.SaveQrCodeDirectly(resultText);

                        if (saveResult)
                        {
                            ScanCount++;
                            System.Diagnostics.Debug.WriteLine($"Scan count incremented to {ScanCount}");
                            await ShowScanSuccessIndicatorAsync();
                            await Task.Delay(800); // Brief pause for user feedback
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Failed to auto-save barcode");
                            await DisplayAlert("Save Error", "Failed to save barcode", "OK");
                        }

                        _isProcessingBarcode = false;
                        // Continue scanning in auto mode
                        await ScanWithMlKitAsync();
                    }
                    else
                    {
                        await _mainViewModel.ProcessScannedQrCode(resultText);
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
                    await _attachCardViewModel.ProcessScannedQrCodeAsync(resultText, _attachCardViewModel.StudentId.Value);
                    _attachCardViewModel.IsQrScannerVisible = false;

                    try
                    {
                        Vibration.Default.Vibrate();
                    }
                    catch (Exception ex)
                        {
                        System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
                    }

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
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ML Kit barcode detection: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Scan Error", $"Error scanning barcode: {ex.Message}", "OK");
            });
            _isProcessingBarcode = false;
            // Continue scanning in auto mode
            if (_mainViewModel?.IsAutoScanMode == true)
            {
                await ScanWithMlKitAsync();
            }
        }
#else
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlert("Platform Not Supported", "QR scanning is only supported on Android with ML Kit.", "OK");
            if (_mainViewModel != null) _mainViewModel.IsQrScannerVisible = false;
            if (_attachCardViewModel != null) _attachCardViewModel.IsQrScannerVisible = false;
            await Navigation.PopAsync();
        });
#endif
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
        /*
        if (cameraView != null &&
            ((_mainViewModel != null && _mainViewModel.IsCameraInitialized) ||
             (_attachCardViewModel != null && _attachCardViewModel.IsCameraInitialized)))
        {
            try
            {
                _isFlashOn = !_isFlashOn;
                cameraView.IsTorchOn = _isFlashOn;
                System.Diagnostics.Debug.WriteLine($"Setting torch to {_isFlashOn}");

                if (sender is Button flashButton)
                {
                    flashButton.BackgroundColor = _isFlashOn ? Colors.Yellow : Colors.Transparent;
                    flashButton.Text = _isFlashOn ? "💡" : "🔦";
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Flashlight Error", $"Could not toggle flashlight: {ex.Message}", "OK");
                });
                System.Diagnostics.Debug.WriteLine($"Flashlight error: {ex}");
            }
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Camera Not Ready", "Camera must be initialized before using the flashlight.", "OK");
            });
        }
        */
        // ML Kit flash control would need to be implemented differently
        // You might need to use platform-specific code or check if MlKitBarcodeScanner supports torch control
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert("Flashlight Not Supported", "Flashlight functionality is not currently supported with ML Kit scanner.", "OK");
        });
    }

    private async void CloseQr(object sender, EventArgs e)
    {
        _isProcessingBarcode = false; // Reset processing flag
        await Navigation.PopAsync();
    }
}