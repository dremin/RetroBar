using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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
        private TaskGroup Group;
        private TaskbarItem TaskbarItem;
        private TaskButtonStyleConverter StyleConverter = new TaskButtonStyleConverter();
        private ApplicationWindow.WindowState PressedWindowState = ApplicationWindow.WindowState.Inactive;

        private DelayedActivationHandler dragHandler;
        private bool _isLoaded;
        private bool _isMouseOverPopup = false;
        private System.Windows.Threading.DispatcherTimer _hoverTimer;
        private System.Windows.Threading.DispatcherTimer _closeTimer;
        private System.Windows.Window _parentWindow;

        // Track which TaskButton currently has its popup open for fast switching
        private static TaskButton _currentlyOpenTaskButton = null;

        public TaskButton()
        {
            InitializeComponent();
            SetStyle();

            _hoverTimer = new System.Windows.Threading.DispatcherTimer();
            _hoverTimer.Interval = TimeSpan.FromMilliseconds(ToolTipService.GetInitialShowDelay(this));
            _hoverTimer.Tick += HoverTimer_Tick;

            _closeTimer = new System.Windows.Threading.DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromSeconds(1); // Auto-close after 1 second
            _closeTimer.Tick += CloseTimer_Tick;
        }

        private void SetStyle()
        {
            MultiBinding multiBinding = new MultiBinding();
            multiBinding.Converter = StyleConverter;

            multiBinding.Bindings.Add(new Binding { RelativeSource = RelativeSource.Self });
            multiBinding.Bindings.Add(new Binding("State"));

            AppButton.SetBinding(StyleProperty, multiBinding);
        }

        private void ScrollIntoView()
        {
            if (Window != null && Window.State == ApplicationWindow.WindowState.Active)
            {
                BringIntoView();
            }
            else if (TaskbarItem != null && TaskbarItem.State == ApplicationWindow.WindowState.Active)
            {
                BringIntoView();
            }
            else if (Group != null && Group.State == ApplicationWindow.WindowState.Active)
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
            Window = DataContext as ApplicationWindow;
            Group = DataContext as TaskGroup;
            TaskbarItem = DataContext as TaskbarItem;

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;

            // Get parent window and subscribe to events
            _parentWindow = System.Windows.Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _parentWindow.Deactivated += ParentWindow_Deactivated;
                _parentWindow.PreviewMouseLeftButtonDown += ParentWindow_PreviewMouseDown;
            }

            // Sync GroupIndicatorButton visual state with AppButton
            if (GroupIndicatorButton != null)
            {
                AppButton.MouseEnter += (s, ev) => SyncGroupIndicatorStyle();
                AppButton.MouseLeave += (s, ev) => SyncGroupIndicatorStyle();
                AppButton.PreviewMouseDown += (s, ev) => SyncGroupIndicatorStyle();
                AppButton.PreviewMouseUp += (s, ev) => SyncGroupIndicatorStyle();
            }

            dragHandler = new DelayedActivationHandler(() =>
            {
                if (Group != null && Group.Windows.Count > 0)
                {
                    Group.Windows[0]?.BringToFront();
                }
                else
                {
                    Window?.BringToFront();
                }
            });

            if (Window != null)
            {
                Window.GetButtonRect += Window_GetButtonRect;
                Window.PropertyChanged += Window_PropertyChanged;
            }

            if (Group != null)
            {
                Group.PropertyChanged += Window_PropertyChanged;
                if (Group.Windows.Count > 0)
                {
                    Group.Windows[0].GetButtonRect += Window_GetButtonRect;
                }
            }

            if (Settings.Instance.SlideTaskbarButtons && Host?.Host?.Orientation == Orientation.Horizontal)
            {
                Animate();
            }

            _isLoaded = true;
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
                ScrollIntoView();
            }
        }

        private void TaskButton_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
            {
                return;
            }

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            dragHandler?.Dispose();

            if (_parentWindow != null)
            {
                _parentWindow.Deactivated -= ParentWindow_Deactivated;
                _parentWindow.PreviewMouseLeftButtonDown -= ParentWindow_PreviewMouseDown;
            }

            if (Window != null)
            {
                Window.GetButtonRect -= Window_GetButtonRect;
                Window.PropertyChanged -= Window_PropertyChanged;
            }

            if (Group != null)
            {
                Group.PropertyChanged -= Window_PropertyChanged;
                if (Group.Windows.Count > 0)
                {
                    Group.Windows[0].GetButtonRect -= Window_GetButtonRect;
                }
            }

            _isLoaded = false;
        }

        private void ParentWindow_Deactivated(object sender, EventArgs e)
        {
            // Close popup when window loses focus
            if (GroupPopup.IsOpen)
            {
                GroupPopup.IsOpen = false;

                // Clear the currently open reference if it's this button
                if (_currentlyOpenTaskButton == this)
                {
                    _currentlyOpenTaskButton = null;
                }
            }
        }

        private void ParentWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // If popup is open, check if click is outside it
            if (!GroupPopup.IsOpen)
            {
                return;
            }

            // Check if the click is within the popup or the button
            var popupChild = GroupPopup.Child as FrameworkElement;
            var clickedElement = e.OriginalSource as DependencyObject;

            if (clickedElement != null)
            {
                // Check if clicked element is within the popup
                if (popupChild != null && IsDescendantOf(clickedElement, popupChild))
                {
                    return; // Click is inside popup, don't close
                }

                // Check if clicked element is within the button itself
                if (IsDescendantOf(clickedElement, AppButton))
                {
                    return; // Click is on button, don't close
                }
            }

            // Click is outside, close the popup
            GroupPopup.IsOpen = false;

            // Clear the currently open reference if it's this button
            if (_currentlyOpenTaskButton == this)
            {
                _currentlyOpenTaskButton = null;
            }
        }

        private bool IsDescendantOf(DependencyObject child, DependencyObject parent)
        {
            if (child == parent)
                return true;

            DependencyObject currentParent = child;
            while (currentParent != null)
            {
                if (currentParent == parent)
                    return true;

                currentParent = System.Windows.Media.VisualTreeHelper.GetParent(currentParent);
            }

            return false;
        }

        private void AppButton_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ApplicationWindow contextWindow = Window ?? TaskbarItem?.RunningWindow;

            // For groups, use the first window for context menu
            if (contextWindow == null && Group != null && Group.Windows.Count > 0)
            {
                contextWindow = Group.Windows[0];
            }

            // For TaskbarItem groups, use the first window
            if (contextWindow == null && TaskbarItem != null && TaskbarItem.IsGroup && TaskbarItem.Windows.Count > 0)
            {
                contextWindow = TaskbarItem.Windows[0];
            }

            if (contextWindow == null)
            {
                return;
            }

            NativeMethods.WindowShowStyle wss = contextWindow.ShowStyle;
            int ws = contextWindow.WindowStyles;

            // disable window operations depending on current window state. originally tried implementing via bindings but found there is no notification we get regarding maximized state
            MaximizeMenuItem.IsEnabled = wss != NativeMethods.WindowShowStyle.ShowMaximized && (ws & (int)NativeMethods.WindowStyles.WS_MAXIMIZEBOX) != 0;
            MinimizeMenuItem.IsEnabled = wss != NativeMethods.WindowShowStyle.ShowMinimized && contextWindow.CanMinimize;
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

        private void CloseMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Group != null && Group.Windows.Count > 0)
            {
                Group.Windows[0]?.Close();
            }
            else if (TaskbarItem != null && TaskbarItem.IsGroup && TaskbarItem.Windows.Count > 0)
            {
                TaskbarItem.Windows[0]?.Close();
            }
            else
            {
                Window?.Close();
                TaskbarItem?.RunningWindow?.Close();
            }
        }

        private void EndTaskMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Group != null && Group.Windows.Count > 0)
            {
                ForceEndTask();
            }
            else if (Window != null || TaskbarItem?.RunningWindow != null)
            {
                ForceEndTask();
            }
        }

        private void ForceEndTask()
        {
            ApplicationWindow targetWindow = Window ?? TaskbarItem?.RunningWindow ?? (Group != null && Group.Windows.Count > 0 ? Group.Windows[0] : null);

            if (targetWindow == null) return;

            try
            {
                if (targetWindow.ProcId.HasValue && targetWindow.ProcId.Value != 0)
                {
                    // Don't kill RetroBar itself - just close the window gracefully
                    int currentProcId = Process.GetCurrentProcess().Id;
                    if (targetWindow.ProcId.Value == currentProcId)
                    {
                        targetWindow?.Close();
                        return;
                    }

                    Process process = Process.GetProcessById((int)targetWindow.ProcId.Value);
                    process.Kill();
                }
            }
            catch (Exception)
            {
                targetWindow?.Close();
            }
        }

        private void RestoreMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Group != null && Group.Windows.Count > 0)
            {
                Group.Windows[0]?.Restore();
            }
            else
            {
                Window?.Restore();
            }
        }

        private void MoveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Group != null && Group.Windows.Count > 0)
            {
                Group.Windows[0]?.Move();
            }
            else
            {
                Window?.Move();
            }
        }

        private void SizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Group != null && Group.Windows.Count > 0)
            {
                Group.Windows[0]?.Size();
            }
            else
            {
                Window?.Size();
            }
        }

        private void MinimizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Group != null && Group.Windows.Count > 0)
            {
                Group.Windows[0]?.Minimize();
            }
            else
            {
                Window?.Minimize();
            }
        }

        private void MaximizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Group != null && Group.Windows.Count > 0)
            {
                Group.Windows[0]?.Maximize();
            }
            else
            {
                Window?.Maximize();
            }
        }

        private void AppButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Handle TaskbarItem (combined pins and programs mode)
            if (TaskbarItem != null)
            {
                if (TaskbarItem.IsGroup)
                {
                    // Multiple windows - show popup
                    GroupPopup.IsOpen = true;
                }
                else if (TaskbarItem.IsRunning)
                {
                    // Has a running window
                    var window = TaskbarItem.RunningWindow;
                    if (window != null)
                    {
                        if (PressedWindowState == ApplicationWindow.WindowState.Active && window.CanMinimize)
                        {
                            window.Minimize();
                        }
                        else
                        {
                            window.BringToFront();
                        }
                    }
                }
                else if (TaskbarItem.IsPinned)
                {
                    // Pinned only (not running) - launch the application
                    if (TaskbarItem.PinnedItem != null)
                    {
                        ShellHelper.StartProcess(TaskbarItem.PinnedItem.Path);
                    }
                }
            }
            else if (Group != null && Group.IsGroup)
            {
                // For groups with multiple windows, show popup
                GroupPopup.IsOpen = true;
            }
            else if (Window != null)
            {
                if (PressedWindowState == ApplicationWindow.WindowState.Active && Window.CanMinimize)
                {
                    Window.Minimize();
                }
                else
                {
                    Window.BringToFront();
                }
            }
            else if (Group != null && Group.Windows.Count > 0)
            {
                // Single window in group
                var window = Group.Windows[0];
                if (PressedWindowState == ApplicationWindow.WindowState.Active && window.CanMinimize)
                {
                    window.Minimize();
                }
                else
                {
                    window.BringToFront();
                }
            }
        }

        private void AppButton_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (Group != null)
                {
                    PressedWindowState = Group.State;
                }
                else if (TaskbarItem != null)
                {
                    PressedWindowState = TaskbarItem.State;
                }
                else if (Window != null)
                {
                    PressedWindowState = Window.State;
                }
            }
        }

        private void AppButton_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                ApplicationWindow targetWindow = Window ?? TaskbarItem?.RunningWindow ?? (Group != null && Group.Windows.Count > 0 ? Group.Windows[0] : null);

                if (targetWindow == null || Settings.Instance.TaskMiddleClickAction == TaskMiddleClickOption.DoNothing)
                {
                    return;
                }
                if (Settings.Instance.TaskMiddleClickAction == TaskMiddleClickOption.CloseTask !=
                    (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                {
                    targetWindow?.Close();
                }
                else
                {
                    ShellHelper.StartProcess(targetWindow.IsUWP ? "appx:" + targetWindow.AppUserModelID : targetWindow.WinFileName);
                }
            }
        }

        private void GroupPopupButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ApplicationWindow window)
            {
                GroupPopup.IsOpen = false;

                // Clear the currently open reference if it's this button
                if (_currentlyOpenTaskButton == this)
                {
                    _currentlyOpenTaskButton = null;
                }

                window?.BringToFront();
            }
        }

        private void AppButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _closeTimer.Stop();
            if (Settings.Instance.ShowTaskThumbnails)
            {
                // If another preview is already open, switch to this one immediately (no delay)
                if (_currentlyOpenTaskButton != null && _currentlyOpenTaskButton != this)
                {
                    // Close the currently open popup
                    _currentlyOpenTaskButton.GroupPopup.IsOpen = false;
                    _currentlyOpenTaskButton = null;

                    // Open this popup immediately without waiting for hover timer
                    if (_parentWindow != null && !_parentWindow.IsActive)
                    {
                        _parentWindow.Activate();
                    }

                    GroupPopup.IsOpen = true;
                    _currentlyOpenTaskButton = this;
                    _isMouseOverPopup = false;
                }
                else
                {
                    // Normal behavior - wait for hover timer
                    _hoverTimer.Start();
                }
            }
        }

        private void AppButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _hoverTimer.Stop();

            // Start a timer to close the popup after a short delay
            // This gives the user time to move their mouse to the popup
            if (GroupPopup.IsOpen)
            {
                _closeTimer.Start();
            }
        }

        private void HoverTimer_Tick(object sender, EventArgs e)
        {
            _hoverTimer.Stop();

            if (Settings.Instance.ShowTaskThumbnails)
            {
                // Close any currently open popup (shouldn't happen with new logic, but just in case)
                if (_currentlyOpenTaskButton != null && _currentlyOpenTaskButton != this)
                {
                    _currentlyOpenTaskButton.GroupPopup.IsOpen = false;
                }

                // Focus the window so we can detect clicks outside
                if (_parentWindow != null && !_parentWindow.IsActive)
                {
                    _parentWindow.Activate();
                }

                GroupPopup.IsOpen = true;
                _currentlyOpenTaskButton = this;
                _isMouseOverPopup = false;
            }
        }

        private void HoverPreviewPopup_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _closeTimer.Stop();
            _isMouseOverPopup = true;
        }

        private void HoverPreviewContent_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _closeTimer.Stop();
            _isMouseOverPopup = true;
        }

        private void HoverPreviewContent_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _closeTimer.Start();
        }

        private void HoverPreviewPopup_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _closeTimer.Start();
        }

        private void CloseTimer_Tick(object sender, EventArgs e)
        {
            _closeTimer.Stop();

            // Check if mouse is actually over the button or popup
            if (AppButton.IsMouseOver || GroupPopup.IsMouseOver)
            {
                return;
            }

            _isMouseOverPopup = false;
            GroupPopup.IsOpen = false;

            // Clear the currently open reference if it's this button
            if (_currentlyOpenTaskButton == this)
            {
                _currentlyOpenTaskButton = null;
            }
        }

        private void GroupPopup_Opened(object sender, EventArgs e)
        {
            // Nothing needed here - deactivation and timer logic handle closing
        }

        private void GroupPopupChild_LostMouseCapture(object sender, MouseEventArgs e)
        {
            // Not used anymore - keeping for XAML compatibility
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.Theme))
            {
                SetStyle();
                SyncGroupIndicatorStyle();
            }
        }

        private void SyncGroupIndicatorStyle()
        {
            if (GroupIndicatorButton == null)
                return;

            // Force GroupIndicatorButton to use the same visual state as AppButton
            // by triggering the style converter update
            MultiBinding multiBinding = new MultiBinding();
            multiBinding.Converter = StyleConverter;
            multiBinding.Bindings.Add(new Binding { RelativeSource = RelativeSource.Self });
            multiBinding.Bindings.Add(new Binding("State"));

            GroupIndicatorButton.SetBinding(StyleProperty, multiBinding);

            // Manually trigger visual state based on AppButton state
            if (AppButton.IsPressed)
            {
                VisualStateManager.GoToState(GroupIndicatorButton, "Pressed", true);
            }
            else if (AppButton.IsMouseOver)
            {
                VisualStateManager.GoToState(GroupIndicatorButton, "MouseOver", true);
            }
            else
            {
                VisualStateManager.GoToState(GroupIndicatorButton, "Normal", true);
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
            BindingOperations.GetMultiBindingExpression(AppButton, StyleProperty).UpdateTarget();
        }

        private void AppButton_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Make GroupIndicatorButton 3px wider so it fully covers AppButton and peeks out 3px on right
            if (GroupIndicatorButton != null)
            {
                GroupIndicatorButton.Width = e.NewSize.Width + 3;
            }
        }
    }
}