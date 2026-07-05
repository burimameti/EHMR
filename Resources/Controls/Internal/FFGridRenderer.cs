//using EHMR.Resources.Controls.Columns;
//using Microsoft.Maui.Controls;
//using System.Collections;
//using System.Collections.Generic;

//namespace EHMR.Resources.Controls;

//internal static class FFGridRenderer
//{
//    // ===========================
//    // HEADER
//    // ===========================
//    internal static View BuildHeader(IList<FFDataGridColumn> columns)
//    {
//        var grid = new Grid
//        {
//            ColumnSpacing=0
//        };

//        foreach(var col in columns)
//        {
//            grid.ColumnDefinitions.Add(new ColumnDefinition(GetWidth(col)));
//            var header = FFGridColumnFactory.CreateHeader(col);

//            grid.Add(header);
//            Grid.SetColumn(header, grid.ColumnDefinitions.Count-1);
//        }

//        return grid;
//    }

//    // ===========================
//    // ROWS
//    // ===========================
//    public static void BuildRows(Grid row, IList<FFDataGridColumn> columns, object item)
//    {
//        row.ColumnDefinitions.Clear();
//        row.Children.Clear();

//        foreach(var col in columns)
//        {
//            if(!col.Visible) continue;

//            row.ColumnDefinitions.Add(new ColumnDefinition(col.Width>0
//                ? new GridLength(col.Width)
//                : GridLength.Star));

//            var cell = col.CreateCell(item);

//            row.Add(cell);
//            Grid.SetColumn(cell, row.ColumnDefinitions.Count-1);
//        }
//    }

//    internal static View BuildRow(IList<FFDataGridColumn> columns, object item)
//    {
//        var grid = new Grid
//        {
//            ColumnSpacing=0
//        };

//        for(int i = 0; i<columns.Count; i++)
//        {
//            var col = columns[i];

//            grid.ColumnDefinitions.Add(new ColumnDefinition(GetWidth(col)));

//            var cell = FFGridColumnFactory.CreateCell(col, item);

//            grid.Add(cell);
//            Grid.SetColumn(cell, i);
//        }

//        return grid;
//    }

//    // ===========================
//    // WIDTH ENGINE
//    // ===========================
//    private static GridLength GetWidth(FFDataGridColumn col)
//    {
//        if(col.Width>0)
//            return new GridLength(col.Width);

//        return new GridLength(1, GridUnitType.Star);
//    }
//}