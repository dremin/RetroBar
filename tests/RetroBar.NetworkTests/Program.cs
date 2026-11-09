using System;
using System.Linq;
using System.Threading;
using RetroBar.Utilities;

var sampler = new NetworkTrafficSampler();
var a = Guid.NewGuid();
var b = Guid.NewGuid();
int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
    Console.WriteLine("PASS " + name);
}
NetworkTrafficCounter Counter(Guid id, ulong received, ulong sent) => new(id, "Test", received, sent);
var rate = sampler.Sample(new[] { Counter(a, 1000, 2000) }, 0);
Check(rate.Download == 0 && rate.Upload == 0, "Initial baseline");
rate = sampler.Sample(new[] { Counter(a, 4000, 8000) }, 1.5);
Check(rate.Download == 2000 && rate.Upload == 4000, "Actual elapsed time and direction");
rate = sampler.Sample(new[] { Counter(a, 5000, 9000), Counter(a, 5000, 9000), Counter(b, 900000, 900000) }, 2.5);
Check(rate.Download == 1000 && rate.Upload == 1000, "Duplicate ignored; new adapter baselines");
rate = sampler.Sample(Array.Empty<NetworkTrafficCounter>(), 3.5);
Check(rate.Download == 0 && rate.Upload == 0, "Disconnected clears stale traffic");
rate = sampler.Sample(new[] { Counter(a, 999999, 999999) }, 4.5);
Check(rate.Download == 0 && rate.Upload == 0, "Reconnect baselines");
rate = sampler.Sample(new[] { Counter(a, 1, 1) }, 5.5);
Check(rate.Download == 0 && rate.Upload == 0, "Counter reset cannot underflow");
rate = sampler.Sample(new[] { Counter(a, 1000000, 1000000) }, 60);
Check(rate.Download == 0 && rate.Upload == 0, "Resume after sleep baselines");
sampler.Reset();
sampler.Sample(new[] { Counter(a, 1, 2), Counter(b, 1, 2) }, 0);
rate = sampler.Sample(new[] { Counter(a, 501, 202), Counter(b, 701, 302) }, 1);
Check(rate.Download == 1200 && rate.Upload == 500, "Independent physical adapters aggregate once");
Check(NetworkTrafficSampler.FormatRate(0) == "0 B/s", "Idle units");
Check(NetworkTrafficSampler.FormatRate(1500000).EndsWith(" MB/s"), "Readable megabyte units");
Check(NetworkTrafficSampler.FormatRate(999999).EndsWith(" MB/s"), "Unit rounding boundary");
Check(NetworkMonitor.IsTrafficInterface(1, 5), "Physical connected interface accepted");
foreach (byte flags in new byte[] { 0, 4, 7, 0x15, 0x25, 0x45, 0x85 })
    Check(!NetworkMonitor.IsTrafficInterface(1, flags), "Excluded flags " + flags);
Check(!NetworkMonitor.IsTrafficInterface(2, 5), "Down interface excluded");
var wifi = new NetworkAdapter(1, a, "Wi-Fi", NetworkConnectionType.WiFi);
var ethernet = new NetworkAdapter(2, b, "Ethernet", NetworkConnectionType.Ethernet);
Check(NetworkMonitor.SelectAdapter(new[] { wifi, ethernet }, 1) == wifi, "Tray icon still follows physical route");
Check(NetworkMonitor.SelectAdapter(new[] { wifi, ethernet }, 99) == ethernet, "Tray icon VPN fallback preserved");
Check(NetworkMonitor.SelectAdapter(Array.Empty<NetworkAdapter>(), 0) == null, "Tray icon disconnected selection");
Check(NetworkMonitor.GetSignalBars(24) == 1 && NetworkMonitor.GetSignalBars(75) == 4, "Wi-Fi signal levels preserved");
var initial = NetworkMonitor.GetTrafficCounters();
Console.WriteLine("Live adapters: " + string.Join(", ", initial.Select(x => x.Name)));
Check(initial.Select(x => x.Id).Distinct().Count() == initial.Count, "Native adapter identities unique");
sampler.Reset();
sampler.Sample(initial, 0);
Thread.Sleep(1100);
rate = sampler.Sample(NetworkMonitor.GetTrafficCounters(), 1.1);
Check(double.IsFinite(rate.Download) && double.IsFinite(rate.Upload), "Live native counters sampled");
Console.WriteLine($"{checks} checks passed. Live download={rate.Download:F0} B/s upload={rate.Upload:F0} B/s");
if (args.Contains("--watch"))
{
    var watch = System.Diagnostics.Stopwatch.StartNew();
    sampler.Reset();
    for (int i = 0; i < 20; i++)
    {
        rate = sampler.Sample(NetworkMonitor.GetTrafficCounters(), watch.Elapsed.TotalSeconds);
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} Download: {NetworkTrafficSampler.FormatRate(rate.Download)}, Upload: {NetworkTrafficSampler.FormatRate(rate.Upload)}");
        Thread.Sleep(1000);
    }
}
