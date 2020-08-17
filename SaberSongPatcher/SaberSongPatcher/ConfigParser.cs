using Newtonsoft.Json;
using System.IO;

namespace SaberSongPatcher
{
    class ConfigParser
    {
        public static Config ParseConfig()
        {
            try
            {
                // Deserialize JSON directly from a file
                using (StreamReader file = File.OpenText(Context.CONFIG_FILE))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    return (Config)serializer.Deserialize(file, typeof(Config));
                }
            } catch (FileNotFoundException ex)
            {
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
                // Serialize JSON directly to a file
                using (StreamWriter file = File.CreateText(Context.CONFIG_FILE))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, config);
                }
            }
        }
    }
}
