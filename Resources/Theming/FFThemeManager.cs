using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;

namespace EHMR.Resources.Theming
{
    public static class FFThemeManager
    {
        private static readonly Dictionary<FFThemeVariant, FFThemeTokens> Registry = new();
        private static readonly object LockObject = new();

        public static FFThemeVariant CurrentVariant { get; private set; } = FFThemeVariant.DeepBlueDark;
        public static FFThemeTokens Current { get; private set; } = null!;

        public static event EventHandler<FFThemeTokens>? ThemeChanged;

        static FFThemeManager()
        {
            FFThemeManager.ResetToDefaults();
            ApplyTheme(FFThemeVariant.DeepBlueDark);
        }

        public static void ApplyTheme(FFThemeVariant variant)
        {
            lock(LockObject)
            {
                var tokens = Registry.TryGetValue(variant, out var t) ? t : Registry[FFThemeVariant.DeepBlueDark];
                CurrentVariant=variant;
                Current=tokens;

                PushToApplicationResources(tokens);
                ThemeChanged?.Invoke(null, tokens);
            }
        }

        private static void PushToApplicationResources(FFThemeTokens t)
        {
            var res = Application.Current?.Resources;
            if(res is null) return;

            res["FFSurfaceBackgroundBrush"]=t.SurfaceBackground;
            res["FFHeaderBackgroundBrush"]=t.HeaderBackground;
            res["FFFooterBackgroundBrush"]=t.FooterBackground;
            res["FFHeaderForegroundBrush"]=t.HeaderForeground;
            res["FFRowBackgroundBrush"]=t.RowBackground;
            res["FFAlternateRowBackgroundBrush"]=t.AlternateRowBackground;
            res["FFHoverRowBackgroundBrush"]=t.HoverRowBackground;
            res["FFSelectedRowBackgroundBrush"]=t.SelectedRowBackground;
            res["FFBorderBrush"]=t.BorderBrush;
            res["FFDividerBrush"]=t.DividerBrush;
            res["FFAccentBrush"]=t.AccentBrush;
            res["FFAccentForegroundBrush"]=t.AccentForeground;

            res["FFPrimaryTextColor"]=t.PrimaryTextColor;
            res["FFSecondaryTextColor"]=t.SecondaryTextColor;
            res["FFMutedTextColor"]=t.MutedTextColor;
            res["FFOverlayBackgroundBrush"]=t.OverlayBackground;
            res["FFShadowBrush"]=t.ShadowBrush;
            res["FFShadowOpacity"]=t.ShadowOpacity;
            res["FFCornerRadius"]=t.CornerRadius;
            res["FFRowHeight"]=t.RowHeight;
            res["FFHeaderHeight"]=t.HeaderHeight;
        }

        public static FFThemeTokens Get(FFThemeVariant variant)
        {
            lock(LockObject)
            {
                return Registry.TryGetValue(variant, out var tokens) ? tokens : Registry[FFThemeVariant.DeepBlueDark];
            }
        }

        public static void Set(FFThemeVariant variant, FFThemeTokens tokens)
        {
            lock(LockObject)
            {
                Registry[variant]=tokens;
            }
        }

        public static void ResetToDefaults()
        {
            lock(LockObject)
            {
                // -----------------------------------------------------------
                // 1. Deep Blue Dark — Material Dark Blue, elevation-based surfaces
                // -----------------------------------------------------------
                Registry[FFThemeVariant.DeepBlueDark]=new()
                {
                    SurfaceBackground=new SolidColorBrush(Color.FromArgb("#0F1A2E")),
                    HeaderBackground=new SolidColorBrush(Color.FromArgb("#16223B")),
                    FooterBackground=new SolidColorBrush(Color.FromArgb("#16223B")),

                    HeaderForeground=new SolidColorBrush(Color.FromArgb("#A9B8D4")),

                    RowBackground=new SolidColorBrush(Color.FromArgb("#182541")),
                    AlternateRowBackground=new SolidColorBrush(Color.FromArgb("#1C2B4A")),
                    HoverRowBackground=new SolidColorBrush(Color.FromArgb("#213259")),
                    SelectedRowBackground=new SolidColorBrush(Color.FromArgb("#2A3F6E")),

                    BorderBrush=new SolidColorBrush(Color.FromArgb("#26375C")),
                    DividerBrush=new SolidColorBrush(Color.FromArgb("#1E2E4E")),

                    AccentBrush=new SolidColorBrush(Color.FromArgb("#5B8DEF")),
                    AccentForeground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),

                    PrimaryTextColor=Color.FromArgb("#EAF0FB"),
                    SecondaryTextColor=Color.FromArgb("#A9B8D4"),
                    MutedTextColor=Color.FromArgb("#6F81A6"),

                    RowHeight=46,
                    HeaderHeight=40,
                    CornerRadius=10
                };

                // -----------------------------------------------------------
                // 2. Classic — professional black-on-white ledger grid, no color, no radius
                // -----------------------------------------------------------
                Registry[FFThemeVariant.Classic]=new()
                {
                    SurfaceBackground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),
                    HeaderBackground=new SolidColorBrush(Color.FromArgb("#F2F2F0")),
                    FooterBackground=new SolidColorBrush(Color.FromArgb("#F2F2F0")),

                    HeaderForeground=new SolidColorBrush(Color.FromArgb("#1A1A1A")),

                    RowBackground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),
                    AlternateRowBackground=new SolidColorBrush(Color.FromArgb("#F7F7F6")),
                    HoverRowBackground=new SolidColorBrush(Color.FromArgb("#ECECEA")),
                    SelectedRowBackground=new SolidColorBrush(Color.FromArgb("#E4E4E1")),

                    BorderBrush=new SolidColorBrush(Color.FromArgb("#2B2B2B")),
                    DividerBrush=new SolidColorBrush(Color.FromArgb("#D8D8D5")),

                    AccentBrush=new SolidColorBrush(Color.FromArgb("#1F2937")),
                    AccentForeground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),

                    PrimaryTextColor=Color.FromArgb("#141414"),
                    SecondaryTextColor=Color.FromArgb("#5A5A57"),
                    MutedTextColor=Color.FromArgb("#8C8C88"),

                    RowHeight=48,
                    HeaderHeight=44,
                    CornerRadius=0
                };

                // -----------------------------------------------------------
                // 2b. Sparked — light, airy "electric" theme.
                //     Contrast fix: HeaderForeground and PrimaryTextColor were
                //     too close in luminance to the near-white surfaces
                //     (#6D7A88 / #606D79 on #F8F9FB / #FFFFFF ~= 2.5-3.7:1,
                //     below the 4.5:1 AA target). Darkened both to a deep
                //     slate. AccentForeground was white-on-light-cyan
                //     (#FFFFFF on #55C7EA), which is also low-contrast since
                //     the accent itself is light — switched to a dark teal so
                //     the "current page" badge and any accent chip stay
                //     legible. Rule of thumb kept consistent across every
                //     variant below: dark background -> light text,
                //     light background -> dark text.
                // -----------------------------------------------------------
                Registry[FFThemeVariant.Sparked]=new()
                {
                    // Container
                    SurfaceBackground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),

                    // Header/Footer
                    HeaderBackground=new SolidColorBrush(Color.FromArgb("#F8F9FB")),
                    FooterBackground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),

                    HeaderForeground=new SolidColorBrush(Color.FromArgb("#33424F")),

                    // Rows
                    RowBackground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),
                    AlternateRowBackground=new SolidColorBrush(Color.FromArgb("#FCFCFD")),
                    HoverRowBackground=new SolidColorBrush(Color.FromArgb("#EAF6FC")),
                    SelectedRowBackground=new SolidColorBrush(Color.FromArgb("#D8F0FA")),

                    // Lines
                    BorderBrush=new SolidColorBrush(Color.FromArgb("#DCE3EA")),
                    DividerBrush=new SolidColorBrush(Color.FromArgb("#EDF1F5")),

                    // Accent
                    AccentBrush=new SolidColorBrush(Color.FromArgb("#2FB6DE")),
                    AccentForeground=new SolidColorBrush(Color.FromArgb("#0B2C36")),
                    OverlayBackground=new SolidColorBrush(Color.FromArgb("#4D0B2C36")),
                    ShadowBrush=new SolidColorBrush(Color.FromArgb("#2FB6DE")),
                    ShadowOpacity=0.10f,
                    PrimaryTextColor=Color.FromArgb("#2C3A45"),
                    SecondaryTextColor=Color.FromArgb("#7C8894"),
                    MutedTextColor=Color.FromArgb("#AEB8C1"),

                    RowHeight=48,
                    HeaderHeight=46,

                    CornerRadius=10
                };

                // -----------------------------------------------------------
                // 3. Orange Dark — Material Deep Orange, warm charcoal surfaces
                // -----------------------------------------------------------
                Registry[FFThemeVariant.OrangeDark]=new()
                {
                    SurfaceBackground=new SolidColorBrush(Color.FromArgb("#1C1815")),
                    HeaderBackground=new SolidColorBrush(Color.FromArgb("#241F1B")),
                    FooterBackground=new SolidColorBrush(Color.FromArgb("#241F1B")),

                    HeaderForeground=new SolidColorBrush(Color.FromArgb("#D8C7BB")),

                    RowBackground=new SolidColorBrush(Color.FromArgb("#221D19")),
                    AlternateRowBackground=new SolidColorBrush(Color.FromArgb("#28221D")),
                    HoverRowBackground=new SolidColorBrush(Color.FromArgb("#332A22")),
                    SelectedRowBackground=new SolidColorBrush(Color.FromArgb("#4A3020")),

                    BorderBrush=new SolidColorBrush(Color.FromArgb("#3A322A")),
                    DividerBrush=new SolidColorBrush(Color.FromArgb("#2C2620")),

                    AccentBrush=new SolidColorBrush(Color.FromArgb("#FF7A3D")),
                    AccentForeground=new SolidColorBrush(Color.FromArgb("#1C1815")),

                    PrimaryTextColor=Color.FromArgb("#F5EDE7"),
                    SecondaryTextColor=Color.FromArgb("#D8C7BB"),
                    MutedTextColor=Color.FromArgb("#9C897B"),

                    RowHeight=46,
                    HeaderHeight=40,
                    CornerRadius=10
                };

                // -----------------------------------------------------------
                // 4. Deep Purple Dark — Material Deep Purple, desaturated for professional feel
                // -----------------------------------------------------------
                Registry[FFThemeVariant.DeepPurpleDark]=new()
                {
                    SurfaceBackground=new SolidColorBrush(Color.FromArgb("#181425")),
                    HeaderBackground=new SolidColorBrush(Color.FromArgb("#201A31")),
                    FooterBackground=new SolidColorBrush(Color.FromArgb("#201A31")),

                    HeaderForeground=new SolidColorBrush(Color.FromArgb("#BDB2D6")),

                    RowBackground=new SolidColorBrush(Color.FromArgb("#1D1830")),
                    AlternateRowBackground=new SolidColorBrush(Color.FromArgb("#221C38")),
                    HoverRowBackground=new SolidColorBrush(Color.FromArgb("#2A2244")),
                    SelectedRowBackground=new SolidColorBrush(Color.FromArgb("#3B2E60")),

                    BorderBrush=new SolidColorBrush(Color.FromArgb("#332A4E")),
                    DividerBrush=new SolidColorBrush(Color.FromArgb("#271F3D")),

                    AccentBrush=new SolidColorBrush(Color.FromArgb("#9575DE")),
                    AccentForeground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),

                    PrimaryTextColor=Color.FromArgb("#EDE9F7"),
                    SecondaryTextColor=Color.FromArgb("#BDB2D6"),
                    MutedTextColor=Color.FromArgb("#8578A3"),

                    RowHeight=46,
                    HeaderHeight=40,
                    CornerRadius=10
                };

                // -----------------------------------------------------------
                // 5. Panda — true black & white identity: black header band, white body,
                //    pure-black accent instead of borrowed blue. Rounded corners keep it
                //    from feeling cold — the "cute but bold" panda contrast.
                // -----------------------------------------------------------
                Registry[FFThemeVariant.Panda]=new()
                {
                    SurfaceBackground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),
                    HeaderBackground=new SolidColorBrush(Color.FromArgb("#1A1A1A")),
                    FooterBackground=new SolidColorBrush(Color.FromArgb("#F5F5F5")),

                    HeaderForeground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),

                    RowBackground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),
                    AlternateRowBackground=new SolidColorBrush(Color.FromArgb("#F5F5F5")),
                    HoverRowBackground=new SolidColorBrush(Color.FromArgb("#EAEAEA")),
                    SelectedRowBackground=new SolidColorBrush(Color.FromArgb("#D9D9D9")),

                    BorderBrush=new SolidColorBrush(Color.FromArgb("#1A1A1A")),
                    DividerBrush=new SolidColorBrush(Color.FromArgb("#E0E0E0")),

                    AccentBrush=new SolidColorBrush(Color.FromArgb("#000000")),
                    AccentForeground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),

                    PrimaryTextColor=Color.FromArgb("#0D0D0D"),
                    SecondaryTextColor=Color.FromArgb("#595959"),
                    MutedTextColor=Color.FromArgb("#9E9E9E"),

                    RowHeight=46,
                    HeaderHeight=42,
                    CornerRadius=12
                };

                // -----------------------------------------------------------
                // 6. Cool Blue Light — Material Blue tonal light surfaces, clinical contrast
                // -----------------------------------------------------------
                Registry[FFThemeVariant.CoolBlueLight]=new()
                {
                    SurfaceBackground=new SolidColorBrush(Color.FromArgb("#F4F8FD")),
                    HeaderBackground=new SolidColorBrush(Color.FromArgb("#E3EDFA")),
                    FooterBackground=new SolidColorBrush(Color.FromArgb("#E3EDFA")),

                    HeaderForeground=new SolidColorBrush(Color.FromArgb("#25466B")),

                    RowBackground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),
                    AlternateRowBackground=new SolidColorBrush(Color.FromArgb("#F6FAFE")),
                    HoverRowBackground=new SolidColorBrush(Color.FromArgb("#DDEBFA")),
                    SelectedRowBackground=new SolidColorBrush(Color.FromArgb("#BFDBF5")),

                    BorderBrush=new SolidColorBrush(Color.FromArgb("#A9C7E5")),
                    DividerBrush=new SolidColorBrush(Color.FromArgb("#E4EEF8")),

                    AccentBrush=new SolidColorBrush(Color.FromArgb("#1D6FD1")),
                    AccentForeground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),

                    PrimaryTextColor=Color.FromArgb("#132A42"),
                    SecondaryTextColor=Color.FromArgb("#3E5C7D"),
                    MutedTextColor=Color.FromArgb("#7F9AB5"),

                    RowHeight=46,
                    HeaderHeight=40,
                    CornerRadius=8
                };

                // -----------------------------------------------------------
                // 7. Milk — warm cream + caramel café palette, not just pale white.
                //    Coffee-brown text instead of black, toffee accent for identity.
                // -----------------------------------------------------------
                Registry[FFThemeVariant.MilkLight]=new()
                {
                    SurfaceBackground=new SolidColorBrush(Color.FromArgb("#FFFCF7")),
                    HeaderBackground=new SolidColorBrush(Color.FromArgb("#F7EFE3")),
                    FooterBackground=new SolidColorBrush(Color.FromArgb("#F7EFE3")),

                    HeaderForeground=new SolidColorBrush(Color.FromArgb("#6B4F3B")),

                    RowBackground=new SolidColorBrush(Color.FromArgb("#FFFCF7")),
                    AlternateRowBackground=new SolidColorBrush(Color.FromArgb("#FBF3E7")),
                    HoverRowBackground=new SolidColorBrush(Color.FromArgb("#F3E6D3")),
                    SelectedRowBackground=new SolidColorBrush(Color.FromArgb("#EAD9BE")),

                    BorderBrush=new SolidColorBrush(Color.FromArgb("#C9A97E")),
                    DividerBrush=new SolidColorBrush(Color.FromArgb("#EFE2CE")),

                    AccentBrush=new SolidColorBrush(Color.FromArgb("#C08552")),
                    AccentForeground=new SolidColorBrush(Color.FromArgb("#FFFFFF")),

                    PrimaryTextColor=Color.FromArgb("#3A2B1E"),
                    SecondaryTextColor=Color.FromArgb("#7A6350"),
                    MutedTextColor=Color.FromArgb("#AC957E"),

                    RowHeight=46,
                    HeaderHeight=40,
                    CornerRadius=14
                };
            }
        }
    }
}