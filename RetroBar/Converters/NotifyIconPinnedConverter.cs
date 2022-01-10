using System;
using System.Windows;
using System.Windows.Data;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(bool), typeof(string))]
    public class NotifyIconPinnedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isPinned)
            {
                return isPinned ? Application.Current.FindResource("always_show") : Application.Current.FindResource("always_hide");
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
