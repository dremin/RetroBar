using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace RetroBar.Converters
{
    public class TaskToolTipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0 || !(values[0] is string title))
            {
                return string.Empty;
            }

            string winFileDescription = values.Length > 1 ? values[1] as string : null;
            string winFileName = values.Length > 2 ? values[2] as string : null;
            
            int taskCount = 0;
            if (values.Length > 3 && values[3] is ICollection coll)
            {
                taskCount = coll.Count;
            }

            if (taskCount > 1)
            {
                string programName = title;
                if (!string.IsNullOrWhiteSpace(winFileDescription))
                {
                    programName = winFileDescription;
                }
                else if (!string.IsNullOrWhiteSpace(winFileName))
                {
                    try
                    {
                        programName = System.IO.Path.GetFileNameWithoutExtension(winFileName);
                    }
                    catch
                    {
                        programName = title;
                    }
                }

                return $"({taskCount}) {programName}";
            }

            return title;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}