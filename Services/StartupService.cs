using Microsoft.Win32;

namespace YtMusicPlayer.Services
{
    // Uses the per-user Run key rather than a scheduled task or startup-folder
    // shortcut: no admin rights needed, and it's the same mechanism most tray
    // apps (Discord, Spotify, etc.) rely on for "start with Windows".
    internal static class StartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "YtMusicPlayer";

        // Launch minimized so booting the PC doesn't pop the browser window every time.
        private static string ExpectedCommand => $"\"{Application.ExecutablePath}\" --minimized";

        public static bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is string existing &&
                   string.Equals(existing, ExpectedCommand, StringComparison.OrdinalIgnoreCase);
        }

        public static void SetEnabled(bool enabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

            if (enabled)
            {
                key.SetValue(ValueName, ExpectedCommand);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
    }
}
