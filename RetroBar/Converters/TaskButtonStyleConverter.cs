using System;
using System.Windows;
using System.Windows.Data;
using ManagedShell.WindowsTasks;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(bool), typeof(Style))]
    public class TaskButtonStyleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(values[0] is FrameworkElement fxElement))
            {
                return null;
            }

            if (fxElement.ContextMenu?.IsOpen == true)
            {
                // Always show as active with an open context menu
                return fxElement.FindResource("TaskButtonActive");
            }

            // Default style is Inactive...
            var fxStyle = fxElement.FindResource("TaskButton");

            if (values[1] is ApplicationWindow.WindowState state)
            {
                switch (state)
                {
                    case ApplicationWindow.WindowState.Active:
                        fxStyle = fxElement.FindResource("TaskButtonActive");
                        break;

                    case ApplicationWindow.WindowState.Flashing:
                        fxStyle = fxElement.FindResource("TaskButtonFlashing");
                        break;
                }
            }

            return fxStyle;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
