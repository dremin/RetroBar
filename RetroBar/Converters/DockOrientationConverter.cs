using System;
using System.Windows.Controls;
using System.Windows.Data;
using ManagedShell.AppBar;
using RetroBar.Utilities;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(bool), typeof(Orientation))]
    public class DockOrientationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool vertical = Settings.Instance.Edge == (int)AppBarEdge.Left || Settings.Instance.Edge == (int)AppBarEdge.Right;

            // parameter is a string "leading" or "trailing"

            if (parameter is string position)
            {
                if (vertical)
                {
                    if (position == "leading")
                    {
                        return Dock.Top;
                    }
                    else
                    {
                        return Dock.Bottom;
                    }
                }
                else
                {
                    if (position == "leading")
                    {
                        return Dock.Left;
                    }
                    else
                    {
                        return Dock.Right;
                    }
                }
            }

            return Dock.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
