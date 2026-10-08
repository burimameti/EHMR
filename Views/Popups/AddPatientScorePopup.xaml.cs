using CommunityToolkit.Maui.Views;
using EHMR.ViewModels;
using System.ComponentModel;

namespace EHMR.Views.Popups;

public partial class AddPatientScorePopup : Popup
{
    private readonly ScoreEditorViewModel _viewModel;
    private bool _isClosing;

    public AddPatientScorePopup(ScoreEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    public ScoreRow? ScoreResult { get; private set; }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ScoreEditorViewModel.SelectedScore) ||
            _viewModel.SelectedScore is null)
            return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            ScoreResult = _viewModel.SelectedScore;
            await ClosePopupAsync();
        });
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
        => await ClosePopupAsync();

    private async Task ClosePopupAsync()
    {
        if (_isClosing)
            return;

        _isClosing = true;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
#if WINDOWS
        await CloseAsync(CancellationToken.None);
#else
        await CloseAsync();
#endif
    }
}
