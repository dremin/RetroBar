﻿using ManagedShell.Common.Logging;
using ManagedShell.Common.Logging.Observers;
using System;
using System.IO;

namespace RetroBar.Utilities
{
    class ManagedShellLogger : IDisposable
    {
        private string _logPath = "Logs".InLocalAppData();
        private string _logName = DateTime.Now.ToString("yyyy-MM-dd_HHmmssfff");
        private string _logExt = "log";
        private TimeSpan _logRetention = new TimeSpan(7, 0, 0);
        private FileLog _fileLog;

        public ManagedShellLogger()
        {
            SetupLogging();
            Settings.Instance.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.DebugLogging))
            {
                SetSeverity();
            }
        }

        private void SetSeverity()
        {
            // Handle null settings instance in case of an error while initializing settings
            ShellLogger.Severity = Settings.Instance?.DebugLogging == true ? LogSeverity.Debug : LogSeverity.Info;
        }

        private void SetupLogging()
        {
            SetSeverity();

            SetupFileLog();

            ShellLogger.Attach(new ConsoleLog(), true);
        }

        private void SetupFileLog()
        {
            DeleteOldLogFiles();

            try
            {
                _fileLog = new FileLog(Path.Combine(_logPath, $"{_logName}.{_logExt}"));
                _fileLog?.Open();

                ShellLogger.Attach(_fileLog);
            }
            catch (Exception ex)
            {
                ShellLogger.Error($"Unable to open and attach file logger: {ex.Message}");
            }
        }

        private void DeleteOldLogFiles()
        {
            try
            {
                if (!Directory.Exists(_logPath))
                {
                    // Nothing to delete
                    return;
                }

                // look for all of the log files
                DirectoryInfo info = new DirectoryInfo(_logPath);
                FileInfo[] files = info.GetFiles($"*.{_logExt}", SearchOption.TopDirectoryOnly);

                // delete any files that are older than the retention period
                DateTime now = DateTime.Now;
                foreach (FileInfo file in files)
                {
                    if (now.Subtract(file.LastWriteTime) > _logRetention)
                    {
                        file.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                ShellLogger.Debug($"Unable to delete old log files: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            _fileLog?.Dispose();
        }
    }
}