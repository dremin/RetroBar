using ManagedShell.AppBar;
using ManagedShell.ShellFolders;
using RetroBar.Extensions;
using RetroBar.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RetroBar
{
    /// <summary>
    /// Interaction logic for QuickLaunchPropertiesWindow.xaml
    /// </summary>
    public partial class QuickLaunchPropertiesWindow : Window
    {
        private static QuickLaunchPropertiesWindow _instance;

        private readonly ShellFolder _quickLaunchFolder;
        private readonly List<AppBarScreen> _screens;

        private QuickLaunchPropertiesWindow(ShellFolder quickLaunchFolder, List<AppBarScreen> screens)
        {
            _quickLaunchFolder = quickLaunchFolder;
            _screens = screens;

            InitializeComponent();

            AddDisplayColumns();
            QuickLaunchListView.ItemsSource = _quickLaunchFolder.Files;

            ListCollectionView cvs = (ListCollectionView)CollectionViewSource.GetDefaultView(_quickLaunchFolder.Files);
            cvs.CustomSort = new QuickLaunchPropertySorter();
        }

        // Dynamically adds one checkbox column per connected display.
        private void AddDisplayColumns()
        {
            for (int i = 0; i < _screens.Count; i++)
            {
                var screen = _screens[i];

                var column = new GridViewColumn
                {
                    Header = screen.DeviceName,
                    Width = 70
                };

                var factory = new FrameworkElementFactory(typeof(CheckBox));
                factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);

                int capturedIndex = i;

                factory.AddHandler(FrameworkElement.LoadedEvent,
                    new RoutedEventHandler((sender, e) => DisplayCheckBox_Loaded(sender, capturedIndex)));

                factory.AddHandler(
                    System.Windows.Controls.Primitives.ToggleButton.CheckedEvent,
                    new RoutedEventHandler((sender, e) => DisplayCheckBox_Changed(sender, capturedIndex, true)));
                factory.AddHandler(
                    System.Windows.Controls.Primitives.ToggleButton.UncheckedEvent,
                    new RoutedEventHandler((sender, e) => DisplayCheckBox_Changed(sender, capturedIndex, false)));

                column.CellTemplate = new DataTemplate { VisualTree = factory };
                QuickLaunchGridView.Columns.Add(column);
            }
        }

        private void DisplayCheckBox_Loaded(object sender, int displayIndex)
        {
            var checkBox = sender as CheckBox;
            var file = checkBox?.DataContext as ShellFile;

            if (file == null) return;

            checkBox.IsChecked = file.IsEnabledOnDisplay(_screens[displayIndex].DeviceName);
        }

        private void DisplayCheckBox_Changed(object sender, int displayIndex, bool isEnabled)
        {
            var checkBox = sender as CheckBox;
            var file = checkBox?.DataContext as ShellFile;

            if (file == null) return;

            file.SetEnabledOnDisplay(_screens[displayIndex].DeviceName, isEnabled);
        }

        public static void Open(ShellFolder quickLaunchFolder, List<AppBarScreen> screens, Point position)
        {
            if (_instance == null)
            {
                _instance = new QuickLaunchPropertiesWindow(quickLaunchFolder, screens);
                _instance.Left = position.X + 10;
                _instance.Top = position.Y + 10;
                _instance.Show();
            }
            else
            {
                _instance.Activate();
            }
        }

        private void OK_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _instance = null;
        }
    }

    public class QuickLaunchPropertySorter : IComparer
    {
        public int Compare(object x, object y)
        {
            if (x is ShellItem a && y is ShellItem b)
            {
                List<string> desiredSort = Settings.Instance.QuickLaunchOrder;

                bool hasA = desiredSort.Contains(a.Path);
                bool hasB = desiredSort.Contains(b.Path);

                if (!hasA && !hasB) return 0;
                if (!hasA) return 1;
                if (!hasB) return -1;

                return desiredSort.IndexOf(a.Path).CompareTo(desiredSort.IndexOf(b.Path));
            }

            return 0;
        }
    }
}
