using RetroBar.Utilities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RetroBar.Controls
{
    public partial class NetworkSpeed : UserControl
    {
        private readonly NetworkTrafficSampler _sampler = new();
        private readonly DispatcherTimer _timer = new(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        public NetworkSpeed()
        {
            InitializeComponent();
            _timer.Tick += Timer_Tick;
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _sampler.Reset();
            UpdateRates();
            _timer.Start();
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _sampler.Reset();
        }

        private void Timer_Tick(object sender, EventArgs e) => UpdateRates();

        private void UpdateRates()
        {
            try
            {
                var counters = NetworkMonitor.GetTrafficCounters();
                var rate = _sampler.Sample(counters, (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency);
                DownloadText.Text = "Download: " + NetworkTrafficSampler.FormatRate(rate.Download);
                UploadText.Text = "Upload: " + NetworkTrafficSampler.FormatRate(rate.Upload);
                ToolTip = counters.Count == 0 ? "Network disconnected" :
                    "Physical network traffic: " + string.Join(", ", counters.Select(counter => counter.Name)) +
                    "\nBytes per second (1 KB = 1000 bytes). Includes local network traffic.";
            }
            catch (Exception ex) when (ex is System.Net.NetworkInformation.NetworkInformationException ||
                                       ex is System.ComponentModel.Win32Exception)
            {
                _sampler.Reset();
                DownloadText.Text = "Download: —";
                UploadText.Text = "Upload: —";
                ToolTip = "Network counters temporarily unavailable";
            }
        }
    }
}
