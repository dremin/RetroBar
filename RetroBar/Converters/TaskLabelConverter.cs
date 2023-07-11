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
            if (!(values[0] is string title &&
                values[1] is int progressValue &&
                values[2] is NativeMethods.TBPFLAG progressState))
            {
                return Binding.DoNothing;
            }

            if (progressState == NativeMethods.TBPFLAG.TBPF_NOPROGRESS ||
                progressState == NativeMethods.TBPFLAG.TBPF_INDETERMINATE ||
                progressValue < 0)
            {
                return title;
            }

            if (title.Contains("%"))
            {
                // Window title may already contain progress percentage
                return title;
            }

            return $"[{Math.Floor(progressValue / 65534.0 * 100)}%] {title}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
