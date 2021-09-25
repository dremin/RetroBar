using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ManagedShell.Common.Helpers;
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
            InitializeComponent();
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
            }
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
    }
}
