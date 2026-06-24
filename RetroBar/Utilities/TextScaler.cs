using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RetroBar.Utilities
{
    // Renders the TextBlocks within a control at a real, pixel-aligned font size when the
    // taskbar is scaled, instead of letting the taskbar's ScaleTransform stretch text that
    // was laid out at its base size (which pushes glyphs off the pixel grid and looks blurry).
    //
    // Each TextBlock gets FontSize = round(baseFontSize * scale) and a 1/scale counter-transform,
    // so its glyphs render with a net identity transform (crisp) while it still occupies the
    // scaled layout slot. The base size is read back from the element each call (after clearing
    // our previous override), so repeated calls don't compound and the base stays correct when
    // the theme changes it.
    public static class TextScaler
    {
        public static void ApplyScaling(DependencyObject root, double scale)
        {
            if (root == null)
            {
                return;
            }

            int count = VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);

                if (child is TextBlock textBlock)
                {
                    // Drop our previous override so FontSize reverts to the (theme) base size.
                    textBlock.ClearValue(TextBlock.FontSizeProperty);

                    if (scale > 1.0)
                    {
                        textBlock.FontSize = Math.Round(textBlock.FontSize * scale);
                        textBlock.LayoutTransform = new ScaleTransform(1.0 / scale, 1.0 / scale);
                    }
                    else
                    {
                        textBlock.ClearValue(FrameworkElement.LayoutTransformProperty);
                    }
                }

                ApplyScaling(child, scale);
            }
        }
    }
}
