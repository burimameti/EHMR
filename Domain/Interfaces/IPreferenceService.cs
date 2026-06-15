namespace EHMR.Domain.Interfaces
{
    public interface IPreferencesService
    {
        void Save(string key, string value);

        string Load(string key, string defaultValue = "");

        bool ContainsKey(string key);

        void Remove(string key);
    }
}