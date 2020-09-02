using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SaberSongPatcher
{
    public class ConfigParser
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

        public static Config ParseConfig(bool strict, string configDirectory)
        {
            Logger.Debug("Parsing config file...");

            var filePath = Path.HasExtension(configDirectory) ?
                configDirectory : Path.Combine(configDirectory, Config.CONFIG_FILE);
            if (!File.Exists(filePath))
            {
                if (strict)
                {
                    throw new FileNotFoundException($"No '{Config.CONFIG_FILE}' config file found", filePath);
                }

                Logger.Debug("No config file found, using default config");
                return new Config
                {
                    IsChanged = true
                };
            }

            JSchema schema;
            var schemaFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                Config.CONFIG_SCHEMA_FILE);
            using (StreamReader file = File.OpenText(schemaFile))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                schema = JSchema.Load(reader);
            }

            // Deserialize JSON directly from a file
            var validationMessages = new List<string>();
            using (StreamReader file = File.OpenText(filePath))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JSchemaValidatingReader validatingReader = new JSchemaValidatingReader(reader)
                {
                    Schema = schema
                };
                JsonSerializer serializer = new JsonSerializer();
                
                validatingReader.ValidationEventHandler += (o, a) => validationMessages.Add(a.Message);
                try
                {
                    var config = serializer.Deserialize<Config>(validatingReader);
                    if (validationMessages.Count > 0)
                    {
                        throw new JsonReaderException();
                    } else
                    {
                        Logger.Debug("Config file parsed");
                    }
                    return config;
                } catch (JsonReaderException ex)
                {
                    Logger.Error("Invalid {filename} config file format", Config.CONFIG_FILE);
                    foreach (var message in validationMessages)
                    {
                        Logger.Error(message);
                    }
                    throw ex;
                }
            }
        }

        public static Config ParseConfig(bool strict)
        {
            return ParseConfig(strict, string.Empty);
        }

        public static void FlushConfigChanges(Config config, string configDirectory)
        {
            if (config.IsChanged)
            {
                Logger.Info("Updating config...");

                var filePath = Path.Combine(configDirectory, Config.CONFIG_FILE);
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
                Logger.Info("{file} saved to {filePath}", Config.CONFIG_FILE, Path.GetFullPath(filePath));
            } else {
                Logger.Debug("Config not changed");
            }
        }

        public static void FlushConfigChanges(Config config)
        {
            FlushConfigChanges(config, string.Empty);
        }
    }
}
