using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace EHMR.Resources.Theming;

using static FFBrushes;

public static partial class FFThemeFactory
{
    public static FFThemeTokens Create(FFThemeVariant variant)
    {
        return variant switch
        {
            FFThemeVariant.Sparked => Sparked(),
            FFThemeVariant.Hospital => HospitalLight(),
            FFThemeVariant.Classic => Classic(),
            FFThemeVariant.CoolBlueLight => MedicalBlue(),
            FFThemeVariant.MilkLight => Milk(),
            FFThemeVariant.OrangeDark => OrangeDark(),
            FFThemeVariant.SunsetDark => SunsetDark(),
            // Dark themes

            FFThemeVariant.DeepBlueDark => DeepBlueDark(),
            FFThemeVariant.Panda => Panda(),

            // NOTE: this variant needs a matching member added to the
            // FFThemeVariant enum (e.g. "SunsetDark") before this case
            // can be wired up. Left commented so the file still compiles
            // against the current enum — uncomment once the enum has it.
            // FFThemeVariant.SunsetDark => SunsetDark(),

            _ => Sparked()
        };
    }
    private static FFThemeTokens OrangeDark()
    {
        var t = BaseDark();

        //==================================================
        // SURFACES — GitHub-dark inspired
        //==================================================

        t.SurfaceBackground=Brush(Color.FromArgb("#0D1117"));
        t.HeaderBackground=Brush(Color.FromArgb("#161B22"));
        t.FooterBackground=Brush(Color.FromArgb("#161B22"));

        //==================================================
        // GRID
        //==================================================
        t.SelectedRowBorderBrush=Brush(t.AccentBrush is SolidColorBrush b ? b.Color : Colors.White); // or set explicitly per theme's accent color
        t.SelectedRowBorderThickness=3;
        t.RowBackground=Brush(Color.FromArgb("#0D1117"));
        t.AlternateRowBackground=Brush(Color.FromArgb("#161B22"));
        t.HoverRowBackground=Brush(Color.FromArgb("#1C2128"));
        t.SelectedRowBackground=Brush(Color.FromArgb("#3A2711"));

        //==================================================
        // HEADER
        //==================================================

        t.HeaderForeground=Brush(Color.FromArgb("#E6EDF3"));

        //==================================================
        // BORDERS
        //==================================================

        t.BorderBrush=Brush(Color.FromArgb("#30363D"));
        t.DividerBrush=Brush(Color.FromArgb("#21262D"));

        //==================================================
        // ACCENT — orange reserved for buttons/badges/selection only
        //==================================================

        t.AccentBrush=Brush(Color.FromArgb("#F0883E"));
        t.AccentForeground=Brush(FFColors.White);

        //==================================================
        // TEXT
        //==================================================

        t.PrimaryTextColor=Color.FromArgb("#E6EDF3");
        t.SecondaryTextColor=Color.FromArgb("#8B949E");
        t.MutedTextColor=Color.FromArgb("#6E7681");

        //==================================================
        // PAGER
        //==================================================

        t.PagerActiveBackground=Brush(Color.FromArgb("#F0883E"));
        t.PagerActiveForeground=Brush(FFColors.White);

        //==================================================
        // EFFECTS
        //==================================================

        t.ShadowBrush=Brush(FFColors.Black);
        t.ShadowOpacity=.22f;

        return t;
    }
    //======================================================================
    // SPARKED — default brand light theme, azure accent
    //======================================================================

