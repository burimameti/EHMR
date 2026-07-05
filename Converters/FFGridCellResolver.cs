using CommunityToolkit.Mvvm.ComponentModel;
using DocumentFormat.OpenXml.Spreadsheet;
using EHMR.Resources.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Converters
{

    public partial class FFGridState<T> : ObservableObject
    {
        private ObservableCollection<T> _items = new();
        public ObservableCollection<T> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        public ObservableCollection<FFGridColumn<T>> Columns { get; } = new();

        private string? _sortedColumn;
        private bool _ascending = true;

        public bool IsAscending => _ascending;
        public string? SortedColumn => _sortedColumn;

        public void SetItems(IEnumerable<T> items)
        {
            Items=new ObservableCollection<T>(items);
        }

        public void Sort(string columnKey, Func<IEnumerable<T>, IEnumerable<T>> sortFunc)
        {
            if(_sortedColumn==columnKey)
                _ascending=!_ascending;
            else
            {
                _sortedColumn=columnKey;
                _ascending=true;
            }

            var sorted = sortFunc(Items);

            Items=new ObservableCollection<T>(
                _ascending ? sorted : sorted.Reverse()
            );
        }
    }


    public class FFGridColumn<T>
    {
        public string Header { get; set; } = "";

        public string Key { get; set; } = "";

        public double Width { get; set; } = 1;

        public bool IsSortable { get; set; } = true;

        public bool IsFrozen { get; set; } = false;

        public TextAlignment Alignment { get; set; } = TextAlignment.Start;

        public Func<T, object?> ValueSelector { get; set; } = _ => null;

        public Func<T, IComparable?>? SortSelector
        {
            get; set;
        }

        public DataTemplate? CellTemplate
        {
            get; set;
        }
    }
    public static class FFGridCellResolver
    {
        public static object? Resolve<T>(T item, FFGridColumn<T> column)
        {
            return column.ValueSelector?.Invoke(item);
        }

        public static IComparable? ResolveSort<T>(T item, FFGridColumn<T> column)
        {
            return column.SortSelector?.Invoke(item);
        }
    }
}
