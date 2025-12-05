using System;
using System.Globalization;
using System.Windows.Data;

namespace RetroBar.Converters
{
    public class InverseScaleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is double scale && scale > 0)
            {
                return 1.0 / scale;
            }
            return 1.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
