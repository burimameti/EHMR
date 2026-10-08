using CommunityToolkit.Maui.Views;
using EHMR.ViewModels;

namespace EHMR.Views.Popups;

public partial class AddPatientScorePopup : Popup
{
    private readonly ScoreEditorViewModel _viewModel;
    private bool _isClosing;
    private bool _isSaving;

    public AddPatientScorePopup(ScoreEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public ScoreRow? ScoreResult { get; private set; }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_isSaving || _isClosing || string.IsNullOrWhiteSpace(_viewModel.DescriptionText))
            return;

        _isSaving = true;
        try
        {
            _viewModel.AddCommand.Execute(null);
            ScoreResult = _viewModel.Items.FirstOrDefault();
            await ClosePopupAsync();
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
        => await ClosePopupAsync();

    private async Task ClosePopupAsync()
    {
        if (_isClosing)
            return;

        _isClosing = true;
#if WINDOWS
        await CloseAsync(CancellationToken.None);
#else
        await CloseAsync();
#endif
    }
}