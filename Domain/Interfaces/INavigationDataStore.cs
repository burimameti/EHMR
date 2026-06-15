namespace EHMR.Domain.Interfaces
{
    public interface INavigationDataStore
    {
        void SetData<T>(string key, T data);

        T? GetData<T>(string key);

        void Clear(string key);
    }
}