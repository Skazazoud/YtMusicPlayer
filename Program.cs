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
    }
}