using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RetroBar.Converters
{
    /// <summary>
    /// Extracts an icon from an ImageList by index
    /// Expects values[0] = bitmap index (uint), values[1] = image list handle (IntPtr)
    /// </summary>
    public class ImageListIconConverter : IMultiValueConverter
    {
        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern IntPtr ImageList_GetIcon(IntPtr himl, int i, int flags);

        // ImageList_GetIcon flags
        private const int ILD_NORMAL = 0x00000000;
        private const int ILD_TRANSPARENT = 0x00000001;
        private const int ILD_BLEND25 = 0x00000002;
        private const int ILD_FOCUS = 0x00000002;
        private const int ILD_BLEND50 = 0x00000004;
        private const int ILD_SELECTED = 0x00000004;
        private const int ILD_BLEND = 0x00000004;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("comctl32.dll", EntryPoint = "#727")]
        private static extern int HIMAGELIST_QueryInterface(IntPtr himl, ref Guid riid, out IntPtr ppv);

        // IImageList COM interface
        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig]
            int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);

            [PreserveSig]
            int ReplaceIcon(int i, IntPtr hicon, ref int pi);

            [PreserveSig]
            int SetOverlayImage(int iImage, int iOverlay);

            [PreserveSig]
            int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);

            [PreserveSig]
            int AddMasked(IntPtr hbmImage, int crMask, ref int pi);

            [PreserveSig]
            int Draw(ref IMAGELISTDRAWPARAMS pimldp);

            [PreserveSig]
            int Remove(int i);

            [PreserveSig]
            int GetIcon(int i, int flags, ref IntPtr picon);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGELISTDRAWPARAMS
        {
            public int cbSize;
            public IntPtr himl;
            public int i;
            public IntPtr hdcDst;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int xBitmap;
            public int yBitmap;
            public int rgbBk;
            public int rgbFg;
            public int fStyle;
            public int dwRop;
            public int fState;
            public int Frame;
            public int crEffect;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 2)
                {
                    return CreateFallbackIcon(0);
                }

                // Get bitmap index
                uint bitmapIndex = 0;
                if (values[0] is uint uintIndex)
                    bitmapIndex = uintIndex;
                else if (values[0] is int intIndex)
                    bitmapIndex = (uint)intIndex;
                else
                {
                    return CreateFallbackIcon(0);
                }

                // NOTE: Cross-process ImageList extraction doesn't work, even with process handles.
                // ImageList handles are process-specific and can't be accessed from another process.
                // We use fallback icons based on the bitmap index, which still allows us to show
                // different icons (play/pause/prev/next) as the index changes.

                return CreateFallbackIcon(bitmapIndex);
            }
            catch
            {
                // Silently fall back to generic icon on any error
                return CreateFallbackIcon(0);
            }
        }

        private IntPtr TryGetIconViaCOM(IntPtr hImageList, int index, string debugPath)
        {
            try
            {
                // Get IImageList interface from HIMAGELIST
                Guid iid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
                IntPtr ppv = IntPtr.Zero;

                int hr = HIMAGELIST_QueryInterface(hImageList, ref iid, out ppv);

                if (hr == 0 && ppv != IntPtr.Zero)
                {
                    System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Got IImageList COM interface\n");

                    // Marshal to IImageList interface
                    IImageList imageList = (IImageList)Marshal.GetObjectForIUnknown(ppv);
                    IntPtr hIcon = IntPtr.Zero;

                    // Get icon from ImageList
                    hr = imageList.GetIcon(index, 0, ref hIcon);

                    // Release COM object
                    Marshal.Release(ppv);

                    if (hr == 0 && hIcon != IntPtr.Zero)
                    {
                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: IImageList.GetIcon succeeded\n");
                        return hIcon;
                    }
                    else
                    {
                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: IImageList.GetIcon failed with hr={hr}\n");
                    }
                }
                else
                {
                    System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: HIMAGELIST_QueryInterface failed with hr={hr}\n");
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: COM approach exception: {ex.Message}\n");
            }

            return IntPtr.Zero;
        }

        private ImageSource CreateFallbackIcon(uint bitmapIndex)
        {
            // Create a visual with Segoe MDL2 Assets font icon
            // Common media button indices map to specific glyphs
            string glyph = GetMediaControlGlyph(bitmapIndex);

            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                // Draw icon background (transparent)
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 16, 16));

                // Draw glyph - use a neutral dark gray that works on button face
                var formattedText = new FormattedText(
                    glyph,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe MDL2 Assets"),
                    14, // Slightly larger for better visibility
                    new SolidColorBrush(Color.FromRgb(0, 0, 0)), // Black works on classic button face
                    VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                // Center the glyph
                double x = (16 - formattedText.Width) / 2;
                double y = (16 - formattedText.Height) / 2;
                dc.DrawText(formattedText, new Point(x, y));
            }

            var renderBitmap = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(visual);
            renderBitmap.Freeze();

            return renderBitmap;
        }

        private string GetMediaControlGlyph(uint bitmapIndex)
        {
            // Maps common bitmap indices to Segoe MDL2 Assets glyphs
            // These patterns are observed from Spotify and other media apps

            // Spotify typically uses:
            // - 0/1: Previous track
            // - 4: Pause (when playing)
            // - 5: Play (when paused)
            // - 6/7: Next track
            // - 10: Shuffle/Repeat

            switch (bitmapIndex)
            {
                // Previous track
                case 0:
                case 1:
                    return "\uE892"; // Previous

                // Play button (when paused)
                case 3:
                case 5:
                    return "\uE768"; // Play

                // Pause button (when playing)
                case 2:
                case 4:
                    return "\uE769"; // Pause

                // Next track
                case 6:
                case 7:
                    return "\uE893"; // Next

                // Shuffle/Repeat/Additional controls
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                    return "\uE8B1"; // Shuffle

                // Stop button
                case 13:
                case 14:
                    return "\uE71A"; // Stop

                // Default: generic control icon
                default:
                    return "\uE8FB"; // More controls (three dots)
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
