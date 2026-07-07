namespace YtMusicPlayer.Services
{
    internal sealed class TrayService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private bool _hasShownHint;

        public TrayService(Icon icon, string tooltipText, Action onRestore, Action onExit)
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (_, _) => onRestore());
            contextMenu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Run when my computer starts")
            {
                CheckOnClick = true,
                Checked = StartupService.IsEnabled()
            };
            startupItem.Click += (_, _) => StartupService.SetEnabled(startupItem.Checked);
            contextMenu.Items.Add(startupItem);

            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (_, _) => onExit());

            _notifyIcon = new NotifyIcon
            {
                Icon = icon,
                Text = tooltipText,
                ContextMenuStrip = contextMenu,
                Visible = true
            };

            // Left click restores immediately, matching the usual tray-icon convention
            // (right click is left alone to open the ContextMenuStrip as normal).
            _notifyIcon.MouseClick += (_, args) =>
            {
                if (args.Button == MouseButtons.Left)
                {
                    onRestore();
                }
            };
        }

        public void ShowMinimizedHintOnce()
        {
            if (_hasShownHint)
            {
                return;
            }

            _hasShownHint = true;
            _notifyIcon.ShowBalloonTip(
                2000,
                "YT Music Player",
                "Still playing in the background. Click the tray icon to reopen.",
                ToolTipIcon.None);
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
