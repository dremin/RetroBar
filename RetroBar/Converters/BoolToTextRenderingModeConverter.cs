using System;
using System.Windows.Data;
using System.Windows.Media;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(bool), typeof(TextRenderingMode))]
    public class BoolToTextRenderingModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TextRenderingMode.Auto : TextRenderingMode.Aliased;
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
