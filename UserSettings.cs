using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ChannelsNativeTest
{
    // --- NEW: The Data Model for your Custom App Links ---
    public class ExternalStream
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Service { get; set; } = "Netflix";
        public string StreamId { get; set; } = "";
    }

    public class UserSettings
    {
        public bool AutoSkipCommercials { get; set; } = true;
        public bool IsLightTheme { get; set; } = false; 
        public string LastServerAddress { get; set; } = ""; 
        public string LastCollection { get; set; } = "All Channels";
		public bool StartPlayersFullscreen { get; set; } = false;

        // --- NEW: The saved list of external streams ---
        public List<ExternalStream> ExternalStreams { get; set; } = new List<ExternalStream>();
    }

    public static class SettingsManager
    {
        private static readonly string FilePath = "user_settings.json";

        public static UserSettings Load()
        {
            if (File.Exists(FilePath))
            {
                try 
                { 
                    return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(FilePath)) ?? new UserSettings(); 
                }
                catch { return new UserSettings(); }
            }
            return new UserSettings();
        }

        public static void Save(UserSettings settings)
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings));
        }
    }
}