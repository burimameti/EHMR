using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace EHMR.Resources.Theming;

/// <summary>
/// Plain data holder for a single theme's design tokens. Produced by
/// <see cref="FFThemeFactory"/>, consumed by <see cref="FFThemeManager"/>
/// (pushed into Application.Current.Resources as DynamicResource keys) and by
/// <see cref="EHMR.Resources.Controls.FFDataGrid"/> (copied into its own
/// bindable properties). Keep this class as a pure data bag — no logic here.
/// </summary>
public sealed class FFThemeTokens
{
    //==================================================
    // ROWS
    //==================================================

    public Brush RowBackground { get; set; } = new SolidColorBrush(Colors.White);
    public Brush AlternateRowBackground { get; set; } = new SolidColorBrush(Colors.White);
    public Brush HoverRowBackground { get; set; } = new SolidColorBrush(Colors.Transparent);
    public Brush SelectedRowBackground { get; set; } = new SolidColorBrush(Colors.Transparent);
    public Brush SelectedRowBorderBrush { get; set; } = new SolidColorBrush(Colors.Transparent);
    public double SelectedRowBorderThickness { get; set; } = 3;
    //==================================================
    // SURFACES
    //==================================================

    public Brush SurfaceBackground { get; set; } = new SolidColorBrush(Colors.White);
    public Brush HeaderBackground { get; set; } = new SolidColorBrush(Colors.White);
    public Brush FooterBackground { get; set; } = new SolidColorBrush(Colors.White);

    //==================================================
    // ROWS
    //==================================================

 

    public Brush HeaderHoverBackground { get; set; } = new SolidColorBrush(Colors.Transparent);
    public Brush CellHoverBackground { get; set; } = new SolidColorBrush(Colors.Transparent);
    public Brush SelectedCellBackground { get; set; } = new SolidColorBrush(Colors.Transparent);

    public double RowHeight { get; set; } = 52;

    //==================================================
    // TYPOGRAPHY
    //==================================================

    public Brush HeaderForeground { get; set; } = new SolidColorBrush(Colors.Black);

    public FontAttributes HeaderFontAttributes { get; set; } = FontAttributes.Bold;

    public double HeaderFontSize { get; set; } = 14;
    public double CellFontSize { get; set; } = 13;
    public double FooterFontSize { get; set; } = 12;

    public Color PrimaryTextColor { get; set; } = Colors.Black;
    public Color SecondaryTextColor { get; set; } = Colors.DarkGray;
    public Color MutedTextColor { get; set; } = Colors.Gray;

    //==================================================
    // BORDERS
    //==================================================

    public Brush BorderBrush { get; set; } = new SolidColorBrush(Colors.Transparent);
    public Brush DividerBrush { get; set; } = new SolidColorBrush(Colors.Transparent);
    public double BorderThickness { get; set; } = 1;

    //==================================================
    // SPACING
    //==================================================

    public Thickness HeaderPadding { get; set; } = new Thickness(16, 10);
    public Thickness CellPadding { get; set; } = new Thickness(14, 10);
    public Thickness FooterPadding { get; set; } = new Thickness(16, 8);

    public double HeaderHeight { get; set; } = 44;
    public double FooterHeight { get; set; } = 44;

    public CornerRadius CornerRadius { get; set; } = new CornerRadius(16);

    //==================================================
    // ACCENT
    //==================================================

    public Brush AccentBrush { get; set; } = new SolidColorBrush(Colors.Blue);
    public Brush AccentForeground { get; set; } = new SolidColorBrush(Colors.White);

    //==================================================
    // PAGER
    //==================================================

    public Brush PagerButtonBackground { get; set; } = new SolidColorBrush(Colors.White);
    public Brush PagerButtonHover { get; set; } = new SolidColorBrush(Colors.Transparent);
    public Brush PagerActiveBackground { get; set; } = new SolidColorBrush(Colors.Blue);
    public Brush PagerActiveForeground { get; set; } = new SolidColorBrush(Colors.White);

    //==================================================
    // BADGES
    //==================================================

    public Brush BadgeSuccess { get; set; } = new SolidColorBrush(Colors.Transparent);
    public Brush BadgeWarning { get; set; } = new SolidColorBrush(Colors.Transparent);
    public Brush BadgeDanger { get; set; } = new SolidColorBrush(Colors.Transparent);
    public Brush BadgeInfo { get; set; } = new SolidColorBrush(Colors.Transparent);

    //==================================================
    // EFFECTS
    //==================================================

    public Brush OverlayBackground { get; set; } = new SolidColorBrush(Color.FromArgb("#44000000"));
    public Brush ShadowBrush { get; set; } = new SolidColorBrush(Colors.Black);
    public float ShadowOpacity { get; set; } = 0.10f;
    public double ShadowRadius { get; set; } = 14;
}