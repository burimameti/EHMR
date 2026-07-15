using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace EHMR.Resources.Theming;

public static class FFThemeManager
{
    public static FFThemeVariant CurrentVariant
    {
        get; private set;
    }

    public static FFThemeTokens Current { get; private set; } = null!;

    public static event EventHandler<FFThemeTokens>? ThemeChanged;

    static FFThemeManager()
    {
        ApplyTheme(FFThemeVariant.Sparked);
    }

    /// <summary>
    /// Sets the app-wide active theme: builds fresh tokens for the given variant,
    /// pushes them into Application.Current.Resources (so every DynamicResource
    /// binding across the app updates), and raises ThemeChanged so any
    /// per-instance listeners (e.g. FFDataGrid) can sync their own bindable
    /// properties too. This is the ONLY method that should be called when the
    /// user/page wants to change the active theme — see FFDataGrid.ApplyCurrentTheme,
    /// which now routes through this instead of the old Get()-only path.
    /// </summary>
    public static void ApplyTheme(FFThemeVariant variant)
    {
        CurrentVariant=variant;
        Current=FFThemeFactory.Create(variant);

        PushToApplicationResources(Current);

        ThemeChanged?.Invoke(null, Current);
    }

    /// <summary>
    /// Returns tokens for a variant WITHOUT touching global state or firing
    /// ThemeChanged. Use only when you explicitly need a one-off token set
    /// (e.g. previewing a theme) that should NOT become the app-wide theme.
    /// For normal "switch the theme" use ApplyTheme(variant) instead.
    /// </summary>
    public static FFThemeTokens Get(FFThemeVariant variant) =>
        FFThemeFactory.Create(variant);

    private static void PushToApplicationResources(FFThemeTokens t)
    {
        var res = Application.Current?.Resources;

        if(res is null)
            return;

        //==================================================
        // SURFACES
        //==================================================

        res["FFSurfaceBackgroundBrush"]=t.SurfaceBackground;
        res["FFHeaderBackgroundBrush"]=t.HeaderBackground;
        res["FFFooterBackgroundBrush"]=t.FooterBackground;

        //==================================================
        // INTERACTION
        //==================================================

        
        res["FFHeaderHoverBackground"]=t.HeaderHoverBackground;
        res["FFCellHoverBackground"]=t.CellHoverBackground;
        res["FFSelectedCellBackground"]=t.SelectedCellBackground;

        //==================================================
        // BORDERS
        //==================================================

        res["FFBorderBrush"]=t.BorderBrush;
        res["FFDividerBrush"]=t.DividerBrush;

        // Color-typed counterparts — needed anywhere a Color-only property
        // is bound (BoxView.BackgroundColor, Button.BorderColor, etc.).
        // Binding a Brush resource to a Color-typed property fails SILENTLY
        // in MAUI, which is what made row dividers / clear-filter borders
        // invisible before these were added.
        res["FFBorderColor"]=ColorOf(t.BorderBrush);
        res["FFDividerColor"]=ColorOf(t.DividerBrush);

        //==================================================
        // ACCENT
        //==================================================

        res["FFAccentBrush"]=t.AccentBrush;
        res["FFAccentForegroundBrush"]=t.AccentForeground;
        res["FFAccentForegroundColor"]=ColorOf(t.AccentForeground);

        //==================================================
        // STATUS / BADGES
        //==================================================

        res["FFBadgeSuccess"]=t.BadgeSuccess;
        res["FFBadgeWarning"]=t.BadgeWarning;
        res["FFBadgeDanger"]=t.BadgeDanger;
        res["FFBadgeInfo"]=t.BadgeInfo;

        //==================================================
        // PAGER
        //==================================================

        res["FFPagerButtonBackground"]=t.PagerButtonBackground;
        res["FFPagerButtonHover"]=t.PagerButtonHover;
        res["FFPagerActiveBackground"]=t.PagerActiveBackground;
        res["FFPagerActiveForeground"]=t.PagerActiveForeground;

        //==================================================
        // TEXT
        //==================================================

        res["FFHeaderForegroundBrush"]=t.HeaderForeground;
        res["FFHeaderForegroundColor"]=ColorOf(t.HeaderForeground);

        res["FFPrimaryTextColor"]=t.PrimaryTextColor;
        res["FFSecondaryTextColor"]=t.SecondaryTextColor;
        res["FFMutedTextColor"]=t.MutedTextColor;

        //==================================================
        // SHADOW / OVERLAY
        //==================================================

        res["FFOverlayBackgroundBrush"]=t.OverlayBackground;
        res["FFShadowBrush"]=t.ShadowBrush;
        res["FFShadowOpacity"]=t.ShadowOpacity;
        res["FFShadowRadius"]=t.ShadowRadius;

        //==================================================
        // LAYOUT
        //==================================================

        res["FFCornerRadius"]=t.CornerRadius;
        res["FFRowHeight"]=t.RowHeight;
        res["FFHeaderHeight"]=t.HeaderHeight;
        res["FFFooterHeight"]=t.FooterHeight;

        //==================================================
        // TYPOGRAPHY
        //==================================================

        res["FFHeaderFontSize"]=t.HeaderFontSize;
        res["FFCellFontSize"]=t.CellFontSize;
        res["FFFooterFontSize"]=t.FooterFontSize;

        //==================================================
        // SPACING
        //==================================================

        res["FFHeaderPadding"]=t.HeaderPadding;
        res["FFCellPadding"]=t.CellPadding;
        res["FFFooterPadding"]=t.FooterPadding;

        res["FFRowBackgroundBrush"]=t.RowBackground;
        res["FFAlternateRowBackgroundBrush"]=t.AlternateRowBackground;
        res["FFHoverRowBackgroundBrush"]=t.HoverRowBackground;
        res["FFSelectedRowBackgroundBrush"]=t.SelectedRowBackground;
        res["FFSelectedRowBorderBrush"]=t.SelectedRowBorderBrush;
        res["FFSelectedRowBorderColor"]=ColorOf(t.SelectedRowBorderBrush);
        res["FFSelectedRowBorderThickness"]=t.SelectedRowBorderThickness;



    }

    /// <summary>
    /// Safely pulls a flat Color out of a Brush token for the Color-typed
    /// DynamicResource counterparts. Falls back to Transparent for non-solid
    /// brushes (gradients etc.) since there's no single "the" color in that case.
    /// </summary>
    private static Color ColorOf(Brush brush) =>
        brush is SolidColorBrush solid ? solid.Color : Colors.Transparent;
}