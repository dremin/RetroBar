using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RetroBar.Converters
{
    public class IconHandleToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IntPtr hIcon = IntPtr.Zero;

            // Handle both int (HICON handle) and IntPtr
            if (value is int intHandle && intHandle != 0)
            {
                hIcon = new IntPtr(intHandle);
            }
            else if (value is IntPtr ptrHandle && ptrHandle != IntPtr.Zero)
            {
                hIcon = ptrHandle;
            }

            if (hIcon != IntPtr.Zero)
            {
                try
                {
                    return Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
