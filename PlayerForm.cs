using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using YtMusicPlayer.Services;

namespace YtMusicPlayer
{
    public partial class PlayerForm : Form
    {
        private const string YtMusicUrl = "https://music.youtube.com";

        private readonly SingleInstanceService _singleInstance;
        private TrayService? _trayService;
        private YtMusicController? _ytMusicController;
        private SmtcService? _smtcService;
        private FormWindowState _lastNonMinimizedState = FormWindowState.Normal;
        private bool _isExiting;

        public PlayerForm(SingleInstanceService singleInstance)
        {
            _singleInstance = singleInstance;
            InitializeComponent();

            // The WebView2 child window doesn't exist until EnsureCoreWebView2Async
            // completes (that's an async, non-instant call), so for that whole gap
            // it's actually the Form's own client area showing through, not WebView2's
            // background - hence setting BackColor here too, not just DefaultBackgroundColor.
            var backgroundColor = ThemeService.IsSystemInDarkMode() ? Color.Black : Color.White;
            BackColor = backgroundColor;
            webView.DefaultBackgroundColor = backgroundColor;

            ApplySavedWindowBounds();

            if (Array.Exists(Environment.GetCommandLineArgs(), a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase)))
            {
                WindowState = FormWindowState.Minimized;
            }

            Load += PlayerForm_Load;
            Resize += PlayerForm_Resize;
            ResizeEnd += PlayerForm_ResizeEnd;
            FormClosing += PlayerForm_FormClosing;
        }

        private void ApplySavedWindowBounds()
        {
            var saved = WindowSettingsService.Load();
            if (saved is null || saved.Width <= 0 || saved.Height <= 0)
            {
                return;
            }

            if (SystemInformation.VirtualScreen.IntersectsWith(new Rectangle(saved.X, saved.Y, saved.Width, saved.Height)))
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(saved.X, saved.Y);
            }

            Size = new Size(saved.Width, saved.Height);

            if (saved.IsMaximized)
            {
                WindowState = FormWindowState.Maximized;
                _lastNonMinimizedState = FormWindowState.Maximized;
            }
        }

        private void SaveWindowBounds()
        {
            var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            WindowSettingsService.Save(new WindowSettings(
                bounds.X, bounds.Y, bounds.Width, bounds.Height,
                _lastNonMinimizedState == FormWindowState.Maximized));
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ThemeService.ApplyTitleBarTheme(Handle);
        }

        private async void PlayerForm_Load(object? sender, EventArgs e)
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YtMusicPlayer", "WebView2");

            // Trims background Chromium activity we don't need for a single-site,
            // no-extensions music player. Deliberately NOT included:
            // --disable-component-update (risks a stale Widevine DRM component),
            // --disable-renderer-backgrounding / --disable-backgrounding-occluded-windows
            // (would undo the minimize-to-tray throttling), --mute-audio (obviously not).
            const string additionalBrowserArguments =
                "--disable-background-networking " +
                "--disable-domain-reliability " +
                "--disable-breakpad " +
                "--disable-sync " +
                "--disable-features=HardwareMediaKeyHandling";

            var environmentOptions = new CoreWebView2EnvironmentOptions(additionalBrowserArguments);
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder, options: environmentOptions);
            await webView.EnsureCoreWebView2Async(environment);
            RequestBlockingService.Install(webView.CoreWebView2);
            webView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;

            // The exe's own embedded icon (see <ApplicationIcon> in the csproj) is the
            // YT Music logo, so extracting it here covers both the window/taskbar icon
            // and the tray icon without needing to fetch anything at runtime.
            var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
            Icon = appIcon;
            _trayService = new TrayService(appIcon, "YT Music Player", RestoreFromTray, ExitApplication);

            _ytMusicController = new YtMusicController(webView);
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            _smtcService = new SmtcService();
            _smtcService.PlayRequested += () => RunOnUiThread(() => _ytMusicController!.PlayPauseAsync());
            _smtcService.PauseRequested += () => RunOnUiThread(() => _ytMusicController!.PlayPauseAsync());
            _smtcService.NextRequested += () => RunOnUiThread(() => _ytMusicController!.NextAsync());
            _smtcService.PreviousRequested += () => RunOnUiThread(() => _ytMusicController!.PreviousAsync());

            webView.CoreWebView2.Navigate(YtMusicUrl);
            await _ytMusicController.InstallBridgeAsync();

            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            _singleInstance.StartListening(() => BeginInvoke(new Action(RestoreFromTray)));
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
            {
                ThemeService.ApplyTitleBarTheme(Handle);
            }
        }

        private void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            switch (e.ProcessFailedKind)
            {
                case CoreWebView2ProcessFailedKind.RenderProcessExited:
                    // WebView2 already spun up a fresh render process and navigated it
                    // to an error page - Reload() is Microsoft's documented recovery path.
                    webView.CoreWebView2.Reload();
                    break;

                case CoreWebView2ProcessFailedKind.BrowserProcessExited:
                    // The whole WebView2 browser process died; per Microsoft's docs the
                    // control is now unusable and a new one must be created. Simplest
                    // reliable recovery here is a full app restart.
                    RestartApplication();
                    break;
            }
        }

        private void RestartApplication()
        {
            // Release the single-instance mutex before launching the replacement
            // process, so it doesn't mistake this (still-dying) process for an
            // already-running instance and just signal it instead of starting fresh.
            _singleInstance.Dispose();
            Process.Start(Application.ExecutablePath);
            Environment.Exit(0);
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            NowPlayingMessage? message;
            try
            {
                message = System.Text.Json.JsonSerializer.Deserialize<NowPlayingMessage>(e.WebMessageAsJson);
            }
            catch
            {
                return;
            }

            if (message is null || message.Type != "nowPlaying")
            {
                return;
            }

            _smtcService?.UpdatePlaybackStatus(message.IsPlaying);
            _smtcService?.UpdateMetadata(message.Title ?? string.Empty, message.Artist ?? string.Empty, message.ArtworkUrl);
        }

        private void RunOnUiThread(Func<Task> action)
        {
            if (IsDisposed)
            {
                return;
            }

            BeginInvoke(new Action(async () =>
            {
                try
                {
                    await action();
                }
                catch
                {
                    // Best-effort remote control; the page may not be ready yet.
                }
            }));
        }

        private sealed record NowPlayingMessage(
            [property: JsonPropertyName("type")] string Type,
            [property: JsonPropertyName("isPlaying")] bool IsPlaying,
            [property: JsonPropertyName("title")] string? Title,
            [property: JsonPropertyName("artist")] string? Artist,
            [property: JsonPropertyName("artworkUrl")] string? ArtworkUrl);

        private void PlayerForm_Resize(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
            else
            {
                _lastNonMinimizedState = WindowState;
            }
        }

        private void PlayerForm_ResizeEnd(object? sender, EventArgs e)
        {
            SaveWindowBounds();
        }

        private void PlayerForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!_isExiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MinimizeToTray();
                return;
            }

            SaveWindowBounds();
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            _trayService?.Dispose();
            _smtcService?.Dispose();
            _singleInstance.Dispose();
        }

        private void MinimizeToTray()
        {
            SaveWindowBounds();

            // Form.Hide() performs a real Win32 ShowWindow(SW_HIDE) on the native window,
            // which Chromium detects the same way it detects a backgrounded browser tab,
            // so WebView2 throttles rendering without any extra API calls.
            Hide();
            _trayService?.ShowMinimizedHintOnce();
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            _isExiting = true;
            Close();
        }
    }
}
