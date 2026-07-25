using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ManagedShell.WindowsTasks;

namespace RetroBar.Converters
{
    public class TaskThumbnailItemsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Length > 1 && values[1] is IEnumerable tasks)
            {
                var list = tasks.OfType<ApplicationWindow>().ToList();
                if (list.Count > 0)
                {
                    return list;
                }
            }

            if (values != null && values.Length > 0 && values[0] is ApplicationWindow win)
            {
                return new List<ApplicationWindow> { win };
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}