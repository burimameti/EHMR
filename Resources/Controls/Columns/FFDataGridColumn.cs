using Microsoft.Maui.Controls;

namespace EHMR.Resources.Controls.Columns;

public abstract class FFDataGridColumn
{
    // =========================================================
    // CORE IDENTITY
    // =========================================================
    public string Header { get; set; } = string.Empty;

    public string BindingPath { get; set; } = string.Empty;

    // =========================================================
    // LAYOUT
    // =========================================================
    public double Width { get; set; } = -1; // -1 = auto

    public FFAlign Alignment { get; set; } = FFAlign.Left;

    // =========================================================
    // FEATURES
    // =========================================================
    public bool Sortable { get; set; } = true;

    public bool Visible { get; set; } = true;

    // =========================================================
    // FORMATTING / DISPLAY
    // =========================================================
    public FFColumnType Type { get; set; } = FFColumnType.Text;

    public string Format { get; set; } = string.Empty;

    public bool TruncateText { get; set; } = true;

    // =========================================================
    // UI OVERRIDES (optional styling per column)
    // =========================================================
    public FontAttributes HeaderFontAttributes { get; set; } = FontAttributes.Bold;

    public double HeaderFontSize { get; set; } = 12;

    public double CellFontSize { get; set; } = 13;

    public TextAlignment TextAlignment { get; set; } = TextAlignment.Start;

    // =========================================================
    // RENDER CONTRACT (ENGINE CORE)
    // =========================================================
    internal abstract View CreateHeader();

    internal abstract View CreateCell(object item);
}