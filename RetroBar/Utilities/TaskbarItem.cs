using ManagedShell.ShellFolders;
using ManagedShell.WindowsTasks;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace RetroBar.Utilities
{
    /// <summary>
    /// Unified wrapper for taskbar items - can represent a pinned item, running program, or both
    /// </summary>
    public class TaskbarItem : INotifyPropertyChanged
    {
        private ShellFile _pinnedItem;
        private ApplicationWindow _runningWindow;
        private ObservableCollection<ApplicationWindow> _windows = new ObservableCollection<ApplicationWindow>();

        public event PropertyChangedEventHandler PropertyChanged;

        public TaskbarItem(ShellFile pinnedItem)
        {
            _pinnedItem = pinnedItem;
        }

        public TaskbarItem(ApplicationWindow window)
        {
            _runningWindow = window;
            _windows.Add(window);

            if (window != null)
            {
                window.PropertyChanged += Window_PropertyChanged;
            }
        }

        /// <summary>
        /// The pinned Quick Launch item (null if not pinned)
        /// </summary>
        public ShellFile PinnedItem
        {
            get => _pinnedItem;
            set
            {
                _pinnedItem = value;
                OnPropertyChanged(nameof(PinnedItem));
                OnPropertyChanged(nameof(IsPinned));
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(Title));
            }
        }

        /// <summary>
        /// The primary running window (null if not running)
        /// </summary>
        public ApplicationWindow RunningWindow
        {
            get => _runningWindow;
            set
            {
                if (_runningWindow != null)
                {
                    _runningWindow.PropertyChanged -= Window_PropertyChanged;
                }

                _runningWindow = value;

                if (_runningWindow != null)
                {
                    _runningWindow.PropertyChanged += Window_PropertyChanged;
                }

                OnPropertyChanged(nameof(RunningWindow));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(State));
                OnPropertyChanged(nameof(Handle));
                OnPropertyChanged(nameof(ThumbnailButtons));
                OnPropertyChanged(nameof(ThumbnailButtonImageList));
            }
        }

        /// <summary>
        /// All windows for this item (for grouped tasks)
        /// </summary>
        public ObservableCollection<ApplicationWindow> Windows
        {
            get => _windows;
        }

        public bool IsPinned => _pinnedItem != null;
        public bool IsRunning => _runningWindow != null || _windows.Count > 0;
        public bool IsGroup => _windows.Count > 1;

        /// <summary>
        /// True if this is a pinned item that is not currently running
        /// </summary>
        public bool IsPinnedOnly => IsPinned && !IsRunning;

        /// <summary>
        /// Icon to display - prefer running window icon, fallback to pinned item
        /// </summary>
        public ImageSource Icon
        {
            get
            {
                if (_runningWindow != null)
                    return _runningWindow.Icon;
                if (_pinnedItem != null)
                    return _pinnedItem.LargeIcon; // Use LargeIcon as default, could also use SmallIcon
                return null;
            }
        }

        /// <summary>
        /// Overlay icon from running window
        /// </summary>
        public ImageSource OverlayIcon => _runningWindow?.OverlayIcon;

        /// <summary>
        /// Title to display - prefer running window title, fallback to pinned item
        /// </summary>
        public string Title
        {
            get
            {
                if (_runningWindow != null)
                    return _runningWindow.Title;
                if (_pinnedItem != null)
                    return _pinnedItem.DisplayName;
                return "";
            }
        }

        /// <summary>
        /// Window state for styling
        /// </summary>
        public ApplicationWindow.WindowState State => _runningWindow?.State ?? ApplicationWindow.WindowState.Inactive;

        /// <summary>
        /// Window handle for thumbnails
        /// </summary>
        public IntPtr Handle => _runningWindow?.Handle ?? IntPtr.Zero;

        /// <summary>
        /// Progress value for taskbar progress
        /// </summary>
        public int ProgressValue => _runningWindow?.ProgressValue ?? 0;

        /// <summary>
        /// Progress state for taskbar progress
        /// </summary>
        public ManagedShell.Interop.NativeMethods.TBPFLAG ProgressState =>
            _runningWindow?.ProgressState ?? ManagedShell.Interop.NativeMethods.TBPFLAG.TBPF_NOPROGRESS;

        /// <summary>
        /// Whether the window can be minimized
        /// </summary>
        public bool CanMinimize => _runningWindow?.CanMinimize ?? false;

        /// <summary>
        /// Path to the executable or shortcut
        /// </summary>
        public string Path
        {
            get
            {
                if (_pinnedItem != null)
                    return _pinnedItem.Path;
                if (_runningWindow != null)
                    return _runningWindow.WinFileName;
                return null;
            }
        }

        /// <summary>
        /// Thumbnail buttons for taskbar button previews (media controls, etc.)
        /// </summary>
        public ManagedShell.WindowsTasks.ThumbnailButton[] ThumbnailButtons => _runningWindow?.ThumbnailButtons;

        /// <summary>
        /// Thumbnail button image list for rendering button icons
        /// </summary>
        public IntPtr ThumbnailButtonImageList => _runningWindow?.ThumbnailButtonImageList ?? IntPtr.Zero;

        /// <summary>
        /// Add a running window to this pinned item
        /// </summary>
        public void AddWindow(ApplicationWindow window)
        {
            if (window == null) return;

            if (!_windows.Contains(window))
            {
                _windows.Add(window);
                window.PropertyChanged += Window_PropertyChanged;

                if (_runningWindow == null)
                {
                    RunningWindow = window;
                }
                else
                {
                    OnPropertyChanged(nameof(IsGroup));
                    OnPropertyChanged(nameof(Windows));
                }

                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsPinnedOnly));
            }
        }

        /// <summary>
        /// Remove a running window from this item
        /// </summary>
        public void RemoveWindow(ApplicationWindow window)
        {
            if (window == null) return;

            if (_windows.Contains(window))
            {
                window.PropertyChanged -= Window_PropertyChanged;
                _windows.Remove(window);

                if (_runningWindow == window)
                {
                    RunningWindow = _windows.FirstOrDefault();
                }
                else
                {
                    OnPropertyChanged(nameof(IsGroup));
                    OnPropertyChanged(nameof(Windows));
                }

                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsPinnedOnly));
            }
        }

        private void Window_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Forward property changes from the window
            switch (e.PropertyName)
            {
                case nameof(ApplicationWindow.Icon):
                    OnPropertyChanged(nameof(Icon));
                    break;
                case nameof(ApplicationWindow.Title):
                    OnPropertyChanged(nameof(Title));
                    break;
                case nameof(ApplicationWindow.State):
                    OnPropertyChanged(nameof(State));
                    break;
                case nameof(ApplicationWindow.ProgressValue):
                    OnPropertyChanged(nameof(ProgressValue));
                    break;
                case nameof(ApplicationWindow.ProgressState):
                    OnPropertyChanged(nameof(ProgressState));
                    break;
                case nameof(ApplicationWindow.OverlayIcon):
                    OnPropertyChanged(nameof(OverlayIcon));
                    break;
                case nameof(ApplicationWindow.ThumbnailButtons):
                    OnPropertyChanged(nameof(ThumbnailButtons));
                    break;
                case nameof(ApplicationWindow.ThumbnailButtonImageList):
                    OnPropertyChanged(nameof(ThumbnailButtonImageList));
                    break;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
