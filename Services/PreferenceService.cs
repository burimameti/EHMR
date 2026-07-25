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
        public async Task SaveAsync(string key, string value)
        {
            await SecureStorage.SetAsync(key, value);
            Preferences.Set($"secure_exists_{key}", true); // ContainsKey relies on this
        }

        public async Task<string> LoadAsync(string key, string defaultValue = "")
        {
            try
            {
                var value = await SecureStorage.GetAsync(key);
                return value??defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public bool ContainsKey(string key)
        {
            // SecureStorage has no native ContainsKey, so we track existence
            // with a companion flag in regular Preferences.
            return Preferences.ContainsKey($"secure_exists_{key}");
        }

        public void Remove(string key)
        {
            SecureStorage.Remove(key);
            Preferences.Remove($"secure_exists_{key}");
        }
    }
}