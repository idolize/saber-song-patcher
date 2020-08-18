namespace SaberSongPatcher
{
    class Context
    {
        public static readonly string CONFIG_FILE = "audio.json";
        public static readonly string FINGERPRINT_FILE = "fingerprint.bin";

        public Config Config { get; set; }

        public Context()
        {
            Config = new Config();
        }

        public Context(Config config) : this()
        {
            Config = config;
        }
    }
}
