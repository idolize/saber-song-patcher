using System.Diagnostics;

namespace SaberSongPatcher
{
    class Context
    {
        public static readonly string CONFIG_FILE = "audio.json";
        public static readonly string FINGERPRINT_FILE = "fingerprint.bin";

        public enum StatusCodes
        {
            GENERAL = 1000,
            MATCH_FOUND = 1001,

            ERROR_UNKNOWN = 5000,
            ERROR_NO_AUDIO_STREAM = 4001,
            ERROR_FFMPEG_FAILED = 4002,
            ERROR_FILE_ACCESS = 4003,
        }

        public Config Config { get; set; }

        public TraceSource Tracer { get; }

        public Context(Config config)
        {
            Config = config;
            Tracer = new TraceSource("SaberSongPatcher");
        }
    }
}
