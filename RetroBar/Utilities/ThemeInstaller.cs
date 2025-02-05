using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using ManagedShell.Common.Logging;

namespace RetroBar.Utilities
{
    public class ThemeInstaller
    {
        private readonly DictionaryManager _dictionaryManager;

        public List<string> NewThemes = new List<string>();

        public ThemeInstaller(DictionaryManager dictionaryManager)
        {
            _dictionaryManager = dictionaryManager;
        }

        public bool ShowDialog()
        {

            using (System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog
            {
                Filter = $"{(string)Application.Current.FindResource("theme_files")}|*.xaml;*.png",
                Multiselect = true
            })
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return InstallFiles(dlg.FileNames);
                }
            }

            return false;
        }

        public void ShowNewThemesAlert()
        {
            if (NewThemes.Count < 1)
            {
                return;
            }

            StringBuilder alertMessage = new StringBuilder();
            alertMessage.AppendLine((string)Application.Current.FindResource("themes_installed_text"));

            foreach (string theme in NewThemes)
            {
                alertMessage.AppendLine(theme);
            }

            MessageBox.Show(alertMessage.ToString(), (string)Application.Current.FindResource("themes_installed_title"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool InstallFiles(string[] files)
        {
            bool success = true;

            for (int i = 0; i < files.Length; i++)
            {
                success = InstallFile(files[i]);

                if (!success)
                {
                    break;
                }
            }

            return success;
        }

        private bool InstallFile(string file)
        {
            try
            {
                var fileName = Path.GetFileName(file);
                Directory.CreateDirectory(_dictionaryManager.GetThemeInstallDir());
                File.Copy(file, Path.Combine(_dictionaryManager.GetThemeInstallDir(), fileName), true);

                if (Path.GetExtension(file).ToLower() == ".xaml")
                {
                    NewThemes.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch (Exception e)
            {
                ShellLogger.Error($"ThemeInstaller: Error copying file: {e}");
                System.Windows.MessageBox.Show(string.Format((string)Application.Current.FindResource("error_installing_theme_text"), file), (string)Application.Current.FindResource("error_installing_theme_title"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }
    }
}
