using CentersBarCode.ViewModels;

namespace CentersBarCode.Views;

public partial class ExamDutiesPage : ContentPage
{
    private readonly ExamDutiesViewModel _viewModel;

    public ExamDutiesPage(ExamDutiesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        // Subscribe to the SearchCommandExecuted event to dismiss keyboard when search button is clicked
        _viewModel.SearchCommandExecuted += () => {
            MainThread.BeginInvokeOnMainThread(DismissKeyboard);
        };
    }
    
    private void DismissKeyboard()
    {
        // Multiple keyboard dismissal attempts for maximum compatibility
        
        // Method 1: Unfocus the entry
        if (SearchEntry != null)
        {
            SearchEntry.IsEnabled = false;
            SearchEntry.IsEnabled = true;
            SearchEntry.Unfocus();
        }
        
        // Method 2: Focus the page itself
        this.Focus();
        
        System.Diagnostics.Debug.WriteLine("Dismissed keyboard using multiple methods");
    }
}
