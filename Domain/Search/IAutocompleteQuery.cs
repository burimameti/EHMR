using EHMR.Services;

namespace EHMR.Domain.Search
{
    public sealed record AppointmentSearchQuery(
    string Query,
    SearchEntityType[] Types,
    int Take = 10);

    public interface IAppointmentSearchQueryHandler
    {
        Task<IReadOnlyList<SearchSuggestionDto>> Handle(
            AppointmentSearchQuery query,
            CancellationToken ct);
    }
}