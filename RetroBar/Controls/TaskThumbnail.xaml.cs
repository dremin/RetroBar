using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using ManagedShell.Interop;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for TaskThumbnail.xaml
    /// </summary>
    public partial class TaskThumbnail : UserControl
    {
        const double MAX_WIDTH = 180;
        const double MAX_HEIGHT = 120;

        public byte ThumbnailOpacity = 255;
        public double DpiScale = 1.0;

        public TaskThumbnail()
        {
            InitializeComponent();
        }

        public IntPtr Handle
        {
            get
            {
                HwndSource source = (HwndSource)PresentationSource.FromVisual(this);

                if (source == null)
                {
                    return IntPtr.Zero;
                }

                IntPtr handle = source.Handle;
                return handle;
            }
        }

        private IntPtr _thumbHandle;
        
        public static DependencyProperty SourceWindowHandleProperty = DependencyProperty.Register("SourceWindowHandle", typeof(IntPtr), typeof(TaskThumbnail), new PropertyMetadata(new IntPtr()));
        
        public IntPtr SourceWindowHandle
        {
            get
            {
                return (IntPtr)GetValue(SourceWindowHandleProperty);
            }
            set
            {
                SetValue(SourceWindowHandleProperty, value);
            }
        }

        public NativeMethods.Rect Rect
        {
            get
            {
                try
                {
                    if (this == null)
                        return new NativeMethods.Rect(0, 0, 0, 0);

                    var generalTransform = TransformToAncestor((System.Windows.Media.Visual)Parent);
                    var leftTopPoint = generalTransform.Transform(new Point(0, 0));
                    return new NativeMethods.Rect(
                          (int)(leftTopPoint.X * DpiScale),
                          (int)(leftTopPoint.Y * DpiScale),
                          (int)(leftTopPoint.X * DpiScale) + (int)(MAX_WIDTH * DpiScale),
                          (int)(leftTopPoint.Y * DpiScale) + (int)(MAX_HEIGHT * DpiScale)
                         );
                }
                catch
                {
                    return new NativeMethods.Rect(0, 0, 0, 0);
                }
            }
        }

        public void Refresh()
        {
            if (this == null)
                return;

            if (_thumbHandle == IntPtr.Zero)
                return;

            if (this != null)
            {
                NativeMethods.DwmQueryThumbnailSourceSize(_thumbHandle, out NativeMethods.PSIZE size);
                double aspectRatio = (double)size.x / size.y;

                var props = new NativeMethods.DWM_THUMBNAIL_PROPERTIES
                {
                    fVisible = true,
                    dwFlags = NativeMethods.DWM_TNP_VISIBLE | NativeMethods.DWM_TNP_RECTDESTINATION | NativeMethods.DWM_TNP_OPACITY,
                    opacity = ThumbnailOpacity,
                    rcDestination = Rect
                };

                if (this != null)
                {
                    if (size.x <= MAX_WIDTH && size.y <= MAX_HEIGHT)
                    {
                        // do not scale
                        Width = size.x;
                        Height = size.y;
                        props.rcDestination.Right = props.rcDestination.Left + size.x;
                        props.rcDestination.Bottom = props.rcDestination.Top + size.y;
                    }
                    else
                    {
                        // scale, preserving aspect ratio
                        double controlAspectRatio = MAX_WIDTH / MAX_HEIGHT;

                        if (aspectRatio > controlAspectRatio)
                        {
                            // wide
                            int height = (int)(MAX_WIDTH / aspectRatio);

                            Width = MAX_WIDTH;
                            Height = height * DpiScale;
                            props.rcDestination.Bottom = props.rcDestination.Top + height;
                        }
                        else if (aspectRatio < controlAspectRatio)
                        {
                            // tall
                            int width = (int)(MAX_HEIGHT * aspectRatio);

                            Width = width * DpiScale;
                            Height = MAX_HEIGHT;
                            props.rcDestination.Right = props.rcDestination.Left + width;
                        }
                    }
                }

                if (this != null)
                    NativeMethods.DwmUpdateThumbnailProperties(_thumbHandle, ref props);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_thumbHandle != IntPtr.Zero)
            {
                NativeMethods.DwmUnregisterThumbnail(_thumbHandle);
                _thumbHandle = IntPtr.Zero;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (SourceWindowHandle != IntPtr.Zero && Handle != IntPtr.Zero && NativeMethods.DwmRegisterThumbnail(Handle, SourceWindowHandle, out _thumbHandle) == 0)
                Refresh();
        }
    }
}