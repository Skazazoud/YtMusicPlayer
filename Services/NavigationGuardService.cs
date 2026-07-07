using System.Diagnostics;
using Microsoft.Web.WebView2.Core;

namespace YtMusicPlayer.Services
{
    // Keeps the WebView2 confined to YouTube/Google's own domains. Without this,
    // a link, redirect, or dropped URL could navigate the main window to arbitrary
    // content with no address bar to warn the user - and our JS bridge would still
    // be live on whatever loaded, since it re-injects on every document. Anything
    // outside the allowlist (and every new-window/target=_blank request) is sent
    // to the system's default browser instead, where a real address bar exists.
    internal static class NavigationGuardService
    {
        private static readonly string[] AllowedNavigationDomains =
        [
            "youtube.com",
            "google.com",
            "gstatic.com",
            "ytimg.com",
            "googleusercontent.com",
        ];

        public static void Install(CoreWebView2 core)
        {
            core.NavigationStarting += OnNavigationStarting;
            core.NewWindowRequested += OnNewWindowRequested;
        }

        private static void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (IsAllowed(e.Uri))
            {
                return;
            }

            e.Cancel = true;
            OpenExternally(e.Uri);
        }

        private static void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Never spawn a second WebView2 popup window - this app has no chrome to
            // manage one (no address bar, no tray/SMTC integration for it, etc.), so
            // every new-window request goes to the system browser instead.
            e.Handled = true;
            OpenExternally(e.Uri);
        }

        private static bool IsAllowed(string uriString)
        {
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
            {
                return true;
            }

            if (uri.Scheme is not ("http" or "https"))
            {
                // Non-web schemes (about:, data:, blob:) aren't the navigation-hijack
                // vector this guards against; only remote http(s) destinations are restricted.
                return true;
            }

            var host = uri.Host;
            return Array.Exists(AllowedNavigationDomains, domain =>
                host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));
        }

        private static void OpenExternally(string uri)
        {
            try
            {
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
            catch
            {
                // Best-effort; nothing more we can do if the shell can't handle it.
            }
        }
    }
}
