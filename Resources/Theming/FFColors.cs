namespace EHMR.Resources.Theming;

public static class FFColors
{
    // ---------------------------------------------------------------
    // Blue family
    // ---------------------------------------------------------------
    public static readonly Color Blue700 = Color.FromArgb("#1473E6"); // Panda accent (Adobe blue)
    public static readonly Color Blue600 = Color.FromArgb("#0F6CBD"); // CoolBlueLight accent (hospital blue)
    public static readonly Color Blue500 = Color.FromArgb("#2D5BFF"); // MilkLight accent
    public static readonly Color Blue400 = Color.FromArgb("#4F9DFF"); // DeepBlueDark accent
    public static readonly Color BlueSelectedLight = Color.FromArgb("#D6E8FF"); // Panda selected
    public static readonly Color BlueHoverLight = Color.FromArgb("#EAF3FF");    // Panda hover
    public static readonly Color BlueSelectedCool = Color.FromArgb("#CFE6FB");  // CoolBlueLight selected
    public static readonly Color BlueHoverCool = Color.FromArgb("#E8F4FD");     // CoolBlueLight hover
    public static readonly Color BlueSelectedDark = Color.FromArgb("#315A93");  // DeepBlueDark selected
    public static readonly Color BlueHoverDark = Color.FromArgb("#2A3A52");     // DeepBlueDark hover

    // ---------------------------------------------------------------
    // Orange family
    // ---------------------------------------------------------------
    public static readonly Color Orange500 = Color.FromArgb("#FF8B3D"); // OrangeDark accent
    public static readonly Color Orange400 = Color.FromArgb("#FFA155"); // accent hover / lighter
    public static readonly Color OrangeSubtleLight = Color.FromArgb("#FFE3CC");
    public static readonly Color OrangeSubtleLighter = Color.FromArgb("#FFF4EA");
    public static readonly Color OrangeSelectedDark = Color.FromArgb("#4D3724"); // OrangeDark selected

    // ---------------------------------------------------------------
    // Purple family
    // ---------------------------------------------------------------
    public static readonly Color Purple500 = Color.FromArgb("#7B61FF"); // DeepPurpleDark accent
    public static readonly Color Purple400 = Color.FromArgb("#9885FF"); // accent hover
    public static readonly Color PurpleSelectedDark = Color.FromArgb("#3A3163");
    public static readonly Color PurpleHoverDark = Color.FromArgb("#242838");

    // ---------------------------------------------------------------
    // Neutral (light-theme text / surfaces / borders)
    // ---------------------------------------------------------------
    public static readonly Color Neutral900 = Color.FromArgb("#1B2838"); // primary text
    public static readonly Color Neutral800 = Color.FromArgb("#39485A"); // secondary text (denser)
    public static readonly Color Neutral600 = Color.FromArgb("#5E6C84"); // secondary text
    public static readonly Color Neutral400 = Color.FromArgb("#8B99A8"); // muted text
    public static readonly Color Neutral200 = Color.FromArgb("#D8E1EA"); // border
    public static readonly Color Neutral150 = Color.FromArgb("#E6E8EC"); // MilkLight border
    public static readonly Color Neutral125 = Color.FromArgb("#E8EDF3"); // divider
    public static readonly Color Neutral100 = Color.FromArgb("#EEF2F7"); // Panda header
    public static readonly Color Neutral75 = Color.FromArgb("#F3F8FD"); // CoolBlueLight bg
    public static readonly Color Neutral60 = Color.FromArgb("#E3F0FC"); // CoolBlueLight header
    public static readonly Color Neutral50 = Color.FromArgb("#F5F6F8"); // MilkLight header
    public static readonly Color Neutral25 = Color.FromArgb("#F6F8FB"); // Panda page background
    public static readonly Color Neutral10 = Color.FromArgb("#FCFCFD"); // MilkLight page background
    public static readonly Color White = Color.FromArgb("#FFFFFF");
    public static readonly Color Black = Color.FromArgb("#000000");

    // ---------------------------------------------------------------
    // Dark neutral (dark-theme surfaces)
    // ---------------------------------------------------------------
    public static readonly Color DarkSurface900 = Color.FromArgb("#151720"); // DeepPurpleDark header
    public static readonly Color DarkSurface850 = Color.FromArgb("#17202E"); // DeepBlueDark header
    public static readonly Color DarkSurface820 = Color.FromArgb("#181C22"); // OrangeDark header
    public static readonly Color DarkSurface800 = Color.FromArgb("#191B26"); // DeepPurpleDark background
    public static readonly Color DarkSurface780 = Color.FromArgb("#1A2433"); // DeepBlueDark background
    public static readonly Color DarkSurface760 = Color.FromArgb("#1F232A"); // OrangeDark background
    public static readonly Color DarkSurface700 = Color.FromArgb("#1F2230"); // DeepPurpleDark cards
    public static readonly Color DarkSurface650 = Color.FromArgb("#202C3D"); // DeepBlueDark cards
    public static readonly Color DarkSurface600 = Color.FromArgb("#262B33"); // OrangeDark cards
    public static readonly Color DarkSurface500 = Color.FromArgb("#2A3A52"); // DeepBlueDark hover
    public static readonly Color DarkBorder = Color.FromArgb("#2C3444");
    public static readonly Color DarkDivider = Color.FromArgb("#242A36");
    public static readonly Color DarkTextPrimary = Color.FromArgb("#EDF1F7");
    public static readonly Color DarkTextSecondary = Color.FromArgb("#9FADC2");
    public static readonly Color DarkTextMuted = Color.FromArgb("#6E7A8F");


    // ---------------------------------------------------------------
    // Semantic (status) — shared base, themes may tint the "subtle" variant
    // ---------------------------------------------------------------
    public static readonly Color Success = Color.FromArgb("#16A34A");
    public static readonly Color Warning = Color.FromArgb("#F59E0B");
    public static readonly Color Error = Color.FromArgb("#DC2626");
    public static readonly Color Info = Color.FromArgb("#0EA5E9");
}
