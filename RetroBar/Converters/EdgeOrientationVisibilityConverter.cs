using ManagedShell.AppBar;
using System;
using System.Windows;
using System.Windows.Data;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class EdgeOrientationVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is AppBarEdge edge && parameter is string visibleOrientation)
            {
                return ((edge == AppBarEdge.Left || edge == AppBarEdge.Right) && visibleOrientation == "vertical") ||
                    ((edge == AppBarEdge.Top || edge == AppBarEdge.Bottom) && visibleOrientation == "horizontal") ? Visibility.Visible : Visibility.Collapsed;
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
