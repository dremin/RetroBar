using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ManagedShell.Interop;
using ManagedShell.WindowsTasks;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for TaskThumbnail.xaml
    /// </summary>
    public partial class TaskThumbnail : UserControl
    {
        const double MAX_WIDTH = 180;
        const double MAX_HEIGHT = 120;
        const double TITLE_BAR_HEIGHT = 24;

        public double DpiScale = 1.0;

        private DispatcherTimer _toolTipTimer;
        private EventHandler _renderingHandler;

        public TaskThumbnail()
        {
            InitializeComponent();

            _toolTipTimer = new DispatcherTimer();
            _toolTipTimer.Tick += ToolTipTimer_Tick;
            _toolTipTimer.Interval = new TimeSpan(0, 0, 0, 0, ToolTipService.GetInitialShowDelay(this));
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

        public static DependencyProperty SourceWindowHandleProperty = DependencyProperty.Register(nameof(SourceWindowHandle), typeof(IntPtr), typeof(TaskThumbnail), new PropertyMetadata(new IntPtr()));

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

        public static DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(TaskThumbnail), new PropertyMetadata(""));

        public string Title
        {
            get
            {
                return (string)GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
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

                    // Transform to the root visual (Window) for proper absolute positioning
                    PresentationSource source = PresentationSource.FromVisual(this);
                    if (source == null)
                        return new NativeMethods.Rect(0, 0, 0, 0);

                    var rootVisual = source.RootVisual;
                    if (rootVisual == null)
                        return new NativeMethods.Rect(0, 0, 0, 0);

                    var generalTransform = TransformToAncestor(rootVisual);
                    var leftTopPoint = generalTransform.Transform(new Point(0, TITLE_BAR_HEIGHT));
                    return new NativeMethods.Rect(
                          (int)(leftTopPoint.X * DpiScale),
                          (int)(leftTopPoint.Y * DpiScale),
                          (int)(leftTopPoint.X * DpiScale) + (int)(MAX_WIDTH * DpiScale),
                          (int)(leftTopPoint.Y * DpiScale) + (int)((MAX_HEIGHT - TITLE_BAR_HEIGHT) * DpiScale)
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
                var clientAreaProps = new NativeMethods.DWM_THUMBNAIL_PROPERTIES
                {
                    dwFlags = NativeMethods.DWM_TNP_SOURCECLIENTAREAONLY,
                    fSourceClientAreaOnly = true
                };
                NativeMethods.DwmUpdateThumbnailProperties(_thumbHandle, ref clientAreaProps);

                NativeMethods.DwmQueryThumbnailSourceSize(_thumbHandle, out NativeMethods.PSIZE size);
                double aspectRatio = (double)size.x / size.y;

                var props = new NativeMethods.DWM_THUMBNAIL_PROPERTIES
                {
                    fVisible = true,
                    dwFlags = NativeMethods.DWM_TNP_VISIBLE | NativeMethods.DWM_TNP_RECTDESTINATION,
                    rcDestination = Rect
                };

                if (this != null)
                {
                    double previewHeight = MAX_HEIGHT - TITLE_BAR_HEIGHT;

                    if (size.x <= (MAX_WIDTH * DpiScale) && size.y <= (previewHeight * DpiScale))
                    {
                        // small, do not scale
                        Width = size.x / DpiScale;
                        Height = (size.y / DpiScale) + TITLE_BAR_HEIGHT;
                        props.rcDestination.Right = props.rcDestination.Left + size.x;
                        props.rcDestination.Bottom = props.rcDestination.Top + size.y;
                    }
                    else
                    {
                        // large, scale preserving aspect ratio
                        double controlAspectRatio = MAX_WIDTH / previewHeight;

                        if (aspectRatio > controlAspectRatio)
                        {
                            // wide
                            int height = (int)(MAX_WIDTH / aspectRatio);

                            Width = MAX_WIDTH;
                            Height = height + TITLE_BAR_HEIGHT;
                            props.rcDestination.Bottom = props.rcDestination.Top + (int)(height * DpiScale);
                        }
                        else if (aspectRatio < controlAspectRatio)
                        {
                            // tall
                            int width = (int)(previewHeight * aspectRatio);

                            Width = width;
                            Height = MAX_HEIGHT;
                            props.rcDestination.Right = props.rcDestination.Left + (int)(width * DpiScale);
                        }
                    }
                }

                if (this != null)
                    NativeMethods.DwmUpdateThumbnailProperties(_thumbHandle, ref props);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_renderingHandler != null)
            {
                CompositionTarget.Rendering -= _renderingHandler;
                _renderingHandler = null;
            }

            if (_thumbHandle != IntPtr.Zero)
            {
                NativeMethods.DwmUnregisterThumbnail(_thumbHandle);
                _thumbHandle = IntPtr.Zero;
            }

            _toolTipTimer.Stop();
            if (ToolTip is ToolTip tip)
            {
                tip.IsOpen = false;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                DpiScale = source.CompositionTarget.TransformToDevice.M11;
            }

            // Try immediate registration first
            if (NativeMethods.DwmIsCompositionEnabled() && SourceWindowHandle != IntPtr.Zero && Handle != IntPtr.Zero && NativeMethods.DwmRegisterThumbnail(Handle, SourceWindowHandle, out _thumbHandle) == 0)
            {
                Refresh();
                // once loaded, we need to refresh the thumbnail...
                _renderingHandler = (s, a) => Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(Refresh));
                CompositionTarget.Rendering += _renderingHandler;
            }
            else
            {
                // If immediate registration failed, retry after a delay
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
                {
                    if (_thumbHandle == IntPtr.Zero && NativeMethods.DwmIsCompositionEnabled() && SourceWindowHandle != IntPtr.Zero && Handle != IntPtr.Zero && NativeMethods.DwmRegisterThumbnail(Handle, SourceWindowHandle, out _thumbHandle) == 0)
                    {
                        Refresh();
                        _renderingHandler = (s, a) => Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(Refresh));
                        CompositionTarget.Rendering += _renderingHandler;
                    }
                }));
            }

            // Show tooltip immediately in popups, no delay needed
            if (ToolTip is ToolTip tip)
            {
                tip.PlacementTarget = this;
                tip.IsOpen = true;
            }
        }

        private void ToolTipTimer_Tick(object sender, EventArgs e)
        {
            if (ToolTip is ToolTip tip)
            {
                tip.PlacementTarget = this;
                tip.IsOpen = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the window associated with this thumbnail
            if (DataContext is ApplicationWindow window)
            {
                window?.Close();
            }
        }
    }
}