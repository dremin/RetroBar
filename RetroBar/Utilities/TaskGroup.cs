using ManagedShell.WindowsTasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace RetroBar.Utilities
{
    /// <summary>
    /// Represents a single window or a group of windows with the same application
    /// </summary>
    public class TaskGroup : INotifyPropertyChanged
    {
        private ObservableCollection<ApplicationWindow> _windows = new ObservableCollection<ApplicationWindow>();

        public ObservableCollection<ApplicationWindow> Windows
        {
            get => _windows;
            set
            {
                _windows = value;
                OnPropertyChanged(nameof(Windows));
                OnPropertyChanged(nameof(IsGroup));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Icon));
            }
        }

        public bool IsGroup => _windows.Count > 1;

        public string Title
        {
            get
            {
                if (_windows.Count == 0) return string.Empty;
                var firstWindow = _windows[0];
                if (IsGroup)
                {
                    return $"{firstWindow.Title} ({_windows.Count})";
                }
                return firstWindow.Title;
            }
        }

        public ImageSource Icon => _windows.Count > 0 ? _windows[0].Icon : null;

        public ImageSource OverlayIcon => _windows.Count > 0 ? _windows[0].OverlayIcon : null;

        public int ProgressValue => _windows.Count > 0 ? _windows[0].ProgressValue : 0;

        public ManagedShell.Interop.NativeMethods.TBPFLAG ProgressState =>
            _windows.Count > 0 ? _windows[0].ProgressState : ManagedShell.Interop.NativeMethods.TBPFLAG.TBPF_NOPROGRESS;

        public ApplicationWindow.WindowState State
        {
            get
            {
                if (_windows.Count == 0) return ApplicationWindow.WindowState.Inactive;

                // If any window is active, the group is active
                if (_windows.Any(w => w.State == ApplicationWindow.WindowState.Active))
                    return ApplicationWindow.WindowState.Active;

                // If any window is flashing, the group is flashing
                if (_windows.Any(w => w.State == ApplicationWindow.WindowState.Flashing))
                    return ApplicationWindow.WindowState.Flashing;

                return ApplicationWindow.WindowState.Inactive;
            }
        }

        public IntPtr Handle => _windows.Count > 0 ? _windows[0].Handle : IntPtr.Zero;

        public string GroupKey { get; set; }

        public TaskGroup(string groupKey)
        {
            GroupKey = groupKey;
            _windows.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(IsGroup));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(OverlayIcon));
                OnPropertyChanged(nameof(ProgressValue));
                OnPropertyChanged(nameof(ProgressState));
                OnPropertyChanged(nameof(State));
                OnPropertyChanged(nameof(Handle));
            };
        }

        public void AddWindow(ApplicationWindow window)
        {
            if (!_windows.Contains(window))
            {
                _windows.Add(window);
                window.PropertyChanged += Window_PropertyChanged;
            }
        }

        public void RemoveWindow(ApplicationWindow window)
        {
            if (_windows.Contains(window))
            {
                window.PropertyChanged -= Window_PropertyChanged;
                _windows.Remove(window);
            }
        }

        private void Window_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Propagate property changes from windows
            if (e.PropertyName == nameof(ApplicationWindow.State))
            {
                OnPropertyChanged(nameof(State));
            }
            else if (e.PropertyName == nameof(ApplicationWindow.Title))
            {
                OnPropertyChanged(nameof(Title));
            }
            else if (e.PropertyName == nameof(ApplicationWindow.Icon))
            {
                OnPropertyChanged(nameof(Icon));
            }
            else if (e.PropertyName == nameof(ApplicationWindow.OverlayIcon))
            {
                OnPropertyChanged(nameof(OverlayIcon));
            }
            else if (e.PropertyName == nameof(ApplicationWindow.ProgressValue))
            {
                OnPropertyChanged(nameof(ProgressValue));
            }
            else if (e.PropertyName == nameof(ApplicationWindow.ProgressState))
            {
                OnPropertyChanged(nameof(ProgressState));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
