using ManagedShell.Interop;
using ManagedShell.WindowsTasks;
using System;
using System.Windows.Data;

namespace RetroBar.Converters
{
    [ValueConversion(typeof(ApplicationWindow), typeof(string))]
    public class TaskLabelConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null || values.Length == 0 || !(values[0] is string title))
            {
                return Binding.DoNothing;
            }

            string winFileDescription = values.Length > 1 ? values[1] as string : null;
            string winFileName = values.Length > 2 ? values[2] as string : null;
            int progressValue = values.Length > 3 && values[3] is int pv ? pv : 0;
            NativeMethods.TBPFLAG progressState = values.Length > 4 && values[4] is NativeMethods.TBPFLAG ps ? ps : NativeMethods.TBPFLAG.TBPF_NOPROGRESS;

            int taskCount = 0;
            if (values.Length > 5)
            {
                if (values[5] is int i)
                {
                    taskCount = i;
                }
                else if (values[5] is System.Collections.ICollection coll)
                {
                    taskCount = coll.Count;
                }
            }

            string displayTitle = title;
            if (taskCount > 1)
            {
                if (!string.IsNullOrWhiteSpace(winFileDescription))
                {
                    displayTitle = winFileDescription;
                }
                else if (!string.IsNullOrWhiteSpace(winFileName))
                {
                    try
                    {
                        displayTitle = System.IO.Path.GetFileNameWithoutExtension(winFileName);
                    }
                    catch
                    {
                        displayTitle = title;
                    }
                }
            }

            if (progressState == NativeMethods.TBPFLAG.TBPF_NOPROGRESS ||
                progressState == NativeMethods.TBPFLAG.TBPF_INDETERMINATE ||
                progressValue < 0)
            {
                return displayTitle;
            }

            if (displayTitle.Contains("%"))
            {
                // Window title may already contain progress percentage
                return displayTitle;
            }

            return $"[{Math.Floor(progressValue / 65534.0 * 100)}%] {displayTitle}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
