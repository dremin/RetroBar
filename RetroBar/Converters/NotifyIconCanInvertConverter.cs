using ManagedShell.WindowsTray;
using RetroBar.Extensions;
using System;
using System.Windows.Data;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(NotifyIcon), typeof(bool))]
    public class NotifyIconCanInvertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is NotifyIcon icon)
            {
                return icon.CanInvert();
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
