//using Microsoft.Maui.Controls;
//using System.Windows.Input;

//namespace EHMR.Resources.Controls.Columns;

//public class FFActionColumn : FFDataGridColumn
//{
//    public string Icon { get; set; } = "⋯";

//    public ICommand? Command
//    {
//        get; set;
//    }

//    internal override View CreateHeader()
//    {
//        return FFGridCellFactory.CreateActionHeader(this);
//    }

//    internal override View CreateCell(object item)
//    {
//        return FFGridCellFactory.CreateActionCell(this, item);
//    }
//}