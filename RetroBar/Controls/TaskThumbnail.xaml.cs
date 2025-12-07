using System;
using System.ComponentModel;
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
        const double MAX_HEIGHT = 150;
        const double TITLE_BAR_HEIGHT = 24;
        const double FOOTER_HEIGHT = 30;

        public double DpiScale = 1.0;

        private DispatcherTimer _toolTipTimer;
        private EventHandler _renderingHandler;

        public TaskThumbnail()
        {
            InitializeComponent();

            _toolTipTimer = new DispatcherTimer();
            _toolTipTimer.Tick += ToolTipTimer_Tick;
            _toolTipTimer.Interval = new TimeSpan(0, 0, 0, 0, ToolTipService.GetInitialShowDelay(this));

            // Subscribe to DataContext changes to update footer visibility
            DataContextChanged += TaskThumbnail_DataContextChanged;
        }

        private void TaskThumbnail_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old DataContext if it implements INotifyPropertyChanged
            if (e.OldValue is INotifyPropertyChanged oldNotify)
            {
                oldNotify.PropertyChanged -= DataContext_PropertyChanged;
            }

            // Subscribe to new DataContext if it implements INotifyPropertyChanged
            if (e.NewValue is INotifyPropertyChanged newNotify)
            {
                newNotify.PropertyChanged += DataContext_PropertyChanged;
            }

            UpdateFooterVisibility();
        }

        private void DataContext_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string debugPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "thumb_debug.txt");

            // Update UI when ThumbnailButtons or ThumbnailButtonImageList changes
            if (e.PropertyName == "ThumbnailButtons" || e.PropertyName == "ThumbnailButtonImageList")
            {
                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: DataContext_PropertyChanged - {e.PropertyName} changed\n");

                // The XAML binding will automatically update when the property changes
                // We just need to update footer visibility
                UpdateFooterVisibility();
            }
        }

        private void UpdateFooterVisibility()
        {
            string debugPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "thumb_debug.txt");

            if (ThumbnailButtonsContainer == null)
            {
                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: UpdateFooterVisibility - ThumbnailButtonsContainer is NULL\n");
                return;
            }

            ThumbnailButton[] buttons = null;
            string windowTitle = "unknown";

            if (DataContext is ApplicationWindow appWindow)
            {
                buttons = appWindow.ThumbnailButtons;
                windowTitle = appWindow.Title;
            }
            else if (DataContext is Utilities.TaskbarItem taskbarItem && taskbarItem.RunningWindow != null)
            {
                buttons = taskbarItem.RunningWindow.ThumbnailButtons;
                windowTitle = taskbarItem.Title;
            }
            else if (DataContext is Utilities.TaskGroup taskGroup && taskGroup.Windows.Count > 0)
            {
                buttons = taskGroup.ThumbnailButtons;
                windowTitle = taskGroup.Title;
            }

            System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: UpdateFooterVisibility - Window: {windowTitle}, Buttons: {buttons?.Length ?? 0}\n");

            // Hide footer if no buttons or all buttons are hidden (Flags == 8 is THBF_HIDDEN)
            bool hasVisibleButtons = false;
            if (buttons != null && buttons.Length > 0)
            {
                foreach (var button in buttons)
                {
                    System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}:   Button ID {button.Id}, Flags: {button.Flags}\n");
                    if (button.Flags != NativeMethods.THUMBBUTTONFLAGS.THBF_HIDDEN)
                    {
                        hasVisibleButtons = true;
                        break;
                    }
                }
            }

            System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: UpdateFooterVisibility - hasVisibleButtons: {hasVisibleButtons}, Setting Visibility to: {(hasVisibleButtons ? "Visible" : "Collapsed")}\n");
            ThumbnailButtonsContainer.Visibility = hasVisibleButtons ? Visibility.Visible : Visibility.Collapsed;

            // Refresh the thumbnail size calculations now that footer visibility changed
            Refresh();
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

                    // Only reserve space for footer if it's actually visible
                    double actualFooterHeight = (ThumbnailButtonsContainer != null && ThumbnailButtonsContainer.Visibility == Visibility.Visible) ? FOOTER_HEIGHT : 0;

                    var generalTransform = TransformToAncestor(rootVisual);
                    var leftTopPoint = generalTransform.Transform(new Point(0, TITLE_BAR_HEIGHT));
                    return new NativeMethods.Rect(
                          (int)(leftTopPoint.X * DpiScale),
                          (int)(leftTopPoint.Y * DpiScale),
                          (int)(leftTopPoint.X * DpiScale) + (int)(MAX_WIDTH * DpiScale),
                          (int)(leftTopPoint.Y * DpiScale) + (int)((MAX_HEIGHT - TITLE_BAR_HEIGHT - actualFooterHeight) * DpiScale)
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
                    // Only reserve space for footer if it's actually visible
                    double actualFooterHeight = (ThumbnailButtonsContainer != null && ThumbnailButtonsContainer.Visibility == Visibility.Visible) ? FOOTER_HEIGHT : 0;
                    double previewHeight = MAX_HEIGHT - TITLE_BAR_HEIGHT - actualFooterHeight;

                    if (size.x <= (MAX_WIDTH * DpiScale) && size.y <= (previewHeight * DpiScale))
                    {
                        // small, do not scale
                        Width = size.x / DpiScale;
                        Height = (size.y / DpiScale) + TITLE_BAR_HEIGHT + actualFooterHeight;
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
                            Height = height + TITLE_BAR_HEIGHT + actualFooterHeight;
                            props.rcDestination.Bottom = props.rcDestination.Top + (int)(height * DpiScale);
                        }
                        else if (aspectRatio < controlAspectRatio)
                        {
                            // tall
                            int width = (int)(previewHeight * aspectRatio);

                            Width = width;
                            Height = previewHeight + TITLE_BAR_HEIGHT + actualFooterHeight;
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
            // Unsubscribe from DataContext PropertyChanged events
            if (DataContext is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged -= DataContext_PropertyChanged;
            }

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

            // Update footer visibility based on whether there are visible buttons
            UpdateFooterVisibility();

            // Debug: Check DataContext and ThumbnailButtons
            string debugPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "thumb_debug.txt");
            try
            {
                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: TaskThumbnail.Loaded - DataContext type: {DataContext?.GetType().Name ?? "null"}\n");
                if (DataContext is ApplicationWindow win)
                {
                    System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: TaskThumbnail.Loaded - Window: {win.Title}, ThumbnailButtons: {win.ThumbnailButtons?.Length ?? 0}\n");
                    if (win.ThumbnailButtons != null && win.ThumbnailButtons.Length > 0)
                    {
                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: TaskThumbnail.Loaded - Button 0 ID: {win.ThumbnailButtons[0].Id}\n");
                    }
                }
                else if (DataContext is Utilities.TaskbarItem taskbarItem && taskbarItem.RunningWindow != null)
                {
                    System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: TaskThumbnail.Loaded - TaskbarItem Window: {taskbarItem.Title}, ThumbnailButtons: {taskbarItem.RunningWindow.ThumbnailButtons?.Length ?? 0}\n");
                    if (taskbarItem.RunningWindow.ThumbnailButtons != null && taskbarItem.RunningWindow.ThumbnailButtons.Length > 0)
                    {
                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: TaskThumbnail.Loaded - Button 0 ID: {taskbarItem.RunningWindow.ThumbnailButtons[0].Id}\n");
                    }
                }
            }
            catch { }

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
            ApplicationWindow window = null;

            if (DataContext is ApplicationWindow appWindow)
            {
                window = appWindow;
            }
            else if (DataContext is Utilities.TaskbarItem taskbarItem)
            {
                window = taskbarItem.RunningWindow;
            }
            else if (DataContext is Utilities.TaskGroup taskGroup && taskGroup.Windows.Count > 0)
            {
                window = taskGroup.Windows[0];
            }

            window?.Close();
        }

        private void PreviewArea_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Bring the window to front when clicking the preview area
            ApplicationWindow window = null;

            if (DataContext is ApplicationWindow appWindow)
            {
                window = appWindow;
            }
            else if (DataContext is Utilities.TaskbarItem taskbarItem)
            {
                window = taskbarItem.RunningWindow;
            }
            else if (DataContext is Utilities.TaskGroup taskGroup && taskGroup.Windows.Count > 0)
            {
                // For groups, this shouldn't happen as groups show multiple thumbnails
                // But if it does, activate the first window
                window = taskGroup.Windows[0];
            }

            window?.BringToFront();

            // Close the popup after activating the window
            var popup = FindVisualParent<System.Windows.Controls.Primitives.Popup>(this);
            if (popup != null)
            {
                popup.IsOpen = false;
            }
        }

        /// <summary>
        /// Finds a parent of a specific type in the visual tree
        /// </summary>
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            if (parentObject is T parent)
                return parent;

            return FindVisualParent<T>(parentObject);
        }

        private void ThumbButton_Click(object sender, RoutedEventArgs e)
        {
            string debugPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "thumb_debug.txt");
            try
            {
                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: ThumbButton_Click called\n");

                if (sender is Button button && button.Tag is uint buttonId)
                {
                    System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Button ID: {buttonId}\n");

                    ApplicationWindow window = null;
                    if (DataContext is ApplicationWindow appWindow)
                    {
                        window = appWindow;
                    }
                    else if (DataContext is Utilities.TaskbarItem taskbarItem)
                    {
                        window = taskbarItem.RunningWindow;
                    }
                    else if (DataContext is Utilities.TaskGroup taskGroup && taskGroup.Windows.Count > 0)
                    {
                        window = taskGroup.Windows[0];
                    }

                    if (window != null)
                    {
                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Sending WM_COMMAND to window: {window.Title}, HWND: {window.Handle}\n");
                        // Send WM_COMMAND message to the window with THBN_CLICKED notification
                        // HIWORD(wParam) = THBN_CLICKED (0x1800), LOWORD(wParam) = button ID
                        const int WM_COMMAND = 0x0111;
                        const int THBN_CLICKED = 0x1800;
                        IntPtr wParam = new IntPtr((THBN_CLICKED << 16) | (int)buttonId);
                        IntPtr result = NativeMethods.SendMessage(window.Handle, WM_COMMAND, wParam, IntPtr.Zero);
                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: SendMessage result: {result}\n");
                    }
                    else
                    {
                        System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: Could not get ApplicationWindow from DataContext: {DataContext?.GetType().Name}\n");
                    }
                }
                else
                {
                    System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: sender is not Button or Tag is not uint. Sender: {sender?.GetType().Name}, Tag: {(sender as Button)?.Tag?.GetType().Name}\n");
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(debugPath, $"{DateTime.Now}: ERROR in ThumbButton_Click: {ex.Message}\n");
            }
        }
    }
}