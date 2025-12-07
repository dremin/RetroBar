using System.Drawing;
using System.Windows.Forms;

namespace ManagedShell.AppBar
{
	public class ScreenInfo
	{
		private ScreenInfo(string deviceName, Rectangle bounds)
		{
			DeviceName = deviceName;
			Bounds = bounds;
		}

		public static ScreenInfo Create(Screen screen)
		{
			return new ScreenInfo(screen.DeviceName, screen.Bounds);
		}

		public static ScreenInfo CreateVirtualScreen()
		{
			return new ScreenInfo(nameof(SystemInformation.VirtualScreen), SystemInformation.VirtualScreen);
		}

		public string DeviceName { get; }

		public Rectangle Bounds { get; }

		public bool IsVirtualScreen => DeviceName == nameof(SystemInformation.VirtualScreen);
	}
}
