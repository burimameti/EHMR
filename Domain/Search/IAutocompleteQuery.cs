using EHMR.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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