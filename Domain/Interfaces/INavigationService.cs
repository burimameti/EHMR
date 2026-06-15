namespace EHMR.Domain.Interfaces
{
    public interface INavigationService
    {
        Task PushModalAsync(object page);

        Task NavigateToAsync<TViewModel>(IDictionary<string, object>? parameters = null);

        Task GoToAsync(string route, IDictionary<string, object>? parameters = null);

        Task GoBackAsync();
    }
}