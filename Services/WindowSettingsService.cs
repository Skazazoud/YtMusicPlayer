using System.Text.Json;

namespace YtMusicPlayer.Services
{
    internal static class WindowSettingsService
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YtMusicPlayer", "window.json");

        public static WindowSettings? Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<WindowSettings>(File.ReadAllText(FilePath));
            }
            catch
            {
                return null;
            }
        }

        public static void Save(WindowSettings settings)
        {
            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (directory is not null)
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(FilePath, JsonSerializer.Serialize(settings));
            }
            catch
            {
                // Best-effort persistence; a failed save just falls back to defaults next launch.
            }
        }
    }
}
