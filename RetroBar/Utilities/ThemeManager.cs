using System;
using System.ComponentModel;
using System.Windows;

namespace RetroBar.Utilities
{
    public class ThemeManager
    {
        public ThemeManager()
        {
            SetThemeFromSettings();

            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        public void SetThemeFromSettings()
        {
            if (Settings.Instance.Theme != "Default")
            {
                SetTheme(Settings.Instance.Theme);
            }
        }

        public void SetTheme(string theme)
        {
            string themeFilePath;

            if (theme == "Default")
            {
                Application.Current.Resources.MergedDictionaries.Clear();
                themeFilePath = "Themes\\Windows9x.xaml";
            }
            else
            {
                themeFilePath = AppDomain.CurrentDomain.BaseDirectory + "Themes\\" + theme;

                if (!System.IO.File.Exists(themeFilePath))
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

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Theme")
            {
                SetTheme("Default");
                SetThemeFromSettings();
            }
        }
    }
}
