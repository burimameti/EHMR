using EHMR.Domain.Interfaces;

namespace EHMR.Domain.Entities.Reports
{
    public class ReportRegistry
    {

        private readonly List<IReportProvider> _providers;



        public IReadOnlyList<IReportProvider> Providers
            => _providers;



        public ReportRegistry(
       IEnumerable<IReportProvider> providers)
        {
            _providers=providers.ToList();


            var duplicates =
                _providers
                .GroupBy(x => x.Key)
                .Where(x => x.Count()>1)
                .Select(x => x.Key)
                .ToList();


            if(duplicates.Any())
            {
                throw new InvalidOperationException(
                    $"Duplicate report keys: {string.Join(",", duplicates)}");
            }
        }



        public IReportProvider Resolve(string key)
        {
            var provider =
                _providers.FirstOrDefault(x =>
                    string.Equals(
                        x.Key,
                        key,
                        StringComparison.OrdinalIgnoreCase));


            if(provider==null)
            {
                throw new InvalidOperationException(
                    $"Report provider '{key}' not registered");
            }


            return provider;
        }

    }
}