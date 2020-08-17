using System;
using System.Collections.Generic;
using System.Text;

namespace SaberSongPatcher
{
    class Context
    {
        public static readonly string CONFIG_FILE = "audio.json";
        public static readonly string FINGERPRINT_FILE = "fingerprint.bin";

        public Config Config { get; set; }

        public Context(Config config) => Config = config;
    }
}
