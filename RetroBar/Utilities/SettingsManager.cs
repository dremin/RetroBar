using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace RetroBar.Utilities
{
    public class SettingsManager<T> : INotifyPropertyChanged where T : new()
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _fileName;

        private T _settings;
        public T Settings
        {
            get { return _settings; }

            set
            {
                _settings = value;
                OnPropertyChanged();
                SaveToFile();
            }
        }

        public SettingsManager(string fileName, T defaultSettings)
        {
            _fileName = fileName;
            _settings = defaultSettings;

            if (!LoadFromFile())
            {
                ShellLogger.Info("SettingsManager: Using default settings");
            }
        }

        private bool LoadFromFile()
        {
            try
            {
                if (!ShellHelper.Exists(_fileName))
                {
                    return false;
                }

                string jsonString = File.ReadAllText(_fileName);
                _settings = JsonSerializer.Deserialize<T>(jsonString);
                return true;
            }
            catch (Exception ex)
            {
                ShellLogger.Error($"SettingsManager: Error loading settings file: {ex.Message}");
                return false;
            }
        }

        private void SaveToFile()
        {
            JsonSerializerOptions options = new()
            {
                IgnoreReadOnlyProperties = true,
                WriteIndented = true
            };

            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(_fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_fileName));
                }

                string jsonString = JsonSerializer.Serialize(Settings, options);
                File.WriteAllText(_fileName, jsonString);
            }
            catch (Exception ex)
            {
                ShellLogger.Error($"SettingsManager: Error saving settings file: {ex.Message}");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}