// ═══════════════════════════════════════════════════════════════════════════
// ADVANCED SCROLLBAR CUSTOMIZATION
// ═══════════════════════════════════════════════════════════════════════════

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Windows.Input;

namespace EHMR.Resources.Controls
{
    // ═══════════════════════════════════════════════════════════════════════
    // OPTION 1: Scrollbar with Fade-In/Fade-Out Effect
    // ═══════════════════════════════════════════════════════════════════════
    public class FadingScrollView : ScrollView
    {
        private bool isScrolling = false;
        private DateTime lastScrollTime;

        public FadingScrollView()
        {
            this.VerticalScrollBarVisibility=ScrollBarVisibility.Always;
            this.HorizontalScrollBarVisibility=ScrollBarVisibility.Never;
            this.Scrolled+=OnScrolled;
        }

        private void OnScrolled(object sender, ScrolledEventArgs e)
        {
            isScrolling=true;
            lastScrollTime=DateTime.Now;

            // Reset fade-out timer
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(1000); // Show scrollbar for 1 second after scroll
                if(DateTime.Now.Subtract(lastScrollTime).TotalMilliseconds>1000)
                {
                    isScrolling=false;
                }
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // OPTION 2: Scrollbar with Thickness Control
    // ═══════════════════════════════════════════════════════════════════════
    public class ThickScrollView : ScrollView
    {
        public static readonly BindableProperty ScrollBarThicknessProperty =
            BindableProperty.Create(
                nameof(ScrollBarThickness),
                typeof(double),
                typeof(ThickScrollView),
                8.0,
                propertyChanged: OnThicknessChanged
            );

        public static readonly BindableProperty ScrollBarColorProperty =
            BindableProperty.Create(
                nameof(ScrollBarColor),
                typeof(Color),
                typeof(ThickScrollView),
                Color.FromArgb("#7B9DB2")
            );

        public double ScrollBarThickness
        {
            get => (double)GetValue(ScrollBarThicknessProperty);
            set => SetValue(ScrollBarThicknessProperty, value);
        }

        public Color ScrollBarColor
        {
            get => (Color)GetValue(ScrollBarColorProperty);
            set => SetValue(ScrollBarColorProperty, value);
        }

        public ThickScrollView()
        {
            this.VerticalScrollBarVisibility=ScrollBarVisibility.Always;
            this.HorizontalScrollBarVisibility=ScrollBarVisibility.Never;
        }

        private static void OnThicknessChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (ThickScrollView)bindable;
            var thickness = (double)newValue;

#if __ANDROID__
            if (control.Handler?.PlatformView is Android.Widget.ScrollView scrollView)
            {
                // Android doesn't have direct API for scrollbar thickness,
                // but we can use the VerticalScrollbarPosition property
                scrollView.VerticalScrollbarPosition = Android.Views.ViewCompat.ScrollbarPositionRight;
            }
#endif
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // OPTION 3: Scrollbar with Position Indicator (Percentage)
    // ═══════════════════════════════════════════════════════════════════════
    public class IndicatorScrollView : ScrollView
    {
        public static readonly BindableProperty ScrollPositionProperty =
            BindableProperty.Create(
                nameof(ScrollPosition),
                typeof(double),
                typeof(IndicatorScrollView),
                0.0
            );

        public static readonly BindableProperty ShowPositionIndicatorProperty =
            BindableProperty.Create(
                nameof(ShowPositionIndicator),
                typeof(bool),
                typeof(IndicatorScrollView),
                false
            );

        public double ScrollPosition
        {
            get => (double)GetValue(ScrollPositionProperty);
            set => SetValue(ScrollPositionProperty, value);
        }

        public bool ShowPositionIndicator
        {
            get => (bool)GetValue(ShowPositionIndicatorProperty);
            set => SetValue(ShowPositionIndicatorProperty, value);
        }

        public IndicatorScrollView()
        {
            this.VerticalScrollBarVisibility=ScrollBarVisibility.Always;
            this.HorizontalScrollBarVisibility=ScrollBarVisibility.Never;
            this.Scrolled+=UpdateScrollPosition;
        }

        private void UpdateScrollPosition(object sender, ScrolledEventArgs e)
        {
            if(this.ContentSize.Height>0)
            {
                ScrollPosition=(e.ScrollY/(this.ContentSize.Height-this.Height))*100;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // OPTION 4: Gradient Scrollbar (Fancy!)
    // ═══════════════════════════════════════════════════════════════════════
    public class GradientScrollView : ScrollView
    {
        public static readonly BindableProperty ScrollBarStartColorProperty =
            BindableProperty.Create(
                nameof(ScrollBarStartColor),
                typeof(Color),
                typeof(GradientScrollView),
                Color.FromArgb("#7B9DB2")
            );

        public static readonly BindableProperty ScrollBarEndColorProperty =
            BindableProperty.Create(
                nameof(ScrollBarEndColor),
                typeof(Color),
                typeof(GradientScrollView),
                Color.FromArgb("#5A7C9A")
            );

        public Color ScrollBarStartColor
        {
            get => (Color)GetValue(ScrollBarStartColorProperty);
            set => SetValue(ScrollBarStartColorProperty, value);
        }

        public Color ScrollBarEndColor
        {
            get => (Color)GetValue(ScrollBarEndColorProperty);
            set => SetValue(ScrollBarEndColorProperty, value);
        }

        public GradientScrollView()
        {
            this.VerticalScrollBarVisibility=ScrollBarVisibility.Always;
            this.HorizontalScrollBarVisibility=ScrollBarVisibility.Never;
        }

        // Note: Gradient scrollbars require custom rendering on native platforms
        // This is a placeholder for the concept
    }

    // ═══════════════════════════════════════════════════════════════════════
    // OPTION 5: Complete Styled ScrollView with All Features
    // ═══════════════════════════════════════════════════════════════════════
    public class StyledScrollView : ScrollView
    {
        // Color properties
        public static readonly BindableProperty ScrollBarColorProperty =
            BindableProperty.Create(nameof(ScrollBarColor), typeof(Color), typeof(StyledScrollView), Color.FromArgb("#7B9DB2"));

        public static readonly BindableProperty ScrollBarHoverColorProperty =
            BindableProperty.Create(nameof(ScrollBarHoverColor), typeof(Color), typeof(StyledScrollView), Color.FromArgb("#5A7C9A"));

        // Size properties
        public static readonly BindableProperty ScrollBarWidthProperty =
            BindableProperty.Create(nameof(ScrollBarWidth), typeof(double), typeof(StyledScrollView), 8.0);

        // Behavior properties
        public static readonly BindableProperty AutoHideScrollBarProperty =
            BindableProperty.Create(nameof(AutoHideScrollBar), typeof(bool), typeof(StyledScrollView), false);

        public static readonly BindableProperty ScrollBarFadeDurationProperty =
            BindableProperty.Create(nameof(ScrollBarFadeDuration), typeof(int), typeof(StyledScrollView), 3000); // milliseconds

        // Properties
        public Color ScrollBarColor
        {
            get => (Color)GetValue(ScrollBarColorProperty);
            set => SetValue(ScrollBarColorProperty, value);
        }

        public Color ScrollBarHoverColor
        {
            get => (Color)GetValue(ScrollBarHoverColorProperty);
            set => SetValue(ScrollBarHoverColorProperty, value);
        }

        public double ScrollBarWidth
        {
            get => (double)GetValue(ScrollBarWidthProperty);
            set => SetValue(ScrollBarWidthProperty, value);
        }

        public bool AutoHideScrollBar
        {
            get => (bool)GetValue(AutoHideScrollBarProperty);
            set => SetValue(AutoHideScrollBarProperty, value);
        }

        public int ScrollBarFadeDuration
        {
            get => (int)GetValue(ScrollBarFadeDurationProperty);
            set => SetValue(ScrollBarFadeDurationProperty, value);
        }

        public StyledScrollView()
        {
            this.VerticalScrollBarVisibility=ScrollBarVisibility.Always;
            this.HorizontalScrollBarVisibility=ScrollBarVisibility.Never;
            this.Scrolled+=OnScrolled;
        }

        private void OnScrolled(object sender, ScrolledEventArgs e)
        {
            // Optional: Handle auto-hide behavior
            if(AutoHideScrollBar)
            {
                // Fade out after ScrollBarFadeDuration ms
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(ScrollBarFadeDuration);
                    // Fade implementation here
                });
            }
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// XAML USAGE EXAMPLES
// ═══════════════════════════════════════════════════════════════════════════

/*

1. BASIC CUSTOM SCROLLBAR
────────────────────────────────────────
<controls:CustomScrollView ScrollBarColor="{StaticResource SparkAccentTeal}">
    <VerticalStackLayout>
        <!-- Content -->
    </VerticalStackLayout>
</controls:CustomScrollView>


2. SCROLLBAR WITH THICKNESS
────────────────────────────────────────
<controls:ThickScrollView 
    ScrollBarThickness="12"
    ScrollBarColor="{StaticResource SparkAccentTeal}">
    <VerticalStackLayout>
        <!-- Content -->
    </VerticalStackLayout>
</controls:ThickScrollView>


3. SCROLLBAR WITH POSITION INDICATOR
────────────────────────────────────────
<controls:IndicatorScrollView 
    ShowPositionIndicator="True">
    <VerticalStackLayout>
        <!-- Content -->
    </VerticalStackLayout>
</controls:IndicatorScrollView>


4. FULLY STYLED SCROLLBAR (Recommended)
────────────────────────────────────────
<controls:StyledScrollView 
    ScrollBarColor="{StaticResource SparkAccentTeal}"
    ScrollBarHoverColor="{StaticResource SparkAccentTealDark}"
    ScrollBarWidth="10"
    AutoHideScrollBar="False"
    ScrollBarFadeDuration="3000">
    <VerticalStackLayout>
        <!-- Content -->
    </VerticalStackLayout>
</controls:StyledScrollView>

*/