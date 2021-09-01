using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace RetroBar.Utilities
{
    public class DictionaryManager : IDisposable
    {
        private const string DICT_DEFAULT = "System"; // TODO: Localize "System"?
        private const string DICT_EXT = "xaml";

        private const string LANG_DEFAULT = DICT_DEFAULT;
        private const string LANG_FALLBACK = "English";
        private const string LANG_FOLDER = "Languages";
        private const string LANG_EXT = DICT_EXT;

        public const string  THEME_DEFAULT = DICT_DEFAULT;
        private const string THEME_FOLDER = "Themes";
        private const string THEME_EXT = DICT_EXT;

        public DictionaryManager()
        {
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
            SetDictionary(theme, THEME_FOLDER, THEME_DEFAULT, THEME_EXT);
        }

        public void SetLanguageFromSettings()
        {
            var currentUICulture = System.Globalization.CultureInfo.CurrentUICulture;
            string systemLanguage = currentUICulture.NativeName;
            string systemLanguageParent = currentUICulture.Parent.NativeName;
            SetLanguage(LANG_FALLBACK);
            if (Settings.Instance.Language == LANG_DEFAULT)
            {
                ManagedShell.Common.Logging.ShellLogger.Info($"Loading system language (if available): {systemLanguage}, {systemLanguageParent}");
                SetLanguage(systemLanguageParent);
                SetLanguage(systemLanguage);
            }
            else
            {
                SetLanguage(Settings.Instance.Language);
            }
        }

        public void SetLanguage(string language)
        {
            SetDictionary(language, LANG_FOLDER, LANG_FALLBACK, LANG_EXT);
        }

        public void SetDictionary(string dictionary, string dictFolder, string dictDefault, string dictExtension)
        {
            string dictFilePath;

            if (dictionary == dictDefault)
            {
                // FIXME (clear only theme dictionaries)
                //Application.Current.Resources.MergedDictionaries.Clear();
                dictFilePath = Path.ChangeExtension(Path.Combine(dictFolder, dictDefault), dictExtension);
            }
            else
            {
                dictFilePath = Path.ChangeExtension(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dictFolder, dictionary), dictExtension);

                if (!File.Exists(dictFilePath))
                {
                    // custom dictionary in app directory
                    dictFilePath = Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(ExePath.GetExecutablePath()), dictFolder, dictionary), dictExtension);

                    if (!File.Exists(dictFilePath))
                    {
                        return;
                    }
                }
            }

            ResourceDictionary newRes = new ResourceDictionary()
            {
                Source = new Uri(dictFilePath, UriKind.RelativeOrAbsolute)
            };
            Application.Current.Resources.MergedDictionaries.Add(newRes);
        }

        public List<string> GetThemes()
        {
            return GetDictionaries(THEME_DEFAULT, THEME_FOLDER, THEME_EXT);
        }

        public List<string> GetLanguages()
        {
            List<string> languages = new List<string>();
            languages.Add(LANG_DEFAULT);
            languages.AddRange(GetDictionaries(LANG_FALLBACK, LANG_FOLDER, LANG_EXT));
            return languages;
        }

        public List<string> GetDictionaries(string dictDefault, string dictFolder, string dictExtension)
        {
            List<string> dictionaries = new List<string>();
            dictionaries.Add(dictDefault);

            foreach (string subStr in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dictFolder)).Where(s => Path.GetExtension(s).Contains(dictExtension)))
            {
                dictionaries.Add(Path.GetFileNameWithoutExtension(subStr));
            }

            // Because RetroBar is published as a single-file app, it gets extracted to a temp directory, so custom dictionaries won't be there.
            // Get the executable path to find the custom dictionaries directory when not a debug build.
            string customThemeDir = Path.Combine(Path.GetDirectoryName(ExePath.GetExecutablePath()), dictFolder);

            if (Directory.Exists(customThemeDir))
            {
                foreach (string subStr in Directory.GetFiles(customThemeDir)
                    .Where(s => Path.GetExtension(s).Contains(dictExtension) && !dictionaries.Contains(Path.GetFileNameWithoutExtension(s))))
                {
                    dictionaries.Add(Path.GetFileNameWithoutExtension(subStr));
                }
            }

            return dictionaries;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Language")
            {
                SetLanguageFromSettings();
            }
            if (e.PropertyName == "Theme")
            {
                SetThemeFromSettings();
            }
        }

        public void Dispose()
        {
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
        }
    }
}
