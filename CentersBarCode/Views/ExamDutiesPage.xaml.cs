using CentersBarCode.ViewModels;
using BarcodeScanning;

namespace CentersBarCode.Views;

public partial class ExamDutiesPage : ContentPage
{
    private readonly ExamDutiesViewModel _viewModel;
    private bool _isProcessingBarcode = false;

    public ExamDutiesPage(ExamDutiesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        // Subscribe to the SearchCommandExecuted event to dismiss keyboard
        _viewModel.SearchCommandExecuted += () => {
            MainThread.BeginInvokeOnMainThread(DismissKeyboard);
        };

        // Subscribe to the OpenCameraRequested event
        _viewModel.OpenCameraRequested += async () => {
            await OpenCameraAndScanAsync();
        };
    }

    private async Task OpenCameraAndScanAsync()
    {
        try
        {
            var scannedCode = await ScanBarcodeAsync();
            
            if (!string.IsNullOrWhiteSpace(scannedCode))
            {
                // Handle the scanned barcode
                await _viewModel.HandleBarcodeScannedAsync(scannedCode);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in camera scan: {ex.Message}");
            await DisplayAlert("Error", $"Failed to scan barcode: {ex.Message}", "OK");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Check if we have a result from barcode scanning in navigation parameters
        var navigationParameter = await GetNavigationParameterAsync();
        if (!string.IsNullOrWhiteSpace(navigationParameter))
        {
            await _viewModel.HandleBarcodeScannedAsync(navigationParameter);
        }
    }

    private async Task<string?> GetNavigationParameterAsync()
    {
        // Check if there's a navigation parameter
        // This is a placeholder - the actual implementation depends on how you pass the parameter
        return null;
    }
    
    private void DismissKeyboard()
    {
        // Unfocus the search entry to dismiss keyboard
        if (SearchEntry != null)
        {
            SearchEntry.IsEnabled = false;
            SearchEntry.IsEnabled = true;
            SearchEntry.Unfocus();
        }
        
        // Don't call this.Focus() as it can trigger the Picker to open
        // The keyboard will be dismissed by unfocusing the entry
        
        System.Diagnostics.Debug.WriteLine("Dismissed keyboard");
    }

    // Create a simple inline barcode scanner
    private async Task<string?> ScanBarcodeAsync()
    {
        var tcs = new TaskCompletionSource<string?>();
        _isProcessingBarcode = false;
        
        // Create a simple scanner popup
        var scannerPage = new ContentPage
        {
            BackgroundColor = Colors.Black
        };

        var cameraView = new CameraView
        {
            CameraEnabled = true,
            CameraFacing = CameraFacing.Back,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        var closeButton = new Button
        {
            Text = "Close Scanner",
            BackgroundColor = Colors.Red,
            TextColor = Colors.White,
            CornerRadius = 10,
            WidthRequest = 200,
            HeightRequest = 50,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 20)
        };

        closeButton.Clicked += async (s, e) =>
        {
            tcs.TrySetResult(null);
            await Navigation.PopModalAsync();
        };

        var label = new Label
        {
            Text = "Position Barcode Inside Frame",
            TextColor = Colors.White,
            FontSize = 16,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 40, 0, 0)
        };

        var frame = new Frame
        {
            Padding = 2,
            BackgroundColor = Colors.Transparent,
            BorderColor = Colors.White,
            CornerRadius = 10,
            HasShadow = false,
            HeightRequest = 200,
            WidthRequest = 300,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Star }
            }
        };

        var overlayGrid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            Padding = new Thickness(20)
        };

        overlayGrid.Add(label, 0, 0);
        overlayGrid.Add(frame, 0, 1);
        overlayGrid.Add(closeButton, 0, 2);

        grid.Add(cameraView, 0, 0);
        grid.Add(overlayGrid, 0, 0);

        scannerPage.Content = grid;

        // Handle barcode detection
        cameraView.OnDetectionFinished += (s, e) =>
        {
            if (_isProcessingBarcode)
            {
                return;
            }

            _isProcessingBarcode = true;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var barcode = e.BarcodeResults?
                        .Where(x => x.BarcodeFormat == BarcodeFormats.Code128 ||
                                   x.BarcodeFormat == BarcodeFormats.Ean8 ||
                                   x.BarcodeFormat == BarcodeFormats.QRCode)
                        .FirstOrDefault();

                    if (barcode != null)
                    {
                        var resultText = barcode.DisplayValue?.Replace("-", "");

                        if (!string.IsNullOrEmpty(resultText) && 
                            int.TryParse(resultText, out int resultInt) &&
                            resultInt > 0 && resultInt < 12000000)
                        {
                            try
                            {
                                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
                            }

                            tcs.TrySetResult(resultText);
                            await Navigation.PopModalAsync();
                            return;
                        }
                    }

                    _isProcessingBarcode = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in barcode detection: {ex.Message}");
                    _isProcessingBarcode = false;
                }
            });
        };

        await Navigation.PushModalAsync(scannerPage);
        
        return await tcs.Task;
    }
}
