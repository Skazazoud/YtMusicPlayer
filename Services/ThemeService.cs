using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace YtMusicPlayer.Services
{
    internal static class ThemeService
    {
        private const int DwmwaUseImmersiveDarkMode = 20;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static bool IsSystemInDarkMode()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            // AppsUseLightTheme: 1 = light, 0 = dark. Missing key means the OS default (light).
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }

        public static void ApplyTitleBarTheme(IntPtr handle)
        {
            var useDark = IsSystemInDarkMode() ? 1 : 0;
            DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int));
        }
    }
}
