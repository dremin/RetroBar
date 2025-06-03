﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
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

        private bool _isLoaded;

        public TaskButton()
        {
            InitializeComponent();
            SetStyle();
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
            if (Window == null)
            {
                return;
            }

            if (Window.State == ApplicationWindow.WindowState.Active)
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

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;

            // drag support - delayed activation using system setting
            dragTimer = new DispatcherTimer { Interval = SystemParameters.MouseHoverTime };
            dragTimer.Tick += dragTimer_Tick;

            if (Window != null)
            {
                Window.GetButtonRect += Window_GetButtonRect;
                Window.PropertyChanged += Window_PropertyChanged;
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

            if (Window != null)
            {
                Window.GetButtonRect -= Window_GetButtonRect;
                Window.PropertyChanged -= Window_PropertyChanged;
            }

            _isLoaded = false;
        }

        private void AppButton_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (Window == null)
            {
                return;
            }

            NativeMethods.WindowShowStyle wss = Window.ShowStyle;
            int ws = Window.WindowStyles;

            // disable window operations depending on current window state. originally tried implementing via bindings but found there is no notification we get regarding maximized state
            MaximizeMenuItem.StaysOpenOnClick = !(wss != NativeMethods.WindowShowStyle.ShowMaximized && (ws & (int)NativeMethods.WindowStyles.WS_MAXIMIZEBOX) != 0);
            MinimizeMenuItem.StaysOpenOnClick = !(wss != NativeMethods.WindowShowStyle.ShowMinimized && (ws & (int)NativeMethods.WindowStyles.WS_MINIMIZEBOX) != 0);
            if (!(RestoreMenuItem.StaysOpenOnClick = wss == NativeMethods.WindowShowStyle.ShowNormal))
            {
                CloseMenuItem.FontWeight = FontWeights.Normal;
                RestoreMenuItem.FontWeight = FontWeights.Bold;
            }
            if (RestoreMenuItem.StaysOpenOnClick || !RestoreMenuItem.StaysOpenOnClick && MaximizeMenuItem.StaysOpenOnClick)
            {
                CloseMenuItem.FontWeight = FontWeights.Bold;
                RestoreMenuItem.FontWeight = FontWeights.Normal;
            }
            MoveMenuItem.StaysOpenOnClick = wss != NativeMethods.WindowShowStyle.ShowNormal;
            SizeMenuItem.StaysOpenOnClick = !(wss == NativeMethods.WindowShowStyle.ShowNormal && (ws & (int)NativeMethods.WindowStyles.WS_MAXIMIZEBOX) != 0);
        }

        private void CloseMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((MenuItem)sender).StaysOpenOnClick)
            {
                Window?.Close();
            }
        }

        private void RestoreMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((MenuItem)sender).StaysOpenOnClick)
            {
                Window?.Restore();
            }
        }

        private void MoveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((MenuItem)sender).StaysOpenOnClick)
            {
                Window?.Move();
            }
        }

        private void SizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((MenuItem)sender).StaysOpenOnClick)
            {
                Window?.Size();
            }
        }

        private void MinimizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((MenuItem)sender).StaysOpenOnClick)
            {
                Window?.Minimize();
            }
        }

        private void MaximizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((MenuItem)sender).StaysOpenOnClick)
            {
                Window?.Maximize();
            }
        }

        private void AppButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (PressedWindowState == ApplicationWindow.WindowState.Active && Window?.CanMinimize == true)
            {
                Window?.Minimize();
            }
            else
            {
                Window?.BringToFront();
            }
        }

        private void AppButton_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                PressedWindowState = Window.State;
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
        }

        #region Drag
        private bool inDrag;
        private DispatcherTimer dragTimer;

        private void dragTimer_Tick(object sender, EventArgs e)
        {
            if (inDrag)
            {
                Window?.BringToFront();
            }

            dragTimer.Stop();
        }

        private void AppButton_OnDragEnter(object sender, DragEventArgs e)
        {
            // Ignore drag operations from a reorder
            if (!inDrag && !e.Data.GetDataPresent("GongSolutions.Wpf.DragDrop"))
            {
                inDrag = true;
                dragTimer.Start();
            }
        }

        private void AppButton_OnDragLeave(object sender, DragEventArgs e)
        {
            if (inDrag)
            {
                dragTimer.Stop();
                inDrag = false;
            }
        }
        #endregion

        private void ContextMenu_OpenedOrClosed(object sender, RoutedEventArgs e)
        {
            BindingOperations.GetMultiBindingExpression(AppButton, StyleProperty).UpdateTarget();
        }
    }
}