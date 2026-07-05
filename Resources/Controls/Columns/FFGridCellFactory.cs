//using Microsoft.Maui.Controls;
//using Microsoft.Maui.Graphics;
//using EHMR.Resources.Controls.Cells;
//using EHMR.Resources.Controls.Columns;
//using Microsoft.Maui.Controls.Shapes;

//namespace EHMR.Resources.Controls;

//internal static class FFGridCellFactory
//{
//    // =========================================================
//    // TEXT CELL
//    // =========================================================
//    public static View CreateTextCell(FFTextColumn column, object item)
//    {
//        var label = new Label
//        {
//            VerticalOptions=LayoutOptions.Center,
//            LineBreakMode=LineBreakMode.TailTruncation,
//            FontSize=column.FontSize,
//            FontAttributes=column.FontAttributes,
//        };

//        if(column.TextColor!=null)
//            label.TextColor=column.TextColor;

//        label.SetBinding(Label.TextProperty, column.BindingPath);

//        return WrapCell(label, column);
//    }

//    public static View CreateTextHeader(FFTextColumn column)
//    {
//        return new Label
//        {
//            Text=column.Header,
//            FontSize=column.HeaderFontSize,
//            FontAttributes=column.IsBoldHeader ? FontAttributes.Bold : FontAttributes.None,
//            VerticalOptions=LayoutOptions.Center
//        };
//    }


//    // =========================================================
//    // ACTION CELL
//    // =========================================================
//    public static View CreateActionCell(FFActionColumn column, object item)
//    {
//        var button = new Button
//        {
//            Text=column.Icon,
//            FontSize=14,
//            BackgroundColor=Colors.Transparent,
//            WidthRequest=40,
//            HeightRequest=40
//        };

//        button.Command=column.Command;
//        button.CommandParameter=item;

//        return WrapCell(button, column);
//    }

//    public static View CreateActionHeader(FFActionColumn column)
//    {
//        return new Label
//        {
//            Text=column.Header,
//            FontSize=11,
//            FontAttributes=FontAttributes.Bold
//        };
//    }

//    // =========================================================
//    // TEMPLATE CELL
//    // =========================================================
//    public static View CreateTemplateCell(FFTemplateColumn column, object item)
//    {
//        var view = column.CellTemplate?.CreateContent() as View;

//        if(view!=null)
//            view.BindingContext=item;

//        return WrapCell(view, column);
//    }

//    public static View CreateTemplateHeader(FFTemplateColumn column)
//    {
//        return new Label
//        {
//            Text=column.Header,
//            FontSize=11,
//            FontAttributes=FontAttributes.Bold
//        };
//    }

//    // =========================================================
//    // CORE WRAPPER (NOW ENGINE-GRADE)
//    // =========================================================
//    private static View WrapCell(View view, FFDataGridColumn column)
//    {
//        return new Grid
//        {
//            Padding=new Thickness(10, 6),

//            HorizontalOptions=
//                column.Alignment==FFAlign.Center ? LayoutOptions.Center :
//                column.Alignment==FFAlign.Right ? LayoutOptions.End :
//                LayoutOptions.Start,

//            VerticalOptions=LayoutOptions.Center,

//            Children=
//            {
//                view
//            }
//        };
//    }
//}