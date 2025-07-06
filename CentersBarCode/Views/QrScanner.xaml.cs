#if ANDROID
using Android.Views.InputMethods;
using Android.Content;
#endif
using CentersBarCode.ViewModels;
using Microsoft.Maui.Platform;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace CentersBarCode.Views;

public partial class QrScanner : ContentPage
{
    private readonly object _viewModel; // Generic object to hold either ViewModel
    private readonly MainViewModel _mainViewModel;
    private readonly AttachCardViewModel _attachCardViewModel;
    private bool _isFlashOn = false;

    public QrScanner(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _mainViewModel = viewModel;
        BindingContext = _mainViewModel;

        System.Diagnostics.Debug.WriteLine("QrScanner constructed with MainViewModel");

        // Hook into CloseQrScannerCommandExecuted

        RequestCameraPermissions();
    }

    public QrScanner(AttachCardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _attachCardViewModel = viewModel;
        BindingContext = _attachCardViewModel;

        System.Diagnostics.Debug.WriteLine("QrScanner constructed with AttachCardViewModel");

        // Hook into CloseQrScannerCommandExecuted

        RequestCameraPermissions();
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
        await CheckCameraPermissionAndInitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        System.Diagnostics.Debug.WriteLine("QrScanner OnDisappearing called");

        // Unsubscribe from events
        if (_mainViewModel != null)
        {
        }
        if (_attachCardViewModel != null)
        {
        }

        // Stop and release camera
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
            System.Diagnostics.Debug.WriteLine($"CameraView state before init: IsEnabled={cameraView.IsEnabled}, IsDetecting={cameraView.IsDetecting}, IsTorchOn={cameraView.IsTorchOn}");

            // Reset camera state
            cameraView.IsDetecting = false;
            cameraView.IsTorchOn = false;
            _isFlashOn = false;

            // Configure barcode reader options based on ViewModel
            cameraView.Options = new BarcodeReaderOptions
            {
                Formats = _mainViewModel != null ? BarcodeFormats.OneDimensional : BarcodeFormat.Ean13,
                AutoRotate = true,
                TryHarder = false,
                Multiple = false,
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
    }

    private  void CameraView_BarCodeDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        if ((_mainViewModel != null && !_mainViewModel.IsQrScannerVisible) ||
            (_attachCardViewModel != null && !_attachCardViewModel.IsQrScannerVisible))
        {
            System.Diagnostics.Debug.WriteLine("Barcode detection skipped: Scanner not visible");
            return;
        }

        Dispatcher.Dispatch(async() =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Barcode detected: {e.Results?.Length} results");

                // Stop scanning
                if (cameraView != null)
                {
                    cameraView.IsDetecting = false;
                }

                // Process the detected barcode
                if (e.Results != null && e.Results.Length > 0)
                {
                    var firstResult = e.Results[0];
                    var resultText = firstResult.Value?.ToString();
                    System.Diagnostics.Debug.WriteLine($"Detected barcode: {resultText}");

                    if (!string.IsNullOrEmpty(resultText))
                    {
                        if (_mainViewModel != null)
                        {
                            _mainViewModel.ProcessScannedQrCode(resultText);
                            _mainViewModel.IsPopupVisible = true;
                            _mainViewModel.IsQrScannerVisible = false;
                        }
                        if (_attachCardViewModel != null)
                        {
                           await _attachCardViewModel.ProcessScannedQrCodeAsync(resultText, _attachCardViewModel.StudentId.Value);
                            _attachCardViewModel.IsQrScannerVisible = false;
                        }

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
                            if (_mainViewModel != null) _mainViewModel.IsCameraInitialized = false;
                            if (_attachCardViewModel != null) _attachCardViewModel.IsCameraInitialized = false;
                        }

                        // Navigate back to the calling page
                        try
                        {
                            await Navigation.PopAsync();
                            System.Diagnostics.Debug.WriteLine($"Navigated back to {(_mainViewModel != null ? "MainPage" : "AttachCardPage")} after barcode scan");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Navigation error after barcode scan: {ex.Message}");
                            await DisplayAlert("Navigation Error", $"Failed to return to previous page: {ex.Message}", "OK");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Empty barcode result");
                        if (cameraView != null)
                        {
                            cameraView.IsDetecting = true;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No barcode results detected");
                    if (cameraView != null)
                    {
                        cameraView.IsDetecting = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in barcode detection: {ex.Message}");
                if (cameraView != null)
                {
                    cameraView.IsDetecting = true;
                }
            }
        });
    }

    private void ToggleFlash_Clicked(object sender, EventArgs e)
    {
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
    }

    private async void CloseQr(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}