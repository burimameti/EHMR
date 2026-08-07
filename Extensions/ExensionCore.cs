using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Core.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static void ReplaceT<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            collection.Clear();
            foreach(var item in items)
                collection.Add(item);
        }
    }
}