namespace EHMR.Domain.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    public interface IPreferencesService
    {
        Task SaveAsync(string key, string value);

        Task<string> LoadAsync(string key, string defaultValue = "");

        bool ContainsKey(string key);

        void Remove(string key);
    }
}