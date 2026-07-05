using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;

namespace EHMR.Resources.Theming
{
    
    public class FFThemeTokens
    {
        public Brush HeaderHoverBackground
        {
            get; set;
        }

        public Brush CellHoverBackground
        {
            get; set;
        }

        public Brush SelectedCellBackground
        {
            get; set;
        }

        public Brush PagerButtonBackground
        {
            get; set;
        }

        public Brush PagerButtonHover
        {
            get; set;
        }

        public Brush PagerActiveBackground
        {
            get; set;
        }

        public Brush PagerActiveForeground
        {
            get; set;
        }

        public Brush BadgeSuccess
        {
            get; set;
        }

        public Brush BadgeWarning
        {
            get; set;
        }

        public Brush BadgeDanger
        {
            get; set;
        }

        public Brush BadgeInfo
        {
            get; set;
        }

        public double HeaderFontSize
        {
            get; set;
        }

        public double CellFontSize
        {
            get; set;
        }

        public Thickness CellPadding
        {
            get; set;
        }

        public Thickness HeaderPadding
        {
            get; set;
        }
        public Brush SurfaceBackground { get; set; } = null!;
        public Brush HeaderBackground { get; set; } = null!;
        public Brush FooterBackground { get; set; } = null!;
        public Brush HeaderForeground { get; set; } = null!;
        public Brush RowBackground { get; set; } = null!;
        public Brush AlternateRowBackground { get; set; } = null!;
        public Brush HoverRowBackground { get; set; } = null!;
        public Brush SelectedRowBackground { get; set; } = null!;
        public Brush BorderBrush { get; set; } = null!;
        public Brush DividerBrush { get; set; } = null!;
        public Brush AccentBrush { get; set; } = null!;
        public Brush AccentForeground { get; set; } = null!;

        public Color PrimaryTextColor { get; set; } = null!;
        public Color SecondaryTextColor { get; set; } = null!;
        public Color MutedTextColor { get; set; } = null!;

        public double CornerRadius
        {
            get; set;
        }
        public double RowHeight
        {
            get; set;
        }
        public double HeaderHeight
        {
            get; set;
        }
        public Brush OverlayBackground { get; set; } = new SolidColorBrush(Color.FromArgb("#66000000"));
        public Brush ShadowBrush { get; set; } = new SolidColorBrush(Colors.Black);
        public float ShadowOpacity { get; set; } = 0.15f;
    }


}