using CentersBarCode.ViewModels;
using Microsoft.Maui.Controls;

namespace CentersBarCode.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;

        // Hook into OpenQrScannerCommand to navigate to QrScanner
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                var qrScannerPage = new QrScanner(_viewModel);
                await Navigation.PushAsync(qrScannerPage);
                System.Diagnostics.Debug.WriteLine("Navigated to QrScanner page");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                await DisplayAlert("Navigation Error", $"Failed to open QR scanner: {ex.Message}", "OK");
            }
        });
    }
}