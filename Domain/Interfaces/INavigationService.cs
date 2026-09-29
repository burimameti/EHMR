using System.Collections.Generic;
using System.Threading.Tasks;

namespace EHMR.Domain.Interfaces
{
    public interface INavigationService
    {
        Task PushModalAsync(object page);

        Task NavigateToAsync<TViewModel>(IDictionary<string, object>? parameters = null);

        Task GoToAsync(string route, IDictionary<string, object>? parameters = null);

        /// <summary>Навигација од менито: секогаш го празни стекот и ја отвора рутата врз dashboard.</summary>
        Task NavigateToRootAsync(string route, IDictionary<string, object>? parameters = null);

        Task GoBackAsync();
    }
}