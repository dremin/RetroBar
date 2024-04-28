using System;
using System.Windows.Data;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(bool), typeof(int))]
    public class BoolToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                var multiplier = 1;
                if (parameter is string strValue)
                {
                    multiplier = System.Convert.ToInt32(strValue);
                }
                return boolValue ? 1 * multiplier : 0;
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue == 1 ? true : false;
            }

            return Binding.DoNothing;
        }
    }
}
