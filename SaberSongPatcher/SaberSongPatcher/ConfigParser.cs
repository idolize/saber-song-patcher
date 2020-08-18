using Newtonsoft.Json;
using System.IO;

namespace SaberSongPatcher
{
    class ConfigParser
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static Config ParseConfig()
        {
            try
            {
                Logger.Debug("Parsing config file...");
                // Deserialize JSON directly from a file
                using (StreamReader file = File.OpenText(Context.CONFIG_FILE))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    return (Config)serializer.Deserialize(file, typeof(Config))!;
                }
            } catch (FileNotFoundException)
            {
                Logger.Debug("No config file found, using default config");
                return new Config
                {
                    IsChanged = true
                };
            }
        }

        public static void FlushConfigChanges(Config config)
        {
            if (config.IsChanged)
            {
                Logger.Info("Updating config...");
                // Serialize JSON directly to a file
                using (StreamWriter file = File.CreateText(Context.CONFIG_FILE))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, config);
                }
            } else {
                Logger.Debug("Config not changed");
            }
        }
    }
}
