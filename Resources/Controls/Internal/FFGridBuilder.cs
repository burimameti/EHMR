using EHMR.Resources.Controls.Columns;
using System.Collections;
using System.Collections.Generic;

namespace EHMR.Resources.Controls;

public class FFGridBuilder
{
    private readonly List<FFDataGridColumn> _columns = new();
    private IEnumerable _items;

    public FFGridBuilder Columns(params FFDataGridColumn[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    public FFGridBuilder Items(IEnumerable items)
    {
        _items=items;
        return this;
    }

    public FFGrid Build()
    {
        // IMPORTANT: freeze columns to avoid runtime mutation bugs
        var frozenColumns = _columns.AsReadOnly();

        return new FFGrid(frozenColumns, _items);
    }
}