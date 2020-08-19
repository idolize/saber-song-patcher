using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.IO;

namespace SaberSongPatcher
{
    class ConfigParser
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Converters = new[] { new StringEnumConverter(new CamelCaseNamingStrategy(), false) },
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

    public static Config ParseConfig(string? configDirectory, bool strict)
        {
            Logger.Debug("Parsing config file...");

            var filePath = Path.Join(configDirectory, Context.CONFIG_FILE);
            if (!File.Exists(filePath))
            {
                if (strict)
                {
                    throw new FileNotFoundException($"No '{Context.CONFIG_FILE}' config file found", filePath);
                }

                Logger.Debug("No config file found, using default config");
                return new Config
                {
                    IsChanged = true
                };
            }

            // Deserialize JSON directly from a file
            using (StreamReader file = File.OpenText(filePath))
            {
                JsonSerializer serializer = JsonSerializer.Create(JsonSettings);
                try
                {
                    var config = (Config)serializer.Deserialize(file, typeof(Config))!;
                    Logger.Debug("Config file parsed");
                    return config;
                } catch (JsonReaderException ex)
                {
                    Logger.Error("Invalid {filename} config file format", Context.CONFIG_FILE);
                    throw ex;
                }
            }
        }

        public static void FlushConfigChanges(Config config, string? configDirectory)
        {
            if (config.IsChanged)
            {
                Logger.Info("Updating config...");

                var filePath = Path.Join(configDirectory, Context.CONFIG_FILE);
                if (File.Exists(filePath))
                {
                    Logger.Debug("Deleting existing config at {filePath}", filePath);
                    File.Delete(filePath);
                }

                // Serialize JSON directly to a file
                using (StreamWriter file = File.CreateText(filePath))
                {
                    JsonSerializer serializer = JsonSerializer.Create(JsonSettings);
                    serializer.Serialize(file, config);
                }
                Logger.Info("{file} saved to {filePath}", Context.CONFIG_FILE, Path.GetFullPath(filePath));
            } else {
                Logger.Debug("Config not changed");
            }
        }
    }
}
