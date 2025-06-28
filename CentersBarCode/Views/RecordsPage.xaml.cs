using Microsoft.Maui.Controls;
using CentersBarCode.ViewModels;

namespace CentersBarCode.Views;

public partial class RecordsPage : ContentPage
{
    private readonly RecordsViewModel _viewModel;

    public RecordsPage(RecordsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadRecordsCommand.ExecuteAsync(null);
    }
}
