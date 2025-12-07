using ManagedShell.Interop;
using System.Windows.Media;

namespace ManagedShell.WindowsTasks
{
    public class ThumbnailButton
    {
        public NativeMethods.THUMBBUTTONMASK Mask { get; set; }
        public uint Id { get; set; }
        public uint Bitmap { get; set; }
        public int Icon { get; set; }
        public string Tooltip { get; set; }
        public NativeMethods.THUMBBUTTONFLAGS Flags { get; set; }

        /// <summary>
        /// The extracted icon as an ImageSource. This is set when the icon is extracted from
        /// the remote process's ImageList, avoiding cross-process handle access issues.
        /// </summary>
        public ImageSource IconSource { get; set; }

        public ThumbnailButton(NativeMethods.THUMBBUTTON button)
        {
            Mask = button.dwMask;
            Id = button.iId;
            Bitmap = button.iBitmap;
            Icon = button.hIcon;
            Tooltip = button.szTip ?? string.Empty;
            Flags = button.dwFlags;
        }
    }
}
