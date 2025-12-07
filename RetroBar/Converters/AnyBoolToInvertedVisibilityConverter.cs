using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RetroBar.Converters
{
    /// <summary>
    /// Converts multiple boolean values to Visibility.
    /// Returns Collapsed if ANY value is true, otherwise Visible.
    /// </summary>
    public class AnyBoolToInvertedVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return Visibility.Visible;

            foreach (var value in values)
            {
                if (value is bool boolValue && boolValue)
                {
                    return Visibility.Collapsed;
                }
            }

            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
