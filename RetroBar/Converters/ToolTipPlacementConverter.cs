using System;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using ManagedShell.AppBar;
using RetroBar.Utilities;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(bool), typeof(PlacementMode))]
    public class ToolTipPlacementConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (Settings.Instance.Edge == (int)AppBarEdge.Left)
            {
                return PlacementMode.Right;
            }
            else if (Settings.Instance.Edge == (int)AppBarEdge.Right)
            {
                return PlacementMode.Left;
            }
            else if (Settings.Instance.Edge == (int)AppBarEdge.Top)
            {
                return PlacementMode.Bottom;
            }
            else
            {
                return PlacementMode.Top;
            }
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
