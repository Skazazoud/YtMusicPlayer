using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using YtMusicPlayer.Services;

namespace YtMusicPlayer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!IsWebView2RuntimeAvailable())
            {
                MessageBox.Show(
                    "YT Music Player needs the Microsoft Edge WebView2 Runtime, which isn't installed on this PC.\n\n" +
                    "It's a small, free download from Microsoft - most Windows 10/11 PCs already have it since it ships with Edge. " +
                    "Your browser will open to the download page now.",
                    "WebView2 Runtime required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                Process.Start(new ProcessStartInfo("https://developer.microsoft.com/en-us/microsoft-edge/webview2/")
                {
                    UseShellExecute = true
                });
                return;
            }

            var singleInstance = new SingleInstanceService();
            if (!singleInstance.IsPrimaryInstance)
            {
                // Another instance is already running - tell it to show itself
                // and exit immediately, before touching WebView2 or the tray.
                SingleInstanceService.NotifyExistingInstance();
                singleInstance.Dispose();
                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new PlayerForm(singleInstance));
        }

        private static bool IsWebView2RuntimeAvailable()
        {
            try
            {
                CoreWebView2Environment.GetAvailableBrowserVersionString(null);
                return true;
            }
            catch (WebView2RuntimeNotFoundException)
            {
                return false;
            }
        }
    }
}