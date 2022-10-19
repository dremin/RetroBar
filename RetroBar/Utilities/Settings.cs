using ManagedShell.AppBar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.CompilerServices;

namespace RetroBar.Utilities
{
    public class Settings : INotifyPropertyChanged
    {
        public delegate void SettingsEventHandler(object sender, EventArgs args);
        public static event SettingsEventHandler Initializing;
        public static event SettingsEventHandler Initialized;
        private static bool _initialized;

        // For Reference
        // https://docs.microsoft.com/en-us/dotnet/framework/wpf/data/how-to-implement-property-change-notification
        public event PropertyChangedEventHandler PropertyChanged;

        private static Settings instance;
        private bool _upgrading;
        private readonly Properties.Settings settings;

        private Settings()
        {
            settings = Properties.Settings.Default;
            settings.PropertyChanged += Settings_PropertyChanged;

            if (IsFirstRun)
            {
                Upgrade();
            }
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_upgrading)
                return;

            // Save the planet, one property at a time.
            Save();

            // Tell the rest of the app.
            OnNotifyPropertyChanged(e.PropertyName);
        }


        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void OnNotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static Settings Instance
        {
            get
            {
                if (instance == null)
                {
                    // add SettingsInitializing event handler
                    // this should be where plugins can register PropertySettings with our core
                    Initializing?.Invoke(null, new EventArgs());

                    instance = new Settings();
                    _initialized = true;

                    // add SettingsInitialized event handler
                    // This should inform the system that all PropertySettings should be added and can now be accessed safely
                    Initialized?.Invoke(instance, new EventArgs());
                }

                return instance;
            }
        }

        #region Properties
        public bool IsFirstRun
        {
            get
            {
                return settings.IsFirstRun;
            }
            set
            {
                if (settings.IsFirstRun != value)
                {
                    settings.IsFirstRun = value;
                }
            }
        }

        public string Language
        {
            get
            {
                return settings.Language;
            }
            set
            {
                if (settings.Language != value)
                {
                    settings.Language = value;
                }
            }
        }

        public string Theme
        {
            get
            {
                return settings.Theme;
            }
            set
            {
                if (settings.Theme != value)
                {
                    settings.Theme = value;
                }
            }
        }

        public bool ShowClock
        {
            get
            {
                return settings.ShowClock;
            }
            set
            {
                if (settings.ShowClock != value)
                {
                    settings.ShowClock = value;
                }
            }
        }

        public bool ShowMultiMon
        {
            get
            {
                return settings.ShowMultiMon;
            }
            set
            {
                if (settings.ShowMultiMon != value)
                {
                    settings.ShowMultiMon = value;
                }
            }
        }

        public bool ShowQuickLaunch
        {
            get
            {
                return settings.ShowQuickLaunch;
            }
            set
            {
                if (settings.ShowQuickLaunch != value)
                {
                    settings.ShowQuickLaunch = value;
                }
            }
        }

        public string QuickLaunchPath
        {
            get
            {
                return settings.QuickLaunchPath;
            }
            set
            {
                if (settings.QuickLaunchPath != value)
                {
                    settings.QuickLaunchPath = value;
                }
            }
        }

        public bool CollapseNotifyIcons
        {
            get
            {
                return settings.CollapseNotifyIcons;
            }
            set
            {
                if (settings.CollapseNotifyIcons != value)
                {
                    settings.CollapseNotifyIcons = value;
                }
            }
        }

        public string[] PinnedNotifyIcons
        {
            get
            {
                return parseConcatString(settings.PinnedNotifyIcons, '|').ToArray();
            }
            set
            {
                string val = concatStringList(value, '|');
                if (settings.PinnedNotifyIcons != val)
                {
                    settings.PinnedNotifyIcons = val;
                }
            }
        }

        public bool AllowFontSmoothing
        {
            get
            {
                return settings.AllowFontSmoothing;
            }
            set
            {
                if (settings.AllowFontSmoothing != value)
                {
                    settings.AllowFontSmoothing = value;
                }
            }
        }

        public bool UseSoftwareRendering
        {
            get
            {
                return settings.UseSoftwareRendering;
            }
            set
            {
                if (settings.UseSoftwareRendering != value)
                {
                    settings.UseSoftwareRendering = value;
                }
            }
        }

        public int Edge
        {
            get
            {
                if (settings.Edge >= 0 && settings.Edge <= 3)
                {
                    return settings.Edge;
                }

                return (int)AppBarEdge.Bottom;
            }
            set
            {
                if (settings.Edge != value && value >= 0 && value <= 3)
                {
                    settings.Edge = value;
                }
            }
        }

        private List<string> _quickLaunchOrder;

        public List<string> QuickLaunchOrder
        {
            get
            {
                if (_quickLaunchOrder == null)
                {
                    _quickLaunchOrder = parseConcatString(settings.QuickLaunchOrder, '|');
                }

                return _quickLaunchOrder;
            }
            set
            {
                _quickLaunchOrder = value;

                string val = concatStringList(value, '|');
                if (settings.QuickLaunchOrder != val)
                {
                    settings.QuickLaunchOrder = val;
                }
            }
        }

        public bool ShowTaskThumbnails
        {
            get
            {
                return settings.ShowTaskThumbnails;
            }
            set
            {
                if (settings.ShowTaskThumbnails != value)
                {
                    settings.ShowTaskThumbnails = value;
                }
            }
        }
        #endregion

        #region Helpers
        private List<string> parseConcatString(string concat, char separator)
        {
            List<string> parsed = new List<string>();

            foreach (string key in concat.Split(separator))
            {
                if (!string.IsNullOrEmpty(key))
                {
                    parsed.Add(key);
                }
            }

            return parsed;
        }

        private string concatStringList(IEnumerable<string> list, char separator)
        {
            string concatenated = "";

            foreach (string key in list)
            {
                if (!string.IsNullOrEmpty(concatenated))
                {
                    concatenated += separator.ToString();
                }

                concatenated += key;
            }

            return concatenated;
        }
        #endregion

        public void Save()
        {
            settings.Save();
        }

        public void Upgrade()
        {
            _upgrading = true;
            settings.Upgrade();
            _upgrading = false;

            if (IsFirstRun) IsFirstRun = false;
        }

        public object this[string propertyName]
        {
            get
            {
                return settings[propertyName];
            }
            set
            {
                settings[propertyName] = value;
            }
        }

        public bool Exists(string name)
        {
            return settings.Properties[name] != null;
        }

        public void AddPropertySetting(string name, Type type, object defaultValue)
        {
            // Only allow settings to be added during initialization
            if (!_initialized)
            {
                string providerName = "LocalFileSettingsProvider";

                SettingsAttributeDictionary attributes = new SettingsAttributeDictionary();
                UserScopedSettingAttribute attr = new UserScopedSettingAttribute();
                attributes.Add(attr.TypeId, attr);

                var prop = new SettingsProperty(
                    new SettingsProperty(name
                    , type
                    , settings.Providers[providerName]
                    , false
                    , defaultValue
                    , SettingsSerializeAs.String
                    , attributes
                    , false
                    , false));

                settings.Properties.Add(prop);
                settings.Save();
                settings.Reload();
            }
        }

    }
}