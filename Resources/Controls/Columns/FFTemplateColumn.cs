using Microsoft.Maui.Controls;

namespace EHMR.Resources.Controls.Columns;

public class FFTemplateColumn : FFDataGridColumn
{
    public DataTemplate? CellTemplate
    {
        get; set;
    }
    public DataTemplate? HeaderTemplate
    {
        get; set;
    }

    internal override View CreateHeader()
    {
        if(HeaderTemplate?.CreateContent() is View v)
            return v;

        return new Label { Text=Header };
    }

    internal override View CreateCell(object item)
    {
        if(CellTemplate?.CreateContent() is View v)
        {
            v.BindingContext=item;
            return v;
        }

        return new Label { Text=item?.ToString() };
    }
}