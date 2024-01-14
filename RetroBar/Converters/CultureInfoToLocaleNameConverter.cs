using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(CultureInfo), typeof(string))]
    public class CultureInfoToLocaleNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CultureInfo cultureInfo)
            {
                switch (parameter)
                { 
                    case "TwoLetterIsoLanguageName": return cultureInfo.TwoLetterISOLanguageName.ToUpper();
                    case "EnglishName": return cultureInfo.EnglishName;
                    case null: return Binding.DoNothing;
                }
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}