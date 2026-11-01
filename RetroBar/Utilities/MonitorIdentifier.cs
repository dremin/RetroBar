using ManagedShell.Common.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace RetroBar.Utilities
{
    /// <summary>
    /// Identifies physical monitors by a stable ID read from their EDID (manufacturer, product code and serial number),
    /// so that per-monitor settings survive changes of display numbering, cables and ports.
    /// </summary>
    public static class MonitorIdentifier
    {
        public readonly struct MonitorInfo
        {
            public string Id { get; init; }
            public string Name { get; init; }
        }

        private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        // Keyed by display device name; cleared when the display configuration changes.
        private static readonly ConcurrentDictionary<string, MonitorInfo> _cache = new();

        /// <summary>
        /// Returns the stable ID and friendly name of the monitor shown on a display (e.g. \\.\DISPLAY2).
        /// Falls back to the display device name when the monitor cannot be identified.
        /// </summary>
        public static MonitorInfo GetMonitor(string displayDeviceName)
        {
            if (string.IsNullOrEmpty(displayDeviceName))
            {
                return new MonitorInfo { Id = "", Name = "" };
            }

            return _cache.GetOrAdd(displayDeviceName, ReadMonitorInfo);
        }

        public static void ClearCache()
        {
            _cache.Clear();
        }

        public static string GetMonitorId(string displayDeviceName)
        {
            return GetMonitor(displayDeviceName).Id;
        }

        private static string GetMonitorInterfacePath(string displayDeviceName)
        {
            DISPLAY_DEVICE device = new DISPLAY_DEVICE();
            device.cb = Marshal.SizeOf(device);

            // The first monitor attached to the display
            if (EnumDisplayDevices(displayDeviceName, 0, ref device, EDD_GET_DEVICE_INTERFACE_NAME))
            {
                return device.DeviceID;
            }

            return null;
        }

        private static MonitorInfo ReadMonitorInfo(string displayDeviceName)
        {
            string interfacePath = GetMonitorInterfacePath(displayDeviceName);

            if (string.IsNullOrEmpty(interfacePath))
            {
                return new MonitorInfo { Id = displayDeviceName, Name = displayDeviceName };
            }

            // \\?\DISPLAY#DELA021#5&2f889435&1&UID1025#{e6f07b5f-...} -> DISPLAY\DELA021\5&2f889435&1&UID1025
            string[] parts = interfacePath.Split('#');
            string instancePath = parts.Length >= 3 ? $"DISPLAY\\{parts[1]}\\{parts[2]}" : null;
            string fallbackId = parts.Length >= 3 ? $"{parts[1]}\\{parts[2]}" : displayDeviceName;

            try
            {
                if (instancePath != null)
                {
                    using RegistryKey key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{instancePath}\Device Parameters");

                    if (key?.GetValue("EDID") is byte[] edid && edid.Length >= 128)
                    {
                        return ParseEdid(edid, fallbackId);
                    }
                }
            }
            catch (Exception e)
            {
                ShellLogger.Warning($"MonitorIdentifier: Unable to read EDID for {interfacePath}: {e.Message}");
            }

            return new MonitorInfo { Id = fallbackId, Name = parts.Length >= 2 ? parts[1] : displayDeviceName };
        }

        private static MonitorInfo ParseEdid(byte[] edid, string fallbackId)
        {
            // Manufacturer: three 5-bit letters, big endian, bytes 8-9
            int mfg = (edid[8] << 8) | edid[9];
            string manufacturer = new string(new[]
            {
                (char)('A' - 1 + ((mfg >> 10) & 0x1F)),
                (char)('A' - 1 + ((mfg >> 5) & 0x1F)),
                (char)('A' - 1 + (mfg & 0x1F))
            });
            int product = edid[10] | (edid[11] << 8);
            uint serialNumber = BitConverter.ToUInt32(edid, 12);

            string serialText = null;
            string name = null;

            // Four 18-byte descriptors; 0xFF = serial number text, 0xFC = monitor name
            for (int offset = 54; offset <= 108; offset += 18)
            {
                if (edid[offset] != 0 || edid[offset + 1] != 0)
                {
                    continue;
                }

                string text = Encoding.ASCII.GetString(edid, offset + 5, 13).Split('\n')[0].Trim();

                if (edid[offset + 3] == 0xFF)
                {
                    serialText = text;
                }
                else if (edid[offset + 3] == 0xFC)
                {
                    name = text;
                }
            }

            string serial = !string.IsNullOrEmpty(serialText) ? serialText : (serialNumber != 0 ? serialNumber.ToString() : null);

            return new MonitorInfo
            {
                // Without a serial number, identical monitors can only be told apart by the port they are connected to.
                Id = serial != null ? $"{manufacturer}{product:X4}-{serial}" : fallbackId,
                Name = !string.IsNullOrEmpty(name) ? name : $"{manufacturer}{product:X4}"
            };
        }
    }
}
