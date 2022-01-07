using System;
using System.Windows.Data;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class NewLineToSpaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string text)
            {
                return text.Replace('\n', ' ').Replace('\r', ' ');
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
