namespace CentersBarCode.Views;

public partial class ManualAddPage : ContentPage
{
    private readonly ManualAddViewModel _viewModel;

    public ManualAddPage(ManualAddViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Refresh centers data when the page appears
        await _viewModel.RefreshCentersCommand.ExecuteAsync(null);
    }
}