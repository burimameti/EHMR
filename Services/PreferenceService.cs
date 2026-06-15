using EHMR.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{
    //public interface IPreferencesService
    //{
    //    void Save(string key, string value);

    //    string Load(string key, string defaultValue = "");

    //    bool ContainsKey(string key);

    //    void Remove(string key);
    //}

    public class SecurePreferencesService : IPreferencesService
    {
        public void Save(string key, string value)
        {
            SecureStorage.SetAsync(key, value).Wait();
        }

        public string Load(string key, string defaultValue = "")
        {
            try
            {
                var task = SecureStorage.GetAsync(key);
                task.Wait();
                return task.Result??defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public bool ContainsKey(string key)
        {
            // SecureStorage does not support ContainsKey
            // So, use a fallback check with regular Preferences
            return Preferences.ContainsKey($"secure_exists_{key}");
        }

        public void Remove(string key)
        {
            SecureStorage.Remove(key);
            Preferences.Remove($"secure_exists_{key}");
        }
    }
}