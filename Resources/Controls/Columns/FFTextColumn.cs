using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace EHMR.Resources.Controls.Columns;

public class FFTextColumn : FFDataGridColumn
{
    // =========================================
    // DATA BINDING
    // =========================================
   

    // =========================================
    // TEXT STYLE (intent only)
    // =========================================
    public FontAttributes FontAttributes { get; set; } = FontAttributes.None;

    public double FontSize { get; set; } = 13;

    public Color? TextColor
    {
        get; set;
    }

    public TextAlignment HorizontalTextAlignment { get; set; } = TextAlignment.Start;

    // =========================================
    // HEADER STYLE
    // =========================================
    public bool IsBoldHeader { get; set; } = true;

    public double HeaderFontSize { get; set; } = 12;

    // =========================================
    // CORE CONTRACT (NO UI HERE)
    // =========================================
    internal override View CreateCell(object item)
    {
        // IMPORTANT:
        // Column does NOT render UI anymore
        throw new InvalidOperationException(
            "FFTextColumn should be rendered by GridCellFactory, not directly.");
    }

    internal override View CreateHeader()
    {
        throw new InvalidOperationException(
            "FFTextColumn should be rendered by GridCellFactory, not directly.");
    }
}