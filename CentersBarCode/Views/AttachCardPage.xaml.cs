#if ANDROID
using Android.Views.InputMethods;
using Android.Content;
#endif
using CentersBarCode.ViewModels;
using Microsoft.Maui.Platform;

namespace CentersBarCode.Views;

public partial class AttachCardPage : ContentPage
{
    private readonly AttachCardViewModel _viewModel;

    public AttachCardPage(AttachCardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        System.Diagnostics.Debug.WriteLine("AttachCardPage constructed with new view model instance");

        // Hook into SearchCommand execution to dismiss keyboard
        _viewModel.SearchCommandExecuted += () =>
        {
            MainThread.BeginInvokeOnMainThread(DismissKeyboard);
        };
    }

    private void DismissKeyboard()
    {
        if (PhoneEntry != null)
        {
            PhoneEntry.Unfocus();
        }

#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var inputMethodManager = (InputMethodManager)context.GetSystemService(Context.InputMethodService);
            if (inputMethodManager != null && Shell.Current?.Handler?.MauiContext?.Context != null)
            {
                var windowToken = Shell.Current.Handler.MauiContext.Context?.GetActivity()?.CurrentFocus?.WindowToken;
                if (windowToken != null)
                {
                    inputMethodManager.HideSoftInputFromWindow(windowToken, HideSoftInputFlags.None);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error dismissing keyboard: {ex.Message}");
        }
#endif
    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("OnSearchClicked called");
        if (_viewModel.SearchCommand?.CanExecute(null) == true)
        {
            _viewModel.SearchCommand.Execute(null);
        }
        DismissKeyboard();

        // Navigate to QrScanner page
        try
        {
            var qrScannerPage = new QrScanner(_viewModel); // Pass the same ViewModel if shared data is needed
            await Navigation.PushAsync(qrScannerPage);
            System.Diagnostics.Debug.WriteLine("Navigated to QrScanner page");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            await DisplayAlert("Navigation Error", $"Failed to open QR scanner: {ex.Message}", "OK");
        }
    }

    private void PhoneEntry_Completed(object sender, EventArgs e)
    {
        if (_viewModel.SearchCommand?.CanExecute(null) == true)
        {
            _viewModel.SearchCommand.Execute(null);
        }
        DismissKeyboard();
    }
}