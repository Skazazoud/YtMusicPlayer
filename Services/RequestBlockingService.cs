using Microsoft.Web.WebView2.Core;

namespace YtMusicPlayer.Services
{
    // Blocks known third-party ad/analytics infrastructure at the network-request
    // level. Deliberately conservative: only pure ad-tech/analytics domains are
    // listed here - never youtubei.googleapis.com (the actual data API),
    // googlevideo.com (the audio/video CDN), or YouTube's own stats/logging
    // endpoints, so playback, search and recommendations are unaffected.
    internal static class RequestBlockingService
    {
        private static readonly string[] BlockedHostSuffixes =
        [
            "doubleclick.net",
            "googlesyndication.com",
            "googleadservices.com",
            "google-analytics.com",
            "googletagmanager.com",
        ];

        public static void Install(CoreWebView2 core)
        {
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All, CoreWebView2WebResourceRequestSourceKinds.All);
            core.WebResourceRequested += OnWebResourceRequested;
        }

        private static void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (sender is not CoreWebView2 core || !Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var uri))
            {
                return;
            }

            var host = uri.Host;
            var isBlocked = Array.Exists(BlockedHostSuffixes, suffix =>
                host.Equals(suffix, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase));

            if (!isBlocked)
            {
                return;
            }

            // Empty 200 (rather than an error status) so scripts that treat a
            // failed beacon as "retry later" don't keep re-firing the request.
            e.Response = core.Environment.CreateWebResourceResponse(null, 200, "OK", string.Empty);
        }
    }
}
