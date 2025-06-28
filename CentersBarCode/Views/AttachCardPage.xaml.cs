using CentersBarCode.ViewModels;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace CentersBarCode.Views;

public partial class AttachCardPage : ContentPage
{
    private readonly AttachCardViewModel _viewModel;
    private bool _isFlashOn = false;

    public AttachCardPage(AttachCardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        RequestCameraPermissions();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Initialize scanner when QR scanner becomes visible
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsQrScannerVisible) && _viewModel.IsQrScannerVisible)
            {
                CheckCameraPermissionAndInitialize();
            }
        };
    }

    private async void RequestCameraPermissions()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Permission Denied", "Camera access is required.", "OK");
            _viewModel.IsQrScannerVisible = false;
        }
    }

    private void CheckCameraPermissionAndInitialize()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Permission Denied",
                            "Camera permission is required to scan QR codes.", "OK");
                        _viewModel.IsQrScannerVisible = false;
                        return;
                    }
                }

                // Initialize the camera view
                await InitializeCameraAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Camera Error",
                    $"An error occurred initializing the camera: {ex.Message}", "OK");
                _viewModel.IsQrScannerVisible = false;
            }
        });
    }

    private async Task InitializeCameraAsync()
    {
        try
        {
            if (cameraView == null)
            {
                System.Diagnostics.Debug.WriteLine("Camera view is null");
                return;
            }

            // Configure barcode reader options
            cameraView.Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.OneDimensional ,
                AutoRotate = true,
                TryHarder = true,
                Multiple = false,
            };

            cameraView.CameraLocation = CameraLocation.Rear;
            cameraView.IsTorchOn = false;
            _isFlashOn = false;

            // Enable barcode detection
            cameraView.IsDetecting = true;

            // Set initialized flag
            _viewModel.IsCameraInitialized = true;

            System.Diagnostics.Debug.WriteLine("Camera initialized successfully for AttachCard");
        }
        catch (Exception ex)
        {
            _viewModel.IsCameraInitialized = false;
            await DisplayAlert("Camera Error",
                $"Failed to initialize camera: {ex.Message}", "OK");
            _viewModel.IsQrScannerVisible = false;

            System.Diagnostics.Debug.WriteLine($"Camera initialization error: {ex}");
        }
    }

    private void CameraView_BarCodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (!_viewModel.IsQrScannerVisible)
        {
            System.Diagnostics.Debug.WriteLine("Barcode detection skipped: Scanner not visible");
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
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
                    var resultText = firstResult.Value.ToString();
                    System.Diagnostics.Debug.WriteLine($"Detected QR code for card attachment: {resultText}");

                    if (!string.IsNullOrEmpty(resultText))
                    {
                        // Process the QR code for card attachment
                        await _viewModel.ProcessScannedQrCodeAsync(resultText);

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
                            _viewModel.IsCameraInitialized = false;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Empty QR code result");
                        if (cameraView != null)
                        {
                            cameraView.IsDetecting = true;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No QR code results detected");
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
        if (cameraView != null && _viewModel.IsCameraInitialized)
        {
            try
            {
                _isFlashOn = !_isFlashOn;
                System.Diagnostics.Debug.WriteLine($"Setting TorchEnabled to {_isFlashOn}");
                cameraView.IsTorchOn = _isFlashOn;

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
                    await DisplayAlert("Flashlight Error",
                        $"Could not toggle flashlight: {ex.Message}", "OK");
                });
                System.Diagnostics.Debug.WriteLine($"Flashlight error: {ex}");
            }
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Camera Not Ready",
                    "Camera must be initialized before using the flashlight.", "OK");
            });
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (cameraView != null && _viewModel.IsCameraInitialized)
        {
            try
            {
                cameraView.IsDetecting = false;
                _viewModel.IsCameraInitialized = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping camera: {ex.Message}");
            }
        }
    }
}