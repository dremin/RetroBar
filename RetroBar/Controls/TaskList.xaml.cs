using ManagedShell.WindowsTasks;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for TaskList.xaml
    /// </summary>
    public partial class TaskList : UserControl
    {
        private bool isLoaded;
        private double DefaultButtonWidth;
        private double MinButtonWidth;
        private double TaskButtonLeftMargin;
        private double TaskButtonRightMargin;

        public static DependencyProperty ButtonWidthProperty = DependencyProperty.Register("ButtonWidth", typeof(double), typeof(TaskList), new PropertyMetadata(new double()));
        private Tasks _tasks;

        public double ButtonWidth
        {
            get { return (double)GetValue(ButtonWidthProperty); }
            set { SetValue(ButtonWidthProperty, value); }
        }

        public TaskList()
        {
            InitializeComponent();
        }

        public void SetTasks(Tasks tasks)
        {
            _tasks = tasks;
        }

        private void TaskList_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
            {
                _tasks.Initialize();
                TasksList.ItemsSource = _tasks.GroupedWindows;
                if (_tasks.GroupedWindows != null)
                    _tasks.GroupedWindows.CollectionChanged += GroupedWindows_CollectionChanged;

                DefaultButtonWidth = Application.Current.FindResource("TaskButtonWidth") as double? ?? 0;
                MinButtonWidth = Application.Current.FindResource("TaskButtonMinWidth") as double? ?? 0;

                Thickness buttonMargin = Application.Current.FindResource("TaskButtonMargin") as Thickness? ?? new Thickness();
                TaskButtonLeftMargin = buttonMargin.Left;
                TaskButtonRightMargin = buttonMargin.Right;

                isLoaded = true;
            }
        }

        private void TaskList_OnUnloaded(object sender, RoutedEventArgs e)
        {
            _tasks.GroupedWindows.CollectionChanged -= GroupedWindows_CollectionChanged;
        }

        private void GroupedWindows_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {                
            SetTaskButtonWidth();
        }

        private void TaskList_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetTaskButtonWidth();
        }

        private void SetTaskButtonWidth()
        {
            double margin = TaskButtonLeftMargin + TaskButtonRightMargin;
            double maxWidth = ActualWidth / TasksList.Items.Count;
            double defaultWidth = DefaultButtonWidth + margin;
            double minWidth = MinButtonWidth + margin;

            if (maxWidth > defaultWidth || maxWidth < minWidth)
            {
                ButtonWidth = DefaultButtonWidth;
            }
            else
            {
                ButtonWidth = Math.Floor(maxWidth) - margin;
            }
        }
    }
}
