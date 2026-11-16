using System;
using System.Collections.Generic;
using System.Globalization;

namespace RetroBar.Utilities
{
    public record NetworkTrafficCounter(Guid Id, string Name, ulong Received, ulong Sent);
    public record NetworkTrafficRate(double Download, double Upload);

    /// <summary>Differences each interface independently so reconnects and counter resets cannot cause spikes.</summary>
    public sealed class NetworkTrafficSampler
    {
        private Dictionary<Guid, NetworkTrafficCounter> _previous = new();
        private double? _previousTime;

        public NetworkTrafficRate Sample(IEnumerable<NetworkTrafficCounter> counters, double seconds)
        {
            var current = new Dictionary<Guid, NetworkTrafficCounter>();
            double download = 0, upload = 0;
            double elapsed = seconds - (_previousTime ?? seconds);
            foreach (var counter in counters)
            {
                // Interface identity also guards against duplicate rows.
                if (!current.TryAdd(counter.Id, counter)) continue;
                if (elapsed > 0 && elapsed <= 5 && _previous.TryGetValue(counter.Id, out var previous))
                {
                    if (counter.Received >= previous.Received)
                        download += (counter.Received - previous.Received) / elapsed;
                    if (counter.Sent >= previous.Sent)
                        upload += (counter.Sent - previous.Sent) / elapsed;
                }
            }
            _previous = current;
            _previousTime = seconds;
            return new NetworkTrafficRate(download, upload);
        }

        public void Reset()
        {
            _previous.Clear();
            _previousTime = null;
        }

        public static string FormatRate(double bytesPerSecond)
        {
            string[] units = { "B/s", "KB/s", "MB/s", "GB/s", "TB/s" };
            double value = Math.Max(0, double.IsFinite(bytesPerSecond) ? bytesPerSecond : 0);
            int unit = 0;
            while (value >= 1000 && unit < units.Length - 1)
            {
                value /= 1000;
                unit++;
            }
            // Keep rounding from producing a misleading 1000 KB/s at a unit boundary.
            if (unit < units.Length - 1 && Math.Round(value, unit == 0 ? 0 : 1) >= 1000)
            {
                value /= 1000;
                unit++;
            }
            return value.ToString(unit == 0 ? "0" : "0.0", CultureInfo.CurrentCulture) + " " + units[unit];
        }
    }
}
