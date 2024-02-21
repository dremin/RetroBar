using ManagedShell.AppBar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace RetroBar.Utilities
{
    internal class Settings : INotifyPropertyChanged
    {
        private static Settings instance;

        public static Settings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = _settingsManager.Settings;
                    _isInitializing = false;
                }

                return instance;
            }
        }

        private static string _settingsPath = "settings.json".InLocalAppData();
        private static bool _isInitializing = true;
        private static SettingsManager<Settings> _settingsManager = new SettingsManager<Settings>(_settingsPath, new Settings());

        public event PropertyChangedEventHandler PropertyChanged;

        // This should not be used directly! Unfortunately it must be public for JsonSerializer.
        public Settings()
        {
            PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isInitializing)
            {
                return;
            }

            _settingsManager.Settings = this;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties
        private string _language = "System";
        public string Language
        {
            get
            {
                return _language;
            }
            set
            {
                if (_language != value)
                {
                    _language = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _theme = "Windows 95-98";
        public string Theme
        {
            get
            {
                return _theme;
            }
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showInputLanguage = false;
        public bool ShowInputLanguage
        {
            get
            {
                return _showInputLanguage;
            }
            set
            {
                if (_showInputLanguage != value)
                {
                    _showInputLanguage = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private bool _showClock = true;
        public bool ShowClock
        {
            get
            {
                return _showClock;
            }
            set
            {
                if (_showClock != value)
                {
                    _showClock = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showDesktopButton = false;
        public bool ShowDesktopButton
        {
            get
            {
                return _showDesktopButton;
            }
            set
            {
                if (_showDesktopButton != value)
                {
                    _showDesktopButton = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _peekAtDesktop = false;
        public bool PeekAtDesktop
        {
            get
            {
                return _peekAtDesktop;
            }
            set
            {
                if (_peekAtDesktop != value)
                {
                    _peekAtDesktop = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showMultiMon = false;
        public bool ShowMultiMon
        {
            get
            {
                return _showMultiMon;
            }
            set
            {
                if (_showMultiMon != value)
                {
                    _showMultiMon = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showQuickLaunch = true;
        public bool ShowQuickLaunch
        {
            get
            {
                return _showQuickLaunch;
            }
            set
            {
                if (_showQuickLaunch != value)
                {
                    _showQuickLaunch = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _quickLaunchPath = "%appdata%\\Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar";
        public string QuickLaunchPath
        {
            get
            {
                return _quickLaunchPath;
            }
            set
            {
                if (_quickLaunchPath != value)
                {
                    _quickLaunchPath = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _collapseNotifyIcons = false;
        public bool CollapseNotifyIcons
        {
            get
            {
                return _collapseNotifyIcons;
            }
            set
            {
                if (_collapseNotifyIcons != value)
                {
                    _collapseNotifyIcons = value;
                    OnPropertyChanged();
                }
            }
        }

        private string[] _pinnedNotifyIcons = { "7820ae76-23e3-4229-82c1-e41cb67d5b9c", "7820ae75-23e3-4229-82c1-e41cb67d5b9c", "7820ae74-23e3-4229-82c1-e41cb67d5b9c", "7820ae73-23e3-4229-82c1-e41cb67d5b9c" };
        public string[] PinnedNotifyIcons
        {
            get
            {
                return _pinnedNotifyIcons;
            }
            set
            {
                if (_pinnedNotifyIcons != value)
                {
                    _pinnedNotifyIcons = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _allowFontSmoothing = false;
        public bool AllowFontSmoothing
        {
            get
            {
                return _allowFontSmoothing;
            }
            set
            {
                if (_allowFontSmoothing != value)
                {
                    _allowFontSmoothing = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _useSoftwareRendering = false;
        public bool UseSoftwareRendering
        {
            get
            {
                return _useSoftwareRendering;
            }
            set
            {
                if (_useSoftwareRendering != value)
                {
                    _useSoftwareRendering = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _middleMouseToClose = false;
        public bool MiddleMouseToClose
        {
            get
            {
                return _middleMouseToClose;
            }
            set
            {
                if (_middleMouseToClose != value)
                {
                    _middleMouseToClose = value;
                    OnPropertyChanged();
                }
            }
        }

        private AppBarEdge _edge = AppBarEdge.Bottom;
        public AppBarEdge Edge
        {
            get
            {
                return _edge;
            }
            set
            {
                if (_edge != value && (int)value >= 0)
                {
                    _edge = value;
                    OnPropertyChanged();
                }
            }
        }

        private List<string> _quickLaunchOrder = new List<string>();

        public List<string> QuickLaunchOrder
        {
            get
            {
                return _quickLaunchOrder;
            }
            set
            {
                if (_quickLaunchOrder != value)
                {
                    _quickLaunchOrder = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showTaskThumbnails = false;
        public bool ShowTaskThumbnails
        {
            get
            {
                return _showTaskThumbnails;
            }
            set
            {
                if (_showTaskThumbnails != value)
                {
                    _showTaskThumbnails = value;
                    OnPropertyChanged();
                }
            }
        }

        private MultiMonOption _multiMonMode = MultiMonOption.AllTaskbars;
        public MultiMonOption MultiMonMode
        {
            get
            {
                return _multiMonMode;
            }
            set
            {
                if (_multiMonMode != value && (int)value >= 0)
                {
                    _multiMonMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _taskbarScale = 1.0;
        public double TaskbarScale
        {
            get
            {
                return _taskbarScale;
            }
            set
            {
                if (_taskbarScale != value)
                {
                    _taskbarScale = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _debugLogging = false;
        public bool DebugLogging
        {
            get
            {
                return _debugLogging;
            }
            set
            {
                if (_debugLogging != value)
                {
                    _debugLogging = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _autoHide = false;
        public bool AutoHide
        {
            get
            {
                return _autoHide;
            }
            set
            {
                if (_autoHide != value)
                {
                    _autoHide = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _lockTaskbar = false;
        public bool LockTaskbar
        {
            get
            {
                return _lockTaskbar;
            }
            set
            {
                if (_lockTaskbar != value)
                {
                    _lockTaskbar = value;
                    OnPropertyChanged();
                }
            }
        }

        private InvertIconsOption _invertIconsMode = InvertIconsOption.WhenNeededByTheme;
        public InvertIconsOption InvertIconsMode
        {
            get
            {
                return _invertIconsMode;
            }
            set
            {
                if (_invertIconsMode != value && (int)value >= 0)
                {
                    _invertIconsMode = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region Enums
        public enum InvertIconsOption
        {
            WhenNeededByTheme,
            Always,
            Never
        }

        public enum MultiMonOption
        {
            AllTaskbars,
            SameAsWindow,
            SameAsWindowAndPrimary
        }
        #endregion
    }
}
