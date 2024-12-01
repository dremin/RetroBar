using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.ShellFolders;
using ManagedShell.ShellFolders.Enums;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for Toolbar.xaml
    /// </summary>
    public partial class Toolbar : UserControl
    {
        private bool _ignoreNextUpdate;
        private bool _isLoaded;

        private enum MenuItem : uint
        {
            OpenParentFolder = CommonContextMenuItem.Paste + 1
        }

        public static DependencyProperty PathProperty = DependencyProperty.Register("Path", typeof(string), typeof(Toolbar), new PropertyMetadata(OnPathChanged));

        public string Path
        {
            get => (string)GetValue(PathProperty);
            set
            {
                SetValue(PathProperty, value);
                SetupFolder(value);
            }
        }

        private static DependencyProperty FolderProperty = DependencyProperty.Register("Folder", typeof(ShellFolder), typeof(Toolbar));

        public ToolbarDropHandler DropHandler { get; set; }

        private ShellFolder Folder
        {
            get => (ShellFolder)GetValue(FolderProperty);
            set
            {
                SetValue(FolderProperty, value);
                SetItemsSource();
            }
        }

        public Toolbar()
        {
            DropHandler = new ToolbarDropHandler(this);

            InitializeComponent();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.QuickLaunchOrder))
            {
                if (_ignoreNextUpdate)
                {
                    _ignoreNextUpdate = false;
                    return;
                }

                Refresh();
            }
            else if (e.PropertyName == nameof(Settings.TaskbarScale))
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (Folder == null)
            {
                return;
            }

            ListCollectionView cvs = (ListCollectionView)CollectionViewSource.GetDefaultView(Folder.Files);
            cvs.Refresh();
        }

        private void SetupFolder(string path)
        {
            Folder?.Dispose();
            Folder = new ShellFolder(Environment.ExpandEnvironmentVariables(path), IntPtr.Zero, true);
        }

        private void UnloadFolder()
        {
            Folder?.Dispose();
            Folder = null;
        }

        private void SetItemsSource()
        {
            if (Folder != null)
            {
                ToolbarItems.ItemsSource = Folder.Files;
                ListCollectionView cvs = (ListCollectionView)CollectionViewSource.GetDefaultView(Folder.Files);
                cvs.CustomSort = new ToolbarSorter(this);
                cvs.Filter += Files_Filter;
            }
        }

        public void SaveItemOrder()
        {
            List<string> itemPaths = new List<string>();

            foreach (ShellFile file in ((ListCollectionView)CollectionViewSource.GetDefaultView(Folder.Files)).OfType<ShellFile>())
            {
                itemPaths.Add(file.Path);
            }

            // small optimization, only other toolbars with this folder need to reload when the setting is saved.
            _ignoreNextUpdate = true;

            Settings.Instance.QuickLaunchOrder = itemPaths;
        }

        public void AddToSource(StringCollection filesToAdd)
        {
            string sourcePath = Environment.ExpandEnvironmentVariables(Path);

            foreach (string itemPath in filesToAdd)
            {
                // Create shortcut to each dragged file
                try
                {
                    string destinationFileName = System.IO.Path.GetFileNameWithoutExtension(itemPath);
                    string destinationPath = System.IO.Path.Combine(sourcePath, destinationFileName + ".lnk");
                    int dupCount = 0;

                    while (ShellHelper.Exists(destinationPath))
                    {
                        dupCount++;

                        destinationPath = System.IO.Path.Combine(sourcePath, $"{destinationFileName} ({dupCount}).lnk");
                    }

                    ShellLinkHelper.CreateAndSave(itemPath, destinationPath);
                }
                catch (Exception e)
                {
                    ShellLogger.Error($"Toolbar: Unable to save shortcut to {itemPath}", e);
                }
            }
        }

        private bool Files_Filter(object obj)
        {
            if (obj is ShellFile file)
            {
                if (file.Path.EndsWith("\\Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar\\Tombstones"))
                {
                    // Windows moves broken shortcuts to this folder, so we should hide it
                    return false;
                }
            }

            return true;
        }

        #region Events
        private static void OnPathChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Toolbar toolbar)
            {
                toolbar.SetupFolder((string)e.NewValue);
            }
        }

        private void ToolbarIcon_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ToolbarButton icon = sender as ToolbarButton;
            if (icon == null)
            {
                return;
            }

            Mouse.Capture(null);
            ShellFile file = icon.DataContext as ShellFile;

            if (file == null || string.IsNullOrWhiteSpace(file.Path))
            {
                return;
            }

            if (InvokeContextMenu(file, false))
            {
                e.Handled = true;
            }
        }

        private void ToolbarIcon_OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ToolbarButton icon = sender as ToolbarButton;
            if (icon == null)
            {
                return;
            }
            
            ShellFile file = icon.DataContext as ShellFile;

            if (InvokeContextMenu(file, true))
            {
                e.Handled = true;
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool visible)
            {
                if (visible)
                {
                    if (Folder != null)
                    {
                        return;
                    }

                    SetupFolder(Path);
                }
                else
                {
                    UnloadFolder();
                }
            }
        }
        #endregion

        #region Context menu
        private ShellMenuCommandBuilder GetFileCommandBuilder(ShellFile file)
        {
            if (file == null)
            {
                return new ShellMenuCommandBuilder();
            }

            ShellMenuCommandBuilder builder = new ShellMenuCommandBuilder();

            builder.AddSeparator();
            builder.AddCommand(new ShellMenuCommand
            {
                Flags = MFT.BYCOMMAND,
                Label = (string)FindResource("open_folder"),
                UID = (uint)MenuItem.OpenParentFolder
            });

            return builder;
        }

        private bool InvokeContextMenu(ShellFile file, bool isInteractive)
        {
            if (file == null)
            {
                return false;
            }
            
            var _ = new ShellItemContextMenu(new ShellItem[] { file }, Folder, IntPtr.Zero, HandleFileAction, isInteractive, false, new ShellMenuCommandBuilder(), GetFileCommandBuilder(file));
            return true;
        }

        private bool HandleFileAction(string action, ShellItem[] items, bool allFolders)
        {
            if (action == ((uint)MenuItem.OpenParentFolder).ToString())
            {
                ShellHelper.StartProcess(Folder.Path);
                return true;
            }

            return false;
        }
        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
            {
                Settings.Instance.PropertyChanged += Settings_PropertyChanged;

                _isLoaded = true;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;

            _isLoaded = false;
        }
    }
}