using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RetroBar.Utilities
{
    public class ExplorerMonitor : IDisposable
    {
        private volatile bool _ExplorerMonitorIsMonitoring;
        private _ExplorerMonitor _explorerMonitor;

        public void ExplorerMonitorStart()
        {
            if(_ExplorerMonitorIsMonitoring) // Prevent multiple monitors.
            {
                return;
            }
            else
            {
                _ExplorerMonitorIsMonitoring = true; // We will set flag to true to prevent multiple monitors.

                // Start monitoring
                _explorerMonitor = new _ExplorerMonitor();
                _explorerMonitor.Show();
            }
        }

        // ExplorerMonitor is a hidden form that captures taskbar events
        private class _ExplorerMonitor : Form
        {
            private const int GWL_EXSTYLE = -20;
            private const int WS_EX_TOOLWINDOW = 0x00000080;
            private const int WS_EX_APPWINDOW = 0x00040000;

            [DllImport("user32.dll", SetLastError = true)] private static extern uint RegisterWindowMessage(string lpString);
            [DllImport("user32.dll")] private static extern IntPtr SetWindowLong(IntPtr hwnd, int index, int newStyle);
            [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);

            public _ExplorerMonitor()
            {
                // These will make the ExplorerMonitor form completely invisible, so we can use it just as a monitor and not form
                ClientSize = new System.Drawing.Size(1, 1); // Set the size to 1x1 pixel (tiny and invisible)
                FormBorderStyle = FormBorderStyle.None; // Make the form borderless
                BackColor = System.Drawing.Color.Lime;  // Use a color thats fully transparent in the form
                TransparencyKey = System.Drawing.Color.Lime; // Set transparency key to make the color transparent
                ShowInTaskbar = false; // Ensure the form doesnt appear in the taskbar
                ControlBox = false; // Ensure no controls (like buttons) are on the form
                Visible = false; // Set the form as invisible
                StartPosition = FormStartPosition.Manual; // Dont center this form
                Location = new System.Drawing.Point(-1000, -1000); // Move it far off-screen

                // Remove ExplorerMonitor from the Alt+Tab list by modifying its extended style
                int extendedStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                SetWindowLong(this.Handle, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);
            }

            // We will override WndProc to listen for TaskbarCreated event
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == (int)RegisterWindowMessage("TaskbarCreated"))
                {
                    try
                    {
                        string appPath = Process.GetCurrentProcess().MainModule?.FileName;
                        Process.Start(appPath); // Start a new instance of RetroBar
                        Environment.Exit(0); // Exit the current instance of RetroBar
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error restarting RetroBar: {ex.Message}");
                    }
                }

                // Call the base class to process other messages so we dont accidentally cause crashes or bugs.
                base.WndProc(ref m);
            }
        }

        public void Dispose()
        {
            if (_explorerMonitor != null){_explorerMonitor?.Dispose();}
        }
    }
}
