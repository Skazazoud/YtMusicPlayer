using Microsoft.Web.WebView2.WinForms;

namespace YtMusicPlayer.Services
{
    // Talks to the YouTube Music page through the WebView2 JS bridge: pushes
    // now-playing state out via window.chrome.webview.postMessage, and drives
    // playback by clicking the page's own player-bar controls.
    internal sealed class YtMusicController
    {
        private const string BridgeScript = """
        (function() {
            if (window.__ytmpBridgeInstalled) return;
            window.__ytmpBridgeInstalled = true;

            function postState() {
                var video = document.querySelector('video');
                var titleEl = document.querySelector('.title.ytmusic-player-bar');
                var artistEl = document.querySelector('.byline.ytmusic-player-bar');
                var artEl = document.querySelector('.image.ytmusic-player-bar');
                window.chrome.webview.postMessage({
                    type: 'nowPlaying',
                    isPlaying: video ? !video.paused : false,
                    title: titleEl ? titleEl.textContent.trim() : '',
                    artist: artistEl ? artistEl.textContent.trim() : '',
                    artworkUrl: artEl ? artEl.src : ''
                });
            }

            function attach() {
                var video = document.querySelector('video');
                if (!video) {
                    setTimeout(attach, 500);
                    return;
                }
                video.addEventListener('play', postState);
                video.addEventListener('pause', postState);
                video.addEventListener('loadedmetadata', postState);
                var bar = document.querySelector('ytmusic-player-bar');
                if (bar) {
                    new MutationObserver(postState).observe(bar, { childList: true, subtree: true, characterData: true });
                }
                postState();
            }

            attach();
        })();
        """;

        private readonly WebView2 _webView;

        public YtMusicController(WebView2 webView)
        {
            _webView = webView;
        }

        public async Task InstallBridgeAsync()
        {
            // Re-injected on every future document load (YT Music's own SPA
            // navigation doesn't reload the document, so this only fires again
            // on a real top-level navigation, e.g. after sign-in).
            await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BridgeScript);
            // Also run once immediately, since the document is already loaded.
            await _webView.CoreWebView2.ExecuteScriptAsync(BridgeScript);
        }

        public Task PlayPauseAsync() => ClickAsync("#play-pause-button");

        public Task NextAsync() => ClickAsync(".next-button");

        public Task PreviousAsync() => ClickAsync(".previous-button");

        private Task ClickAsync(string selector)
        {
            var script = $"(function() {{ var el = document.querySelector('{selector}'); if (el) el.click(); }})();";
            return _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
    }
}
