using EHMR.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{
    public class NavigationDataStore : INavigationDataStore
    {
        private readonly Dictionary<string, object> _store = new();

        public void SetData<T>(string key, T data) => _store[key]=data!;

        public T? GetData<T>(string key)
        {
            if(_store.TryGetValue(key, out var value)&&value is T t)
                return t;

            return default;
        }

        public void Clear(string key) => _store.Remove(key);
    }
}