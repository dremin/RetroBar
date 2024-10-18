using ManagedShell.AppBar;
using RetroBar.Utilities;
using System.Windows.Data;
using System;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(AppBarEdge), typeof(bool))]
    public class EdgeIsHorizontalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Settings.Instance.Edge is AppBarEdge.Top or AppBarEdge.Bottom;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
