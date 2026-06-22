using EHMR.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Search
{
    public sealed class SearchSuggestionDto
    {
        public string Id { get; init; } = "";
        public string DisplayText { get; init; } = "";

        public SearchEntityType Type
        {
            get; init;
        }

        public double Score
        {
            get; init;
        }

        public string Group => Type.ToString();
    }
}