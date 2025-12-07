using ManagedShell.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ManagedShell.AppBar
{
    public class AppBarScreen
    {
        public Rectangle Bounds { get; set; }
        
        public string DeviceName { get; set; }
        
        public bool Primary { get; set; }
        
        public Rectangle WorkingArea { get; set; }

        public IntPtr HMonitor { get; set; }

        public static AppBarScreen FromScreen(Screen screen)
        {
            IntPtr hMonitor = NativeMethods.MonitorFromPoint(new Point(screen.Bounds.X, screen.Bounds.Y), NativeMethods.MONITOR_DEFAULTTONEAREST);
            return new AppBarScreen
            {
                Bounds = screen.Bounds,
                DeviceName = screen.DeviceName,
                Primary = screen.Primary,
                WorkingArea = screen.WorkingArea,
                HMonitor = hMonitor
            };
        }

        public static AppBarScreen FromPrimaryScreen()
        {
            return FromScreen(Screen.PrimaryScreen);
        }

        public static List<AppBarScreen> FromAllScreens()
        {
            List<AppBarScreen> screens = new List<AppBarScreen>();
            
            foreach (var screen in Screen.AllScreens)
            {
                screens.Add(FromScreen(screen));
            }

            return screens;
        }
    }
}