    private static FFThemeTokens Sparked()
    {
        var t = BaseLight();

        // SURFACES

        t.SurfaceBackground=Brush(FFColors.White);
        t.HeaderBackground=Brush(FFColors.Azure50);
        t.FooterBackground=Brush(FFColors.White);

        // ROWS

        t.RowBackground=Brush(FFColors.White);
        t.AlternateRowBackground=Brush(FFColors.Slate50);
        t.HoverRowBackground=Brush(FFColors.Azure50);
        t.SelectedRowBackground=Brush(FFColors.Azure100);
        t.SelectedRowBorderBrush=Brush(FFColors.Azure700);
        t.SelectedRowBorderThickness=3;
        // HEADER / TEXT

        t.HeaderForeground=Brush(FFColors.Slate700);
        t.PrimaryTextColor=FFColors.Slate900;
        t.SecondaryTextColor=FFColors.Slate600;
        t.MutedTextColor=FFColors.Slate400;

        // BORDERS

        t.BorderBrush=Brush(FFColors.Slate200);
        t.DividerBrush=Brush(FFColors.Slate150);

        // ACCENT

        t.AccentBrush=Brush(FFColors.Azure700);
        t.AccentForeground=Brush(FFColors.White);

        // PAGER

        t.PagerActiveBackground=Brush(FFColors.Azure700);

        // EFFECTS

        t.CornerRadius=new CornerRadius(14);
        t.ShadowBrush=Brush(FFColors.Azure700);
        t.ShadowOpacity=.05f;
        t.ShadowRadius=16;

        return t;
    }

    //======================================================================
    // HOSPITAL — clinical, white surfaces, orange accent
    //======================================================================

    private static FFThemeTokens HospitalLight()
    {
        var t = BaseLight();

        // SURFACES

        t.SurfaceBackground=Brush(FFColors.Slate50);
        t.HeaderBackground=Brush(FFColors.White);
        t.FooterBackground=Brush(FFColors.White);

        // ROWS

        t.RowBackground=Brush(FFColors.White);
        t.AlternateRowBackground=Brush(FFColors.Gray100);
        t.HoverRowBackground=Brush(FFColors.Orange100);
        t.SelectedRowBackground=Brush(FFColors.Orange200);
        t.SelectedRowBorderBrush=Brush(Color.FromArgb("#FF8A00"));
        t.SelectedRowBorderThickness=3;
        // HEADER / TEXT

        t.HeaderForeground=Brush(FFColors.Slate800);
        t.PrimaryTextColor=FFColors.Slate900;
        t.SecondaryTextColor=FFColors.Slate600;
        t.MutedTextColor=FFColors.Slate400;

        // BORDERS

        t.BorderBrush=Brush(FFColors.Slate200);
        t.DividerBrush=Brush(FFColors.Slate150);

        // ACCENT

        t.AccentBrush=Brush(FFColors.Orange600);
        t.AccentForeground=Brush(FFColors.White);

        // PAGER

        t.PagerActiveBackground=Brush(FFColors.Orange600);

        // EFFECTS

        t.CornerRadius=new CornerRadius(14);
        t.ShadowBrush=Brush(FFColors.Slate400);
        t.ShadowOpacity=.08f;
        t.ShadowRadius=14;

        return t;
    }

    //======================================================================
    // CLASSIC — tight, corporate, minimal shadow, azure accent
    //======================================================================

    private static FFThemeTokens Classic()
    {
        var t = BaseLight();

        //--------------------------------------------------
        // SURFACE
        //--------------------------------------------------
        t.SelectedRowBorderBrush=Brush(FFColors.Purple600);
        t.SelectedRowBorderThickness=3;
        t.SurfaceBackground=Brush(FFColors.White);
        t.HeaderBackground=Brush(FFColors.Neutral50);
        t.FooterBackground=Brush(FFColors.Neutral50);

        //--------------------------------------------------
        // ROWS
        //--------------------------------------------------

        t.RowBackground=Brush(FFColors.White);
        t.AlternateRowBackground=Brush(FFColors.Neutral25);
        t.HoverRowBackground=Brush(FFColors.Neutral100);
        t.SelectedRowBackground=Brush(FFColors.Purple100);

        //--------------------------------------------------
        // HEADER
        //--------------------------------------------------

        t.HeaderForeground=Brush(FFColors.Neutral800);

        //--------------------------------------------------
        // BORDERS
        //--------------------------------------------------

        t.BorderBrush=Brush(FFColors.Neutral200);
        t.DividerBrush=Brush(FFColors.Neutral150);

        //--------------------------------------------------
        // ACCENT — matches the violet logo mark
        //--------------------------------------------------

        t.AccentBrush=Brush(FFColors.Purple600);
        t.AccentForeground=Brush(FFColors.White);

        //--------------------------------------------------
        // TEXT
        //--------------------------------------------------

        t.PrimaryTextColor=FFColors.Neutral900;
        t.SecondaryTextColor=FFColors.Neutral600;
        t.MutedTextColor=FFColors.Neutral400;

        //--------------------------------------------------
        // BADGES — colorful multi-tone pills (Purchased / Sponsored / grade letters)
        //--------------------------------------------------

        t.BadgeSuccess=Brush(FFColors.SuccessLight);
        t.BadgeWarning=Brush(FFColors.WarningLight);
        t.BadgeDanger=Brush(FFColors.ErrorLight);
        t.BadgeInfo=Brush(FFColors.InfoLight);

        //--------------------------------------------------
        // STYLE — dense rows, small type
        //--------------------------------------------------

        t.CornerRadius=10;

        t.HeaderFontSize=12.5;
        t.CellFontSize=12.5;

        t.RowHeight=48;
        t.HeaderHeight=40;

        t.ShadowOpacity=.04f;

        return t;
    }

    //======================================================================
    // MEDICAL BLUE — cool clinical, azure surfaces
    //======================================================================

    private static FFThemeTokens MedicalBlue()
    {
        var t = BaseLight();

        //==================================================
        // SURFACES
        //==================================================
        t.SelectedRowBorderBrush=Brush(FFColors.Azure600);
        t.SelectedRowBorderThickness=3;

        t.SurfaceBackground=Brush(FFColors.Slate50);
        t.HeaderBackground=Brush(FFColors.White);
        t.FooterBackground=Brush(FFColors.White);

        //==================================================
        // GRID
        //==================================================

        t.RowBackground=Brush(FFColors.White);
        t.AlternateRowBackground=Brush(FFColors.Azure25);
        t.HoverRowBackground=Brush(FFColors.Azure50);
        t.SelectedRowBackground=Brush(FFColors.Azure100);

        //==================================================
        // HEADER
        //==================================================

        t.HeaderForeground=Brush(FFColors.Slate900);

        //==================================================
        // BORDERS
        //==================================================

        t.BorderBrush=Brush(FFColors.Slate200);
        t.DividerBrush=Brush(FFColors.Slate150);

        //==================================================
        // ACCENT
        //==================================================

        t.AccentBrush=Brush(FFColors.Azure600);
        t.AccentForeground=Brush(FFColors.White);

        //==================================================
        // TEXT
        //==================================================

        t.PrimaryTextColor=FFColors.Slate900;
        t.SecondaryTextColor=FFColors.Slate600;
        t.MutedTextColor=FFColors.Slate400;

        //==================================================
        // STYLE — big rounded card, roomy rows (avatar + 2-line cells)
        //==================================================

        t.CornerRadius=20;
        t.RowHeight=60;
        t.HeaderHeight=48;

        //==================================================
        // EFFECTS
        //==================================================

        t.ShadowBrush=Brush(FFColors.Slate400);
        t.ShadowOpacity=.08f;

        return t;
    }

    //======================================================================
    // MILK — warm, off-white, orange accent
    //======================================================================

    private static FFThemeTokens Milk()
    {
        var t = BaseLight();

        //--------------------------------------------------
        // SURFACE
        //--------------------------------------------------

        t.SurfaceBackground=Brush(FFColors.White);
        t.HeaderBackground=Brush(FFColors.White);
        t.FooterBackground=Brush(FFColors.White);

        //--------------------------------------------------
        // ROWS — flat, no zebra
        //--------------------------------------------------
        t.SelectedRowBorderBrush=Brush(Colors.Transparent);
        t.RowBackground=Brush(FFColors.White);
        t.AlternateRowBackground=Brush(FFColors.White);
        t.HoverRowBackground=Brush(FFColors.Neutral50);
        t.SelectedRowBackground=Brush(FFColors.Azure50);

        //--------------------------------------------------
        // HEADER — small, gray, not heavy
        //--------------------------------------------------

        t.HeaderForeground=Brush(FFColors.Neutral700);

        //--------------------------------------------------
        // BORDERS — near invisible
        //--------------------------------------------------

        t.BorderBrush=Brush(FFColors.Neutral150);
        t.DividerBrush=Brush(FFColors.Neutral125);

        //--------------------------------------------------
        // ACCENT
        //--------------------------------------------------

        t.AccentBrush=Brush(FFColors.Azure600);
        t.AccentForeground=Brush(FFColors.White);

        //--------------------------------------------------
        // TEXT
        //--------------------------------------------------

        t.PrimaryTextColor=FFColors.Neutral800;
        t.SecondaryTextColor=FFColors.Neutral500;
        t.MutedTextColor=FFColors.Neutral400;

        //--------------------------------------------------
        // STYLE — compact, flat, tiny type
        //--------------------------------------------------

        t.CornerRadius=6;
        t.HeaderHeight=34;
        t.RowHeight=40;

        t.HeaderFontSize=12;
        t.CellFontSize=11.5;

        t.ShadowOpacity=0;

        return t;
    }

    //======================================================================
    // DEEP BLUE DARK — dark navy surfaces, azure accent
    //======================================================================

    private static FFThemeTokens DeepBlueDark()
    {
        var t = BaseDark();

        // SURFACES

        t.SurfaceBackground=Brush(FFColors.DarkSurface780);
        t.HeaderBackground=Brush(FFColors.DarkSurface850);
        t.FooterBackground=Brush(FFColors.DarkSurface850);

        // ROWS
        t.SelectedRowBorderBrush=Brush(t.AccentBrush is SolidColorBrush b ? b.Color : Colors.White); // or set explicitly per theme's accent color
        t.SelectedRowBorderThickness=3;
        t.RowBackground=Brush(FFColors.DarkSurface800);
        t.AlternateRowBackground=Brush(FFColors.DarkSurface780);
        t.HoverRowBackground=Brush(FFColors.BlueHoverDark);
        t.SelectedRowBackground=Brush(FFColors.BlueSelectedDark);

        // HEADER / TEXT

        t.HeaderForeground=Brush(FFColors.DarkTextPrimary);
        t.PrimaryTextColor=FFColors.DarkTextPrimary;
        t.SecondaryTextColor=FFColors.DarkTextSecondary;
        t.MutedTextColor=FFColors.DarkTextMuted;

        // BORDERS

        t.BorderBrush=Brush(FFColors.DarkBorder);
        t.DividerBrush=Brush(FFColors.DarkDivider);

        // ACCENT

        t.AccentBrush=Brush(FFColors.Azure500);
        t.AccentForeground=Brush(FFColors.White);

        // PAGER

        t.PagerActiveBackground=Brush(FFColors.Azure500);

        // EFFECTS

        t.CornerRadius=new CornerRadius(12);
        t.ShadowBrush=Brush(FFColors.Azure700);
        t.ShadowOpacity=.22f;
        t.ShadowRadius=20;

        return t;
    }

    //======================================================================
    // SUNSET DARK — genuine dark-with-orange theme: warm charcoal
    // surfaces, orange used as the actual background tint (hover / selected
    // rows, header underlay), not just an accent chip.
    //======================================================================

    private static FFThemeTokens SunsetDark()
    {
        var t = BaseDark();

        // Warm dark neutrals — no orange-tinted dark surface exists yet in
        // FFColors, so these are local until promoted. Same pattern the
        // original file used for one-off hex values.

        var surfaceBase = Color.FromArgb("#1C1712");
        var surfaceRaised = Color.FromArgb("#221B14");
        var surfaceHeader = Color.FromArgb("#2A2117");
        var rowAlt = Color.FromArgb("#231C15");
        var borderWarm = Color.FromArgb("#3A2E1E");
        var dividerWarm = Color.FromArgb("#2E2519");

        // SURFACES

        t.SurfaceBackground=Brush(surfaceRaised);
        t.HeaderBackground=Brush(surfaceHeader);
        t.FooterBackground=Brush(surfaceHeader);

        // ROWS — orange is the background tint here, not just the accent
        t.SelectedRowBorderBrush=Brush(t.AccentBrush is SolidColorBrush b ? b.Color : Colors.White); // or set explicitly per theme's accent color
        t.SelectedRowBorderThickness=3;
        t.RowBackground=Brush(surfaceBase);
        t.AlternateRowBackground=Brush(rowAlt);
        t.HoverRowBackground=Brush(FFColors.Orange900);
        t.SelectedRowBackground=Brush(FFColors.Orange800);

        // HEADER / TEXT

        t.HeaderForeground=Brush(FFColors.DarkTextPrimary);
        t.PrimaryTextColor=FFColors.DarkTextPrimary;
        t.SecondaryTextColor=FFColors.DarkTextSecondary;
        t.MutedTextColor=FFColors.DarkTextMuted;

        // BORDERS

        t.BorderBrush=Brush(borderWarm);
        t.DividerBrush=Brush(dividerWarm);

        // ACCENT

        t.AccentBrush=Brush(FFColors.Orange500);
        t.AccentForeground=Brush(FFColors.White);

        // PAGER

        t.PagerButtonBackground=Brush(surfaceHeader);
        t.PagerButtonHover=Brush(FFColors.Orange900);
        t.PagerActiveBackground=Brush(FFColors.Orange500);
        t.PagerActiveForeground=Brush(FFColors.White);

        // EFFECTS — warm glow shadow

        t.CornerRadius=new CornerRadius(12);
        t.ShadowBrush=Brush(FFColors.Orange700);
        t.ShadowOpacity=.28f;
        t.ShadowRadius=20;

        return t;
    }

    //======================================================================
    // PANDA — monochrome, high contrast, black header
    //======================================================================

    private static FFThemeTokens Panda()
    {
        var t = BaseLight();

        //==================================================
        // SURFACES
        //==================================================
        t.SelectedRowBorderBrush=Brush(FFColors.Azure600);
        t.SelectedRowBorderThickness=3;
        t.SurfaceBackground=Brush(FFColors.White);
        t.HeaderBackground=Brush(FFColors.White);
        t.FooterBackground=Brush(FFColors.White);

        //==================================================
        // GRID
        //==================================================

        t.RowBackground=Brush(FFColors.White);
        t.AlternateRowBackground=Brush(FFColors.Neutral25);
        t.HoverRowBackground=Brush(FFColors.Neutral100);
        t.SelectedRowBackground=Brush(FFColors.Azure100);

        //==================================================
        // HEADER
        //==================================================

        t.HeaderForeground=Brush(FFColors.Neutral900);

        //==================================================
        // BORDERS
        //==================================================

        t.BorderBrush=Brush(FFColors.Neutral250);
        t.DividerBrush=Brush(FFColors.Neutral150);

        //==================================================
        // ACCENT — shadcn blue-600
        //==================================================

        t.AccentBrush=Brush(FFColors.Azure600);
        t.AccentForeground=Brush(FFColors.White);

        //==================================================
        // TEXT
        //==================================================

        t.PrimaryTextColor=FFColors.Neutral900;
        t.SecondaryTextColor=FFColors.Neutral600;
        t.MutedTextColor=FFColors.Neutral400;

        //==================================================
        // PAGER
        //==================================================

        t.PagerActiveBackground=Brush(FFColors.Azure600);
        t.PagerActiveForeground=Brush(FFColors.White);

        //==================================================
        // STYLE — shadcn card: rounded-lg, thin border, soft shadow
        //==================================================

        t.CornerRadius=8;
        t.RowHeight=52;
        t.HeaderHeight=44;

        t.ShadowBrush=Brush(FFColors.Black);
        t.ShadowOpacity=.06f;

        return t;
    }

    //======================================================================
    // BASE LIGHT — shared defaults every light theme starts from
    //======================================================================

    private static FFThemeTokens BaseLight()
    {
        return new FFThemeTokens
        {
            // Layout

            RowHeight=56,
            HeaderHeight=46,

            HeaderFontAttributes=FontAttributes.Bold,
            HeaderFontSize=13,
            CellFontSize=13.5,
            FooterFontSize=12,

            HeaderPadding=new Thickness(18, 12),
            CellPadding=new Thickness(18, 14),
            FooterPadding=new Thickness(18, 10),

            CornerRadius=new CornerRadius(14),

            // Interaction

            HeaderHoverBackground=Brush(FFColors.Slate100),
            CellHoverBackground=Brush(FFColors.Slate50),
            SelectedCellBackground=Brush(FFColors.Azure100),

            // Pagination

            PagerButtonBackground=Brush(FFColors.White),
            PagerButtonHover=Brush(FFColors.Slate100),
            PagerActiveBackground=Brush(FFColors.Azure700),
            PagerActiveForeground=Brush(FFColors.White),

            // Status — kept semantically standard (green/amber/red/blue)
            // across all light themes regardless of accent color, so status
            // meaning stays legible no matter which theme is active.

            BadgeSuccess=Brush(FFColors.SuccessLight),
            BadgeWarning=Brush(FFColors.WarningLight),
            BadgeDanger=Brush(FFColors.ErrorLight),
            BadgeInfo=Brush(FFColors.InfoLight),

            // Overlay

            OverlayBackground=Brush(Color.FromArgb("#44000000")),

            // Shadow

            ShadowBrush=Brush(FFColors.Black),
            ShadowOpacity=.08f,
            ShadowRadius=16
        };
    }

    //======================================================================
    // BASE DARK — shared defaults every dark theme starts from
    //======================================================================

    private static FFThemeTokens BaseDark()
    {
        return new FFThemeTokens
        {
            RowHeight=50,
            HeaderHeight=46,

            HeaderFontAttributes=FontAttributes.Bold,
            HeaderFontSize=13,
            CellFontSize=13,
            FooterFontSize=12,

            HeaderPadding=new Thickness(16, 10),
            CellPadding=new Thickness(16, 10),
            FooterPadding=new Thickness(16, 8),

            CornerRadius=new CornerRadius(10),

            HeaderHoverBackground=Brush(FFColors.DarkSurface600),
            CellHoverBackground=Brush(FFColors.DarkSurface600),
            SelectedCellBackground=Brush(FFColors.BlueSelectedDark),

            PagerButtonBackground=Brush(FFColors.DarkSurface650),
            PagerButtonHover=Brush(FFColors.DarkSurface500),
            PagerActiveBackground=Brush(FFColors.Azure500),
            PagerActiveForeground=Brush(FFColors.White),

            // Status — brighter, more saturated variants read better on
            // dark surfaces than the *Light tokens used for light themes.

            BadgeSuccess=Brush(FFColors.Success),
            BadgeWarning=Brush(FFColors.Warning),
            BadgeDanger=Brush(FFColors.Error),
            BadgeInfo=Brush(FFColors.Info),

            OverlayBackground=Brush(Color.FromArgb("#66000000")),

            ShadowBrush=Brush(FFColors.Black),
            ShadowOpacity=.22f,
            ShadowRadius=18
        };
    }
}