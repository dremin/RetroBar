using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace RetroBar
{
    internal sealed class Program
    {
        private const string MutexName = "RetroBar";
        private const int MutexAttempts = 10;
        private const int MutexWaitMs = 1000;
        private static readonly string settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RetroBar", "settings.json");

        private static System.Threading.Mutex _retroBarMutex;

        /// <summary>
        /// The main entry point for the application
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {
            // Check if the -ChangeTheme argument is provided
            if (args.Length > 0 && args[0] == "-ChangeTheme" && args.Length > 1)
            {
                string themeName = args[1];
                ChangeTheme(themeName);
                return 0; // Exit immediately after processing the command
            }

            // If no command line argument is given, start the normal RetroBar application
            if (!SingleInstanceCheck())
            {
                return 1;
            }

            // Proceed with the normal application flow
            App app = new App();
            app.InitializeComponent();
            return app.Run();
        }

        private static bool GetMutex()
        {
            _retroBarMutex = new System.Threading.Mutex(true, MutexName, out bool ok);
            return ok;
        }

        private static bool SingleInstanceCheck()
        {
            for (int i = 0; i < MutexAttempts; i++)
            {
                if (!GetMutex())
                {
                    // Dispose the mutex, otherwise it will never create new
                    _retroBarMutex.Dispose();
                    System.Threading.Thread.Sleep(MutexWaitMs);
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        // Method to change the theme and update the settings.json file
        private static void ChangeTheme(string themeName)
        {
            // Kill any running instances of retrobar.exe
            KillRetroBarProcesses();

            // Ensure the folder exists
            string directoryPath = Path.GetDirectoryName(settingsFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Check if the settings file exists
            if (!File.Exists(settingsFilePath))
            {
                // If file does nott exist, create a new one with the theme property
                Dictionary<string, object> initialSettings = new()
                {
                    ["Theme"] = themeName
                };
                WriteJsonToFile(initialSettings);
                return;
            }

            // Read the existing settings.json file
            try
            {
                string jsonContent = File.ReadAllText(settingsFilePath);

                // Parse the existing JSON into a Dictionary
                Dictionary<string, object> settings = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                // Check if the theme property exists and update it
                if (settings != null)
                {
                    if (settings.ContainsKey("Theme"))
                    {
                        settings["Theme"] = themeName;
                    }
                    else
                    {
                        settings.Add("Theme", themeName);
                    }

                    // Write the updated JSON back to the file
                    WriteJsonToFile(settings);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading or updating settings: {ex.Message}");
            }
        }

        private static void WriteJsonToFile(object jsonObject)
        {
            try
            {
                // Serialize the object to JSON string with indentation
                string jsonString = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });

                // Write the serialized JSON string to the file
                File.WriteAllText(settingsFilePath, jsonString);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to file: {ex.Message}");
            }
        }

        private static void KillRetroBarProcesses()
        {
            try
            {
                // Get the current process ID (this is the one we want to keep running)
                int currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;

                // Get all processes running with the name "retrobar.exe"
                Process[] processes = System.Diagnostics.Process.GetProcessesByName("retrobar");

                // Iterate over all retrobar.exe processes
                foreach (var process in processes)
                {
                    // Skip the process that is currently running this command
                    if (process.Id != currentProcessId)
                    {
                        process.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error killing processes: {ex.Message}");
            }
        }
    }
}
