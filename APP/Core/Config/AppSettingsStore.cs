using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Storage;

namespace APP.Core.Config
{
    public class AppSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string _filePath;

        public AppSettingsStore()
            : this(Path.Combine(FileSystem.AppDataDirectory, "settings.json"))
        {
        }

        public AppSettingsStore(string filePath)
        {
            _filePath = filePath;
        }

        public RuntimeConfig Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return CreateDefaultConfig();

                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<AppSettingsDto>(json, JsonOptions);
                return settings?.ToRuntimeConfig() ?? CreateDefaultConfig();
            }
            catch
            {
                return CreateDefaultConfig();
            }
        }

        public void Save(RuntimeConfig config)
        {
            try
            {
                var folder = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrWhiteSpace(folder))
                    Directory.CreateDirectory(folder);

                var settings = AppSettingsDto.FromConfig(config);
                var json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Settings are helpful, but the timer should still work if saving fails.
            }
        }

        private static RuntimeConfig CreateDefaultConfig()
        {
            return new RuntimeConfig(
                Constants.DefaultCycles,
                Constants.DefaultFocusDuration,
                Constants.DefaultBreakDuration);
        }

        private class AppSettingsDto
        {
            public int Cycles { get; set; } = Constants.DefaultCycles;
            public int FocusMinutes { get; set; } = (int)Constants.DefaultFocusDuration.TotalMinutes;
            public int BreakMinutes { get; set; } = (int)Constants.DefaultBreakDuration.TotalMinutes;
            public bool StrictModeEnabled { get; set; } = true;
            public bool VibrationEnabled { get; set; } = true;
            public bool KeepScreenOnEnabled { get; set; } = true;
            public FlipTheme Theme { get; set; } = FlipTheme.TropicalSunrise;

            public RuntimeConfig ToRuntimeConfig()
            {
                var cycles = Math.Max(1, Cycles);
                var focusMinutes = Math.Max(1, FocusMinutes);
                var breakMinutes = Math.Max(1, BreakMinutes);

                return new RuntimeConfig(
                    cycles,
                    TimeSpan.FromMinutes(focusMinutes),
                    TimeSpan.FromMinutes(breakMinutes))
                {
                    StrictModeEnabled = StrictModeEnabled,
                    VibrationEnabled = VibrationEnabled,
                    KeepScreenOnEnabled = KeepScreenOnEnabled,
                    Theme = Theme
                };
            }

            public static AppSettingsDto FromConfig(RuntimeConfig config)
            {
                return new AppSettingsDto
                {
                    Cycles = config.Cycles,
                    FocusMinutes = (int)config.FocusDuration.TotalMinutes,
                    BreakMinutes = (int)config.BreakDuration.TotalMinutes,
                    StrictModeEnabled = config.StrictModeEnabled,
                    VibrationEnabled = config.VibrationEnabled,
                    KeepScreenOnEnabled = config.KeepScreenOnEnabled,
                    Theme = config.Theme
                };
            }
        }
    }
}
