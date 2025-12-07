using ManagedShell.Common.Logging;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace ManagedShell.Common.Helpers
{
    public class SoundHelper
    {
        private const string SYSTEM_SOUND_ROOT_KEY = @"AppEvents\Schemes\Apps";

        /// <summary>
        /// Flag values for playing the sound.
        /// </summary>
        [Flags]
        public enum SND : uint
        {
            /// <summary>
            /// The sound is played synchronously; PlaySound returns after the sound event completes (default behavior).
            /// </summary>
            SYNC = 0x0000,

            /// <summary>
            /// The sound is played asynchronously; PlaySound returns immediately after initiating the sound.
            /// To stop an asynchronously played sound, call PlaySound with pszSound set to NULL.
            /// </summary>
            ASYNC = 0x00000001,

            /// <summary>
            /// No default sound event is used. If the sound is not found, PlaySound returns without playing a sound.
            /// </summary>
            NODEFAULT = 0x00000002,

            /// <summary>
            /// The pszSound parameter points to a sound loaded in memory.
            /// </summary>
            MEMORY = 0x00000004,

            /// <summary>
            /// The sound plays repeatedly until PlaySound is called with pszSound set to NULL.
            /// Use the ASYNC flag with LOOP.
            /// </summary>
            LOOP = 0x00000008,

            /// <summary>
            /// The specified sound event will yield to another sound event already playing in the same process.
            /// If the required resource is busy, the function returns immediately without playing the sound.
            /// </summary>
            NOSTOP = 0x00000010,

            /// <summary>
            /// The pszSound parameter is a system-event alias from the registry or WIN.INI file.
            /// Do not use with FILENAME or RESOURCE.
            /// </summary>
            ALIAS = 0x00010000,

            /// <summary>
            /// The pszSound parameter is a predefined identifier for a system-event alias.
            /// </summary>
            ALIAS_ID = 0x00110000,

            /// <summary>
            /// The pszSound parameter is a file name. If the file is not found, the default sound is played unless NODEFAULT is set.
            /// </summary>
            FILENAME = 0x00020000,

            /// <summary>
            /// The pszSound parameter is a resource identifier; hmod must identify the instance that contains the resource.
            /// </summary>
            RESOURCE = 0x00040004,

            /// <summary>
            /// The pszSound parameter is an application-specific alias in the registry.
            /// Can be combined with ALIAS or ALIAS_ID to specify an application-defined sound alias.
            /// </summary>
            APPLICATION = 0x00000080,

            /// <summary>
            /// Requires Windows Vista or later. If set, triggers a SoundSentry event when the sound is played,
            /// providing a visual cue for accessibility.
            /// </summary>
            SENTRY = 0x00080000,

            /// <summary>
            /// Treats the sound as a ring from a communications app.
            /// </summary>
            RING = 0x00100000,

            /// <summary>
            /// Requires Windows Vista or later. If set, the sound is assigned to the audio session for system notification sounds,
            /// allowing control via the system volume slider. Otherwise, it is assigned to the application's default audio session.
            /// </summary>
            SYSTEM = 0x00200000,
        }

        private const SND DEFAULT_SYSTEM_SOUND_FLAGS = SND.ASYNC | SND.NODEFAULT | SND.SYSTEM;

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PlaySound(string pszSound, IntPtr hmod, SND soundFlags);

        /// <summary>
        /// Plays the specified system sound using the audio session for system notification sounds.
        /// </summary>
        /// <param name="app">The name of the app that the sound belongs to. For example, ".Default" contains system sounds, "Explorer" contains Explorer sounds.</param>
        /// <param name="name">The name of the system sound to play.</param>
        public static bool PlaySystemSound(string app, string name)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey($@"{SYSTEM_SOUND_ROOT_KEY}\{app}\{name}\.Current");
                if (key == null)
                {
                    ShellLogger.Debug($"SoundHelper: Unable to find sound {name} for app {app}");
                    return false;
                }

                var soundFileName = key.GetValue(null) as string;
                if (string.IsNullOrEmpty(soundFileName))
                {
                    ShellLogger.Debug($"SoundHelper: Missing file for sound {name} for app {app}");
                    return false;
                }

                return PlaySound(soundFileName, IntPtr.Zero, DEFAULT_SYSTEM_SOUND_FLAGS | SND.FILENAME);
            }
            catch (Exception e)
            {
                ShellLogger.Debug($"SoundHelper: Unable to play sound {name} for app {app}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Plays the specified system sound using the audio session for system notification sounds.
        /// </summary>
        /// <param name="alias">The name of the system sound for ".Default" to play.</param>
        public static bool PlaySystemSound(string alias)
        {
            try
            {
                return PlaySound(alias, IntPtr.Zero, DEFAULT_SYSTEM_SOUND_FLAGS | SND.ALIAS);
            }
            catch (Exception e)
            {
                ShellLogger.Debug($"SoundHelper: Unable to play sound {alias}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Plays the system notification sound.
        /// </summary>
        public static void PlayNotificationSound()
        {
            // System default sound for the classic notification balloon.
            if (PlaySystemSound("Explorer", "SystemNotification")) return;
            if (EnvironmentHelper.IsWindows8OrBetter)
            {
                // Toast notification sound.
                if (!PlaySystemSound("Notification.Default"))
                    PlayXPNotificationSound();
            }
            else
            {
                PlayXPNotificationSound();
            }
        }

        public static void PlayXPNotificationSound()
        {
            PlaySystemSound("SystemNotification");
        }
    }
}