using Microsoft.Maui.Graphics;

namespace EHMR.Resources.Theming;
public static class FFBrushes
{
    public static SolidColorBrush Brush(Color color)
        => new(color);
}

public static class FFColors
{
    //==============================================================
    // WHITE / BLACK
    //==============================================================

    public static readonly Color White = Color.FromArgb("#FFFFFF");
    public static readonly Color Black = Color.FromArgb("#000000");


    //==============================================================
    // SLATE (Default EHMR)
    //==============================================================

    public static readonly Color Slate950 = Color.FromArgb("#0F172A");
    public static readonly Color Slate900 = Color.FromArgb("#17202E");
    public static readonly Color Slate850 = Color.FromArgb("#1D2635");
    public static readonly Color Slate800 = Color.FromArgb("#253040");
    public static readonly Color Slate700 = Color.FromArgb("#344255");
    public static readonly Color Slate600 = Color.FromArgb("#526174");
    public static readonly Color Slate500 = Color.FromArgb("#6C7B8B");
    public static readonly Color Slate400 = Color.FromArgb("#94A3B8");
    public static readonly Color Slate300 = Color.FromArgb("#CBD5E1");
    public static readonly Color Slate250 = Color.FromArgb("#DCE4EC");
    public static readonly Color Slate200 = Color.FromArgb("#E2E8F0");
    public static readonly Color Slate150 = Color.FromArgb("#EEF2F6");
    public static readonly Color Slate100 = Color.FromArgb("#F1F5F9");
    public static readonly Color Slate50 = Color.FromArgb("#F8FAFC");


    //==============================================================
    // GRAY
    //==============================================================

    public static readonly Color Gray950 = Color.FromArgb("#111827");
    public static readonly Color Gray900 = Color.FromArgb("#1F2937");
    public static readonly Color Gray800 = Color.FromArgb("#374151");
    public static readonly Color Gray700 = Color.FromArgb("#4B5563");
    public static readonly Color Gray600 = Color.FromArgb("#6B7280");
    public static readonly Color Gray500 = Color.FromArgb("#9CA3AF");
    public static readonly Color Gray400 = Color.FromArgb("#D1D5DB");
    public static readonly Color Gray300 = Color.FromArgb("#E5E7EB");
    public static readonly Color Gray200 = Color.FromArgb("#F3F4F6");
    public static readonly Color Gray100 = Color.FromArgb("#F9FAFB");


    //==============================================================
    // AZURE (Spark / Medical)
    //==============================================================

    public static readonly Color Azure900 = Color.FromArgb("#0B3A66");
    public static readonly Color Azure800 = Color.FromArgb("#0D5CA6");
    public static readonly Color Azure700 = Color.FromArgb("#1464C8");
    public static readonly Color Azure600 = Color.FromArgb("#1976D2");
    public static readonly Color Azure500 = Color.FromArgb("#2196F3");
    public static readonly Color Azure400 = Color.FromArgb("#64B5F6");
    public static readonly Color Azure300 = Color.FromArgb("#90CAF9");
    public static readonly Color Azure200 = Color.FromArgb("#BBDEFB");
    public static readonly Color Azure150 = Color.FromArgb("#D7E8FA");
    public static readonly Color Azure100 = Color.FromArgb("#E3F2FD");
    public static readonly Color Azure50 = Color.FromArgb("#F1F8FF");
    public static readonly Color Azure25 = Color.FromArgb("#F8FBFF");


    //==============================================================
    // EMERALD
    //==============================================================

    public static readonly Color Emerald900 = Color.FromArgb("#065F46");
    public static readonly Color Emerald800 = Color.FromArgb("#047857");
    public static readonly Color Emerald700 = Color.FromArgb("#059669");
    public static readonly Color Emerald600 = Color.FromArgb("#10B981");
    public static readonly Color Emerald500 = Color.FromArgb("#34D399");
    public static readonly Color Emerald400 = Color.FromArgb("#6EE7B7");
    public static readonly Color Emerald300 = Color.FromArgb("#A7F3D0");
    public static readonly Color Emerald200 = Color.FromArgb("#D1FAE5");
    public static readonly Color Emerald100 = Color.FromArgb("#ECFDF5");


    //==============================================================
    // ORANGE
    //==============================================================

    public static readonly Color Orange900 = Color.FromArgb("#9A3412");
    public static readonly Color Orange800 = Color.FromArgb("#C2410C");
    public static readonly Color Orange700 = Color.FromArgb("#EA580C");
    public static readonly Color Orange600 = Color.FromArgb("#F97316");

    // Hospital accent compatible
    public static readonly Color Orange500 = Color.FromArgb("#FF8A00");

    public static readonly Color Orange400 = Color.FromArgb("#FDBA74");
    public static readonly Color Orange300 = Color.FromArgb("#FED7AA");
    public static readonly Color Orange200 = Color.FromArgb("#FFEDD5");
    public static readonly Color Orange100 = Color.FromArgb("#FFF7ED");


    //==============================================================
    // PURPLE
    //==============================================================

    public static readonly Color Purple900 = Color.FromArgb("#4C1D95");
    public static readonly Color Purple800 = Color.FromArgb("#5B21B6");
    public static readonly Color Purple700 = Color.FromArgb("#6D28D9");
    public static readonly Color Purple600 = Color.FromArgb("#7C3AED");
    public static readonly Color Purple500 = Color.FromArgb("#8B5CF6");
    public static readonly Color Purple400 = Color.FromArgb("#A78BFA");
    public static readonly Color Purple300 = Color.FromArgb("#C4B5FD");
    public static readonly Color Purple200 = Color.FromArgb("#DDD6FE");
    public static readonly Color Purple100 = Color.FromArgb("#F3E8FF");


    //==============================================================
    // ROSE
    //==============================================================

    public static readonly Color Rose900 = Color.FromArgb("#881337");
    public static readonly Color Rose800 = Color.FromArgb("#9F1239");
    public static readonly Color Rose700 = Color.FromArgb("#BE123C");
    public static readonly Color Rose600 = Color.FromArgb("#E11D48");
    public static readonly Color Rose500 = Color.FromArgb("#F43F5E");
    public static readonly Color Rose400 = Color.FromArgb("#FB7185");
    public static readonly Color Rose300 = Color.FromArgb("#FDA4AF");
    public static readonly Color Rose200 = Color.FromArgb("#FECDD3");
    public static readonly Color Rose100 = Color.FromArgb("#FFF1F2");


    //==============================================================
    // SEMANTIC
    //==============================================================

    public static readonly Color Success = Color.FromArgb("#16A34A");
    public static readonly Color SuccessLight = Color.FromArgb("#DCFCE7");

    public static readonly Color Warning = Color.FromArgb("#D97706");
    public static readonly Color WarningLight = Color.FromArgb("#FEF3C7");

    public static readonly Color Error = Color.FromArgb("#DC2626");
    public static readonly Color ErrorLight = Color.FromArgb("#FEE2E2");

    public static readonly Color Info = Color.FromArgb("#0284C7");
    public static readonly Color InfoLight = Color.FromArgb("#E0F2FE");


    //==============================================================
    // DARK SURFACE
    //==============================================================

    public static readonly Color SurfaceDark950 = Color.FromArgb("#080D18");
    public static readonly Color SurfaceDark900 = Color.FromArgb("#111827");
    public static readonly Color SurfaceDark800 = Color.FromArgb("#1F2937");
    public static readonly Color SurfaceDark700 = Color.FromArgb("#273449");
    public static readonly Color SurfaceDark600 = Color.FromArgb("#334155");
    public static readonly Color SurfaceDark500 = Color.FromArgb("#475569");


    public static readonly Color DarkSurface950 = SurfaceDark950;
    public static readonly Color DarkSurface900 = SurfaceDark900;
    public static readonly Color DarkSurface850 = Color.FromArgb("#172235");
    public static readonly Color DarkSurface800 = SurfaceDark800;
    public static readonly Color DarkSurface780 = Color.FromArgb("#243247");
    public static readonly Color DarkSurface700 = SurfaceDark700;
    public static readonly Color DarkSurface650 = Color.FromArgb("#2D3B50");
    public static readonly Color DarkSurface600 = SurfaceDark600;
    public static readonly Color DarkSurface500 = SurfaceDark500;


    public static readonly Color DarkBorder =
        Color.FromArgb("#334155");

    public static readonly Color DarkDivider =
        Color.FromArgb("#293548");

    public static readonly Color DarkTextPrimary =
        Color.FromArgb("#F8FAFC");

    public static readonly Color DarkTextSecondary =
        Color.FromArgb("#CBD5E1");

    public static readonly Color DarkTextMuted =
        Color.FromArgb("#94A3B8");


    //==============================================================
    // CLINICAL
    //==============================================================

    public static readonly Color ClinicalBlue = Color.FromArgb("#1976D2");
    public static readonly Color ClinicalGreen = Color.FromArgb("#2E7D32");
    public static readonly Color ClinicalAmber = Color.FromArgb("#F9A825");
    public static readonly Color ClinicalRed = Color.FromArgb("#C62828");
    public static readonly Color ClinicalPurple = Color.FromArgb("#6A1B9A");
    public static readonly Color ClinicalTeal = Color.FromArgb("#00897B");


    //==============================================================
    // NEUTRAL
    //==============================================================

    public static readonly Color Neutral900 = Color.FromArgb("#111827");
    public static readonly Color Neutral800 = Color.FromArgb("#1F2937");
    public static readonly Color Neutral700 = Color.FromArgb("#374151");
    public static readonly Color Neutral600 = Color.FromArgb("#4B5563");
    public static readonly Color Neutral500 = Color.FromArgb("#6B7280");
    public static readonly Color Neutral400 = Color.FromArgb("#9CA3AF");

    public static readonly Color Neutral300 = Color.FromArgb("#D1D5DB");
    public static readonly Color Neutral250 = Color.FromArgb("#E5E7EB");
    public static readonly Color Neutral200 = Color.FromArgb("#E9ECEF");

    public static readonly Color Neutral150 = Color.FromArgb("#EEF0F3");
    public static readonly Color Neutral125 = Color.FromArgb("#F3F4F6");

    public static readonly Color Neutral100 = Color.FromArgb("#F3F4F6");
    public static readonly Color Neutral50 = Color.FromArgb("#F9FAFB");
    public static readonly Color Neutral25 = Color.FromArgb("#FCFCFD");
    public static readonly Color Neutral10 = Color.FromArgb("#FFFFFF");


    //==============================================================
    // SPECIAL
    //==============================================================

    public static readonly Color BlueHoverDark =
        Color.FromArgb("#263B59");

    public static readonly Color BlueSelectedDark =
        Color.FromArgb("#164B85");


    public static readonly Color MedicalGreen =
        Color.FromArgb("#22C55E");

    public static readonly Color MedicalGreenSoft =
        Color.FromArgb("#DCFCE7");


    public static readonly Color MedicalBlue =
        Color.FromArgb("#2563EB");

    public static readonly Color MedicalBlueSoft =
        Color.FromArgb("#DBEAFE");


    public static readonly Color MedicalAmber =
        Color.FromArgb("#F59E0B");

    public static readonly Color MedicalAmberSoft =
        Color.FromArgb("#FEF3C7");


    public static readonly Color MedicalRed =
        Color.FromArgb("#DC2626");

    public static readonly Color MedicalRedSoft =
        Color.FromArgb("#FEE2E2");
}