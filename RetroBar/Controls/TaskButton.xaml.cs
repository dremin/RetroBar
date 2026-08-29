using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ManagedShell.Common.Helpers;
using ManagedShell.Interop;
using ManagedShell.WindowsTasks;
using RetroBar.Converters;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for TaskButton.xaml
    /// </summary>
    public partial class TaskButton : UserControl
    {
        public static DependencyProperty HostProperty = DependencyProperty.Register(nameof(Host), typeof(TaskList), typeof(TaskButton));

        public TaskList Host
        {
            get { return (TaskList)GetValue(HostProperty); }
            set { SetValue(HostProperty, value); }
        }

        private ApplicationWindow Window;
        private TaskButtonStyleConverter StyleConverter = new TaskButtonStyleConverter();
        private ApplicationWindow.WindowState PressedWindowState = ApplicationWindow.WindowState.Inactive;

        private DelayedActivationHandler dragHandler;
        private bool _isLoaded;
        private INotifyCollectionChanged _subscribedCollection;
        private List<ApplicationWindow> _subscribedWindows = new List<ApplicationWindow>();
        private bool _isGroupMenuOpen;
        private bool _allowOpenGroupMenu;

        public static readonly DependencyProperty TasksProperty = DependencyProperty.Register("Tasks", typeof(IEnumerable), typeof(TaskButton), new PropertyMetadata(null, OnTasksChanged));

        private static void OnTasksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TaskButton tb)
            {
                tb.UpdateDisplayIcon();
            }
        }

        public IEnumerable Tasks
        {
            get => (IEnumerable)GetValue(TasksProperty);
            set => SetValue(TasksProperty, value);
        }

        public TaskButton()
        {
            InitializeComponent();
            SetStyle();
            DataContextChanged += (s, e) => UpdateDisplayIcon();
        }

        private void SetStyle()
        {
            ApplicationWindow.WindowState state = ApplicationWindow.WindowState.Inactive;
            
            if (Tasks != null)
            {
                var windows = Tasks.OfType<ApplicationWindow>().ToList();
                if (windows.Any(w => w.State == ApplicationWindow.WindowState.Active))
                {
                    state = ApplicationWindow.WindowState.Active;
                }
                else if (windows.Any(w => w.State == ApplicationWindow.WindowState.Flashing))
                {
                    state = ApplicationWindow.WindowState.Flashing;
                }
            }
            else if (Window != null)
            {
                state = Window.State;
            }

            var fxStyle = this.FindResource("TaskButton") as Style;
            if (state == ApplicationWindow.WindowState.Active)
            {
                fxStyle = this.FindResource("TaskButtonActive") as Style;
            }
            else if (state == ApplicationWindow.WindowState.Flashing)
            {
                fxStyle = this.FindResource("TaskButtonFlashing") as Style;
            }

            if (AppButton.ContextMenu?.IsOpen == true || _isGroupMenuOpen)
            {
                fxStyle = this.FindResource("TaskButtonActive") as Style;
            }

            AppButton.Style = fxStyle;
        }

        private void ScrollIntoView()
        {
            if (Window == null)
            {
                return;
            }

            ApplicationWindow.WindowState state = ApplicationWindow.WindowState.Inactive;
            if (Tasks != null)
            {
                var windows = Tasks.OfType<ApplicationWindow>().ToList();
                if (windows.Any(w => w.State == ApplicationWindow.WindowState.Active))
                {
                    state = ApplicationWindow.WindowState.Active;
                }
            }
            else
            {
                state = Window.State;
            }

            if (state == ApplicationWindow.WindowState.Active)
            {
                BringIntoView();
            }
        }

        private void Animate()
        {
            var ease = new SineEase();
            ease.EasingMode = EasingMode.EaseInOut;

            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 0;
            animation.To = Host?.ButtonWidth ?? ActualWidth;
            animation.Duration = new Duration(TimeSpan.FromMilliseconds(250));
            animation.FillBehavior = FillBehavior.Stop;
            animation.EasingFunction = ease;
            Storyboard.SetTarget(animation, this);
            Storyboard.SetTargetProperty(animation, new PropertyPath(WidthProperty));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void TaskButton_OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            if (DataContext is CollectionViewGroup group)
            {
                Window = group.Items.Count > 0 ? group.Items[0] as ApplicationWindow : null;
                AppButton.DataContext = Window;
                
                if (group.Items is INotifyCollectionChanged collectionChanged)
                {
                    _subscribedCollection = collectionChanged;
                    _subscribedCollection.CollectionChanged -= GroupItems_CollectionChanged;
                    _subscribedCollection.CollectionChanged += GroupItems_CollectionChanged;
                }
                foreach (ApplicationWindow w in group.Items)
                {
                    if (!_subscribedWindows.Contains(w))
                    {
                        w.PropertyChanged -= Window_PropertyChanged;
                        w.PropertyChanged += Window_PropertyChanged;
                        w.GetButtonRect -= Window_GetButtonRect;
                        w.GetButtonRect += Window_GetButtonRect;
                        _subscribedWindows.Add(w);
                    }
                }
            }
            else
            {
                Window = DataContext as ApplicationWindow;
                AppButton.DataContext = Window;
                if (Window != null && !_subscribedWindows.Contains(Window))
                {
                    Window.PropertyChanged -= Window_PropertyChanged;
                    Window.PropertyChanged += Window_PropertyChanged;
                    Window.GetButtonRect -= Window_GetButtonRect;
                    Window.GetButtonRect += Window_GetButtonRect;
                    _subscribedWindows.Add(Window);
                }
            }

            UpdateDisplayIcon();
            Settings.Instance.PropertyChanged += Settings_PropertyChanged;

            dragHandler = new DelayedActivationHandler(() =>
            {
                Window?.BringToFront();
            });

            if (Settings.Instance.SlideTaskbarButtons && Host?.Host?.Orientation == Orientation.Horizontal)
            {
                Animate();
            }

            if (AppButton.ToolTip is ToolTip toolTip)
            {
                toolTip.CustomPopupPlacementCallback = new System.Windows.Controls.Primitives.CustomPopupPlacementCallback(ToolTipCustomPlacement);
            }

            _isLoaded = true;
            SetStyle();
        }

        private System.Windows.Controls.Primitives.CustomPopupPlacement[] ToolTipCustomPlacement(Size popupSize, Size targetSize, Point offset)
        {
            double x = (targetSize.Width - popupSize.Width) / 2.0;
            double y = -popupSize.Height - 5;
            System.Windows.Controls.Primitives.PopupPrimaryAxis axis = System.Windows.Controls.Primitives.PopupPrimaryAxis.Horizontal;
            
            if (Settings.Instance.Edge == ManagedShell.AppBar.AppBarEdge.Top)
            {
                y = targetSize.Height + 5;
            }

            return new System.Windows.Controls.Primitives.CustomPopupPlacement[] {
                new System.Windows.Controls.Primitives.CustomPopupPlacement(new Point(x, y), axis)
            };
        }

        private void GroupItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (ApplicationWindow w in e.OldItems)
                {
                    w.PropertyChanged -= Window_PropertyChanged;
                    w.GetButtonRect -= Window_GetButtonRect;
                    _subscribedWindows.Remove(w);
                }
            }
            if (e.NewItems != null)
            {
                foreach (ApplicationWindow w in e.NewItems)
                {
                    if (!_subscribedWindows.Contains(w))
                    {
                        w.PropertyChanged += Window_PropertyChanged;
                        w.GetButtonRect += Window_GetButtonRect;
                        _subscribedWindows.Add(w);
                    }
                }
            }
            // Update the main Window binding to point to the remaining active window
            if (DataContext is CollectionViewGroup group)
            {
                Window = group.Items.Count > 0 ? group.Items[0] as ApplicationWindow : null;
                AppButton.DataContext = Window;
            }

            UpdateDisplayIcon();
            SetStyle();
        }

        private void Window_GetButtonRect(ref NativeMethods.ShortRect rect)
        {
            if (Host?.Host?.Screen.Primary != true && Settings.Instance.MultiMonMode != MultiMonOption.SameAsWindow)
            {
                // If there are multiple instances of a button, use the button on the primary display only
                return;
            }

            Point buttonTopLeft = PointToScreen(new Point(0, 0));
            Point buttonBottomRight = PointToScreen(new Point(ActualWidth, ActualHeight));
            rect.Top = (short)buttonTopLeft.Y;
            rect.Left = (short)buttonTopLeft.X;
            rect.Bottom = (short)buttonBottomRight.Y;
            rect.Right = (short)buttonBottomRight.X;
        }

        private void Window_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
            {
                Dispatcher.InvokeAsync(() =>
                {
                    ScrollIntoView();
                    SetStyle();
                });
            }
            else if (e.PropertyName == "Icon")
            {
                Dispatcher.InvokeAsync(() =>
                {
                    UpdateDisplayIcon();
                });
            }
        }

        private void UpdateDisplayIcon()
        {
            if (Window == null)
            {
                DisplayIcon = null;
                return;
            }

            if (Tasks != null)
            {
                var windows = Tasks.OfType<ApplicationWindow>().ToList();
                if (windows.Count > 1 && !string.IsNullOrEmpty(Window.WinFileName))
                {
                    bool useLargeIcons = Settings.Instance.TaskbarScale > 1 || (Application.Current.FindResource("UseLargeIcons") as bool? ?? false);
                    DisplayIcon = IconImageConverter.GetImageFromAssociatedIcon(Window.WinFileName, useLargeIcons ? ManagedShell.Common.Enums.IconSize.Large : ManagedShell.Common.Enums.IconSize.Small);
                    return;
                }
            }

            DisplayIcon = Window.Icon;
        }

        private void TaskButton_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
            {
                return;
            }

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            dragHandler?.Dispose();

            if (_subscribedCollection != null)
            {
                _subscribedCollection.CollectionChanged -= GroupItems_CollectionChanged;
                _subscribedCollection = null;
            }

            foreach (ApplicationWindow w in _subscribedWindows)
            {
                w.GetButtonRect -= Window_GetButtonRect;
                w.PropertyChanged -= Window_PropertyChanged;
            }
            _subscribedWindows.Clear();

            _isLoaded = false;
        }

        private void AppButton_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var windows = Tasks?.OfType<ApplicationWindow>().ToList();
            bool isGroup = windows != null && windows.Count > 1;

            if (isGroup)
            {
                RestoreMenuItem.Visibility = Visibility.Collapsed;
                MoveMenuItem.Visibility = Visibility.Collapsed;
                SizeMenuItem.Visibility = Visibility.Collapsed;
                MinimizeMenuItem.Visibility = Visibility.Collapsed;
                MaximizeMenuItem.Visibility = Visibility.Collapsed;
                EndTaskMenuItem.Visibility = Visibility.Collapsed;
                CloseMenuItem.Visibility = Visibility.Collapsed;
                SingleSeparator.Visibility = Visibility.Collapsed;

                MinimizeGroupMenuItem.Visibility = Visibility.Visible;
                GroupSeparator.Visibility = Visibility.Visible;
                CloseGroupMenuItem.Visibility = Visibility.Visible;

                MinimizeGroupMenuItem.IsEnabled = windows.Any(w => w.CanMinimize && w.ShowStyle != NativeMethods.WindowShowStyle.ShowMinimized);
                CloseGroupMenuItem.IsEnabled = true;
                CloseGroupMenuItem.FontWeight = FontWeights.Normal;
                return;
            }

            MinimizeGroupMenuItem.Visibility = Visibility.Collapsed;
            GroupSeparator.Visibility = Visibility.Collapsed;
            CloseGroupMenuItem.Visibility = Visibility.Collapsed;

            RestoreMenuItem.Visibility = Visibility.Visible;
            MoveMenuItem.Visibility = Visibility.Visible;
            SizeMenuItem.Visibility = Visibility.Visible;
            MinimizeMenuItem.Visibility = Visibility.Visible;
            MaximizeMenuItem.Visibility = Visibility.Visible;
            EndTaskMenuItem.Visibility = Settings.Instance.ShowEndTaskButton ? Visibility.Visible : Visibility.Collapsed;
            CloseMenuItem.Visibility = Visibility.Visible;
            SingleSeparator.Visibility = Visibility.Visible;

            if (Window == null)
            {
                return;
            }

            NativeMethods.WindowShowStyle wss = Window.ShowStyle;
            int ws = Window.WindowStyles;

            // disable window operations depending on current window state. originally tried implementing via bindings but found there is no notification we get regarding maximized state
            MaximizeMenuItem.IsEnabled = wss != NativeMethods.WindowShowStyle.ShowMaximized && (ws & (int)NativeMethods.WindowStyles.WS_MAXIMIZEBOX) != 0;
            MinimizeMenuItem.IsEnabled = wss != NativeMethods.WindowShowStyle.ShowMinimized && Window.CanMinimize;
            if (RestoreMenuItem.IsEnabled = wss != NativeMethods.WindowShowStyle.ShowNormal)
            {
                CloseMenuItem.FontWeight = FontWeights.Normal;
                RestoreMenuItem.FontWeight = FontWeights.Bold;
            }
            if (!RestoreMenuItem.IsEnabled || RestoreMenuItem.IsEnabled && !MaximizeMenuItem.IsEnabled)
            {
                CloseMenuItem.FontWeight = FontWeights.Bold;
                RestoreMenuItem.FontWeight = FontWeights.Normal;
            }
            MoveMenuItem.IsEnabled = wss == NativeMethods.WindowShowStyle.ShowNormal;
            SizeMenuItem.IsEnabled = wss == NativeMethods.WindowShowStyle.ShowNormal && (ws & (int)NativeMethods.WindowStyles.WS_MAXIMIZEBOX) != 0;
        }

        private void MinimizeGroupMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Tasks != null)
            {
                foreach (ApplicationWindow win in Tasks.OfType<ApplicationWindow>())
                {
                    if (win.CanMinimize)
                    {
                        win.Minimize();
                    }
                }
            }
        }

        private void CloseGroupMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Tasks != null)
            {
                var windows = Tasks.OfType<ApplicationWindow>().ToList();
                foreach (ApplicationWindow win in windows)
                {
                    win.Close();
                }
            }
        }

        private void CloseMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Close();
        }

        private void EndTaskMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Window != null)
            {
                ForceEndTask();
            }
        }

        private void ForceEndTask()
        {
            try
            {
                if (Window.ProcId.HasValue && Window.ProcId.Value != 0)
                {
                    // Don't kill RetroBar itself - just close the window gracefully
                    int currentProcId = Process.GetCurrentProcess().Id;
                    if (Window.ProcId.Value == currentProcId)
                    {
                        Window?.Close();
                        return;
                    }

                    Process process = Process.GetProcessById((int)Window.ProcId.Value);
                    process.Kill();
                }
            }
            catch (Exception)
            {
                Window?.Close();
            }
        }

        private void RestoreMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Restore();
        }

        private void MoveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Move();
        }

        private void SizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Size();
        }

        private void MinimizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Minimize();
        }

        private void MaximizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Window?.Maximize();
        }



        private void OpenGroupMenu(List<ApplicationWindow> windows)
        {
            ContextMenu groupMenu = new ContextMenu();
            
            foreach (var window in windows)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = window.Title;
                
                Image icon = new Image();
                icon.SetBinding(Image.SourceProperty, new Binding(nameof(window.Icon)) { Source = window });
                icon.Width = 16;
                icon.Height = 16;
                menuItem.Icon = icon;

                // We need a local copy of the window reference for the closure
                var localWindow = window;
                menuItem.Click += (s, ev) => 
                {
                    if (localWindow.State == ApplicationWindow.WindowState.Active && localWindow.CanMinimize)
                    {
                        localWindow.Minimize();
                    }
                    else
                    {
                        localWindow.BringToFront();
                    }
                };
                groupMenu.Items.Add(menuItem);
            }

            groupMenu.PlacementTarget = AppButton;
            
            if (Settings.Instance.Edge == ManagedShell.AppBar.AppBarEdge.Top)
                groupMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            else if (Settings.Instance.Edge == ManagedShell.AppBar.AppBarEdge.Left)
                groupMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
            else if (Settings.Instance.Edge == ManagedShell.AppBar.AppBarEdge.Right)
                groupMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Left;
            else
                groupMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;

            // Bind TextRenderingMode to match other menus if possible
            Binding textRenderingBinding = new Binding("AllowFontSmoothingMenu");
            textRenderingBinding.Source = Settings.Instance;
            textRenderingBinding.Converter = FindResource("menuTextRenderingModeConverter") as IValueConverter;
            if (textRenderingBinding.Converter != null)
            {
                groupMenu.SetBinding(System.Windows.Media.TextOptions.TextRenderingModeProperty, textRenderingBinding);
            }

            groupMenu.Closed += (s, ev) => 
            {
                _isGroupMenuOpen = false;
                SetStyle();
            };

            _isGroupMenuOpen = true;
            groupMenu.IsOpen = true;
            SetStyle();
        }

        private void AppButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_allowOpenGroupMenu)
            {
                return;
            }

            if (Tasks != null)
            {
                var windows = Tasks.OfType<ApplicationWindow>().ToList();
                if (windows.Count > 1)
                {
                    OpenGroupMenu(windows);
                    return;
                }
            }

            // Fallback for single windows or ungrouped task buttons
            ApplicationWindow targetWindow = Window;

            if (Tasks != null)
            {
                var windows = Tasks.OfType<ApplicationWindow>().ToList();
                if (windows.Count == 1)
                {
                    targetWindow = windows[0];
                }
            }

            if (PressedWindowState == ApplicationWindow.WindowState.Active && targetWindow?.CanMinimize == true)
            {
                targetWindow?.Minimize();
            }
            else
            {
                targetWindow?.BringToFront();
            }
        }

        private void AppButton_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                PressedWindowState = Window?.State ?? ApplicationWindow.WindowState.Inactive;
                _allowOpenGroupMenu = !_isGroupMenuOpen;
            }
        }

        private void AppButton_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                if (Window == null || Settings.Instance.TaskMiddleClickAction == TaskMiddleClickOption.DoNothing)
                {
                    return;
                }
                if (Settings.Instance.TaskMiddleClickAction == TaskMiddleClickOption.CloseTask !=
                    (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                {
                    Window?.Close();
                }
                else
                {
                    ShellHelper.StartProcess(Window.IsUWP ? "appx:" + Window.AppUserModelID : Window.WinFileName);
                }
            }
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Theme))
            {
                SetStyle();
            }
            else if (e.PropertyName == nameof(Settings.TaskbarScale))
            {
                Dispatcher.InvokeAsync(() =>
                {
                    UpdateDisplayIcon();
                });
            }
        }

        private void AppButton_OnDragEnter(object sender, DragEventArgs e)
        {
            dragHandler?.OnDragEnter(e);
        }

        private void AppButton_OnDragLeave(object sender, DragEventArgs e)
        {
            dragHandler?.OnDragLeave();
        }

        private void ContextMenu_OpenedOrClosed(object sender, RoutedEventArgs e)
        {
            SetStyle();
        }

        public static readonly DependencyProperty DisplayIconProperty = DependencyProperty.Register("DisplayIcon", typeof(ImageSource), typeof(TaskButton));

        public ImageSource DisplayIcon
        {
            get => (ImageSource)GetValue(DisplayIconProperty);
            set => SetValue(DisplayIconProperty, value);
        }
    }
}