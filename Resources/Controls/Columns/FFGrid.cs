using EHMR.Resources.Controls.Columns;
using System.Collections;
using System.Collections.Generic;

namespace EHMR.Resources.Controls;

public class FFGrid
{
    public IList<FFDataGridColumn> Columns
    {
        get;
    }
    public IEnumerable Items
    {
        get;
    }

    public FFGrid(IList<FFDataGridColumn> columns, IEnumerable items)
    {
        Columns=columns;
        Items=items;
    }
}