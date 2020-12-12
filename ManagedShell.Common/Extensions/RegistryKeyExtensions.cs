using Microsoft.Win32;

namespace ManagedShell.Common.Extensions
{
    public static class RegistryKeyExtensions
    {
        public static T GetValue<T>(this RegistryKey key, string valueName, T defaultValue = default)
        {
            if (key == null)
                return defaultValue;

            if (string.IsNullOrWhiteSpace(valueName))
                return defaultValue;

            object val = key.GetValue(valueName, defaultValue);
            if (val is T value)
                return value;

            return defaultValue;
        }

        public static T GetSubKeyValue<T>(this RegistryKey key, string subKey, string valueName, T defaultValue = default)
        {
            if (key == null)
                return defaultValue;

            if (string.IsNullOrWhiteSpace(subKey))
                return defaultValue;

            if (string.IsNullOrWhiteSpace(valueName))
                return defaultValue;

            var reg = key.OpenSubKey(subKey);

            if (reg == null)
                return defaultValue;

            return reg.GetValue<T>(valueName, defaultValue);
        }
    }
}