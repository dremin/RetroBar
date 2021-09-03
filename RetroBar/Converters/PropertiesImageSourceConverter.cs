using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using RetroBar.Utilities;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(bool), typeof(BitmapImage))]
    public class PropertiesImageSourceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(values[0] is FrameworkElement fxElement))
            {
                return null;
            }

            if (Settings.Instance.ShowClock)
            {
                return fxElement.FindResource("PropertiesImage");
            }
            else
            {
                return fxElement.FindResource("PropertiesImage_NoClock");
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
