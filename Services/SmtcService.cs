using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace YtMusicPlayer.Services
{
    internal sealed class SmtcService : IDisposable
    {
        // A MediaPlayer is never actually used for playback here - it's the only
        // supported way for a classic desktop (non-UWP) app to obtain a
        // SystemMediaTransportControls instance.
        private readonly MediaPlayer _mediaPlayer;
        private readonly SystemMediaTransportControls _smtc;

        public event Action? PlayRequested;
        public event Action? PauseRequested;
        public event Action? NextRequested;
        public event Action? PreviousRequested;

        public SmtcService()
        {
            _mediaPlayer = new MediaPlayer();
            _smtc = _mediaPlayer.SystemMediaTransportControls;
            _smtc.IsEnabled = true;
            _smtc.IsPlayEnabled = true;
            _smtc.IsPauseEnabled = true;
            _smtc.IsNextEnabled = true;
            _smtc.IsPreviousEnabled = true;
            _smtc.ButtonPressed += OnButtonPressed;
        }

        private void OnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    PlayRequested?.Invoke();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    PauseRequested?.Invoke();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    NextRequested?.Invoke();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    PreviousRequested?.Invoke();
                    break;
            }
        }

        public void UpdatePlaybackStatus(bool isPlaying)
        {
            _smtc.PlaybackStatus = isPlaying
                ? MediaPlaybackStatus.Playing
                : MediaPlaybackStatus.Paused;
        }

        public void UpdateMetadata(string title, string artist, string? artworkUrl)
        {
            var updater = _smtc.DisplayUpdater;
            updater.Type = MediaPlaybackType.Music;
            updater.MusicProperties.Title = title;
            updater.MusicProperties.Artist = artist;

            updater.Thumbnail = !string.IsNullOrEmpty(artworkUrl) && Uri.TryCreate(artworkUrl, UriKind.Absolute, out var uri)
                ? RandomAccessStreamReference.CreateFromUri(uri)
                : null;

            updater.Update();
        }

        public void Dispose()
        {
            _smtc.ButtonPressed -= OnButtonPressed;
            _smtc.IsEnabled = false;
            _mediaPlayer.Dispose();
        }
    }
}
