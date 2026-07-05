using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain
{
    public class DataGridQuery
    {
        public string? Search
        {
            get; set;
        }

        public string? SortColumn
        {
            get; set;
        }

        public bool SortDesc
        {
            get; set;
        }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;

        public Dictionary<string, string> Filters { get; set; } = new();
    }

    public static class DataGridResolver
    {
        public static object? GetValue<T>(T row, DataGridColumn<T> column)
            => column.Value(row);

        public static string GetDisplay<T>(T row, DataGridColumn<T> column)
        {
            var value = column.Value(row);

            if(value is null)
                return string.Empty;

            if(column.Display!=null)
                return column.Display(value);

            return value.ToString()??string.Empty;
        }
    }

    public class DataGridSort
    {
        public string? ColumnKey
        {
            get; set;
        }

        public bool Desc
        {
            get; set;
        }
    }

    public class DataGridPage
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;

        public int Total
        {
            get; set;
        }
    }

    public class DataGridAction<T>
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";
        public Func<T, Task> Execute { get; set; } = _ => Task.CompletedTask;
    }
}