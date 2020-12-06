using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace RetroBar.Utilities
{
    public class ThemeManager
    {
        private const string THEME_DEFAULT = "Default";
        private const string THEME_FOLDER = "Themes";
        private const string THEME_EXT = "xaml";

        public static ThemeManager Instance;

        public ThemeManager()
        {
            SetThemeFromSettings();

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        public void SetThemeFromSettings()
        {
            SetTheme(THEME_DEFAULT);
            if (Settings.Instance.Theme != THEME_DEFAULT)
            {
                SetTheme(Settings.Instance.Theme);
            }
        }

        public void SetTheme(string theme)
        {
            string themeFilePath;

            if (theme == THEME_DEFAULT)
            {
                Application.Current.Resources.MergedDictionaries.Clear();
                themeFilePath = Path.ChangeExtension(Path.Combine(THEME_FOLDER, THEME_DEFAULT), THEME_EXT);
            }
            else
            {
                themeFilePath = Path.ChangeExtension(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, THEME_FOLDER, theme), THEME_EXT);

                if (!File.Exists(themeFilePath))
                {
                    return;
                }
            }

            ResourceDictionary newRes = new ResourceDictionary()
            {
                Source = new Uri(themeFilePath, UriKind.RelativeOrAbsolute)
            };
            Application.Current.Resources.MergedDictionaries.Add(newRes);
        }

        public List<string> GetThemes()
        {
            List<string> themes = new List<string>();
            themes.Add(THEME_DEFAULT);

            foreach (string subStr in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, THEME_FOLDER)).Where(s => Path.GetExtension(s).Contains(THEME_EXT)))
            {
                themes.Add(Path.GetFileNameWithoutExtension(subStr));
            }

            return themes;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Theme")
            {
                SetThemeFromSettings();
            }
        }
    }
}
