// Create a custom ScrollView wrapper for beautiful scrollbar styling

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace EHMR.Resources.Controls
{
    /// <summary>
    /// Custom ScrollView with visible, styled vertical scrollbar
    /// </summary>
    public class CustomScrollView : ScrollView
    {
        // Bindable property for scrollbar color
        public static readonly BindableProperty ScrollBarColorProperty =
            BindableProperty.Create(
                nameof(ScrollBarColor),
                typeof(Color),
                typeof(CustomScrollView),
                Color.FromArgb("#7B9DB2"), // Your SparkAccentTeal
                propertyChanged: OnScrollBarColorChanged
            );

        public Color ScrollBarColor
        {
            get => (Color)GetValue(ScrollBarColorProperty);
            set => SetValue(ScrollBarColorProperty, value);
        }

        public CustomScrollView()
        {
            // Force scrollbar visibility on all platforms
            this.VerticalScrollBarVisibility=ScrollBarVisibility.Always;
            this.HorizontalScrollBarVisibility=ScrollBarVisibility.Never;

            // Subscribe to scroll events to update scrollbar appearance
            this.Scrolled+=OnScrolled;
        }

        private static void OnScrollBarColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CustomScrollView)bindable;
            control.UpdateScrollBarColor((Color)newValue);
        }

        private void UpdateScrollBarColor(Color color)
        {
#if __ANDROID__
            if (this.Handler?.PlatformView is Android.Widget.ScrollView scrollView)
            {
                // Set scrollbar color for Android (API 29+)
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
                {
                    scrollView.VerticalScrollbarTrackColor = color.AsAndroid().ToArgb();
                }
            }
#endif

#if __IOS__
            if (this.Handler?.PlatformView is UIKit.UIScrollView uiScrollView)
            {
                // iOS scrollbar color is controlled by the view's tint color
                uiScrollView.IndicatorStyle = UIKit.UIScrollViewIndicatorStyle.Default;
            }
#endif
        }

        private void OnScrolled(object sender, ScrolledEventArgs e)
        {
            // Optional: Add fade-in/fade-out effect for scrollbar
            // This can be customized based on your UX needs
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// USAGE IN XAML:
// ═══════════════════════════════════════════════════════════════════════════

/*
Add namespace at top of ContentPage:
    xmlns:controls="clr-namespace:EHMR.Resources.Controls"

Replace your ScrollView with:
    <controls:CustomScrollView 
        x:Name="FormScrollView"
        ScrollBarColor="{StaticResource SparkAccentTeal}">
        <VerticalStackLayout Padding="6" Spacing="6">
            <!-- Your content -->
        </VerticalStackLayout>
    </controls:CustomScrollView>
*/