namespace YtMusicPlayer.Services
{
    // Session-local named kernel objects (no "Global\" prefix) so this only
    // enforces one instance per logged-in user, not machine-wide.
    public sealed class SingleInstanceService : IDisposable
    {
        private const string MutexName = "YtMusicPlayer-SingleInstance-9F3B2C1A";
        private const string ShowEventName = "YtMusicPlayer-ShowRequest-9F3B2C1A";

        private readonly Mutex _mutex;
        private readonly EventWaitHandle _showEvent;

        public bool IsPrimaryInstance { get; }

        public SingleInstanceService()
        {
            _mutex = new Mutex(initiallyOwned: false, MutexName, out var createdNew);
            IsPrimaryInstance = createdNew;
            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        }

        // Runs a background listener that invokes onShowRequested whenever another
        // launch calls NotifyExistingInstance(). Only meaningful for the primary instance.
        public void StartListening(Action onShowRequested)
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    _showEvent.WaitOne();
                    onShowRequested();
                }
            })
            {
                IsBackground = true
            };
            thread.Start();
        }

        public static void NotifyExistingInstance()
        {
            using var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            showEvent.Set();
        }

        public void Dispose()
        {
            _mutex.Dispose();
            _showEvent.Dispose();
        }
    }
}
