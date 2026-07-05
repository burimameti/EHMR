//using EHMR.Resources.Controls.Columns;
//using Microsoft.Maui.Controls;

//namespace EHMR.Resources.Controls;

//internal static class FFGridColumnFactory
//{
//    // =========================================================
//    // NORMALIZE COLUMN HEADER
//    // =========================================================
//    public static View CreateHeader(FFDataGridColumn column)
//    {
//        return column.CreateHeader();
//    }

//    // =========================================================
//    // NORMALIZE CELL
//    // =========================================================
//    public static View CreateCell(FFDataGridColumn column, object item)
//    {
//        return column.CreateCell(item);
//    }

//    // =========================================================
//    // HELPERS (optional future extension point)
//    // =========================================================
//    public static BindingBase? ResolveBinding(FFDataGridColumn column)
//    {
//        return new Binding(column.BindingPath);
//    }
//}