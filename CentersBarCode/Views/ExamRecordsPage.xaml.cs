using CentersBarCode.ViewModels;

namespace CentersBarCode.Views;

public partial class ExamRecordsPage : ContentPage
{
    private readonly ExamRecordsViewModel _viewModel;

    public ExamRecordsPage(ExamRecordsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load records when the page appears
        await _viewModel.LoadRecordsCommand.ExecuteAsync(null);
    }
}
