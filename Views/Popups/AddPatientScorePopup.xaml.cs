using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using EHMR.ViewModels;

namespace EHMR.Views.Popups;

public partial class AddPatientScorePopup : Popup
{
    private readonly ScoreEditorViewModel _viewModel;

    public AddPatientScorePopup(ScoreEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel=viewModel;
        BindingContext=_viewModel;

        SaveCommand=new Command(async () => await SaveAsync());
        CancelCommand=new Command(async () => await CloseAsync());
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private async Task SaveAsync()
    {
        if(string.IsNullOrWhiteSpace(_viewModel.DescriptionText))
            return;

        _viewModel.AddCommand.Execute(null);
        var row=_viewModel.Items.FirstOrDefault();
        if(row is not null)
            await CloseAsync(row);
    }
}