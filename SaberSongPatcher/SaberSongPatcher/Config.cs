using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SaberSongPatcher
{
    class Config
    {
        public class FingerprintConfig
        {
            [JsonProperty("startAtSecond")]
            public double StartAtSecond { get; set; } = 0;
        }

        public class KnownGoodHash : IEquatable<KnownGoodHash>
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("hash")]
            public string Hash { get; set; }

            public override bool Equals(object obj)
            {
                return Equals(obj as KnownGoodHash);
            }

            public bool Equals(KnownGoodHash other)
            {
                return other != null &&
                       Hash == other.Hash;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Hash);
            }
        }

        public bool IsChanged { get; set; } = false;

        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonProperty("lengthMs")]
        public int LengthMs { get; set; } = 0;

        [JsonProperty("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonProperty("downloadUrls")]
        public IList<string> DownloadUrls { get; set; }

        [JsonProperty("fingerprint")]
        public FingerprintConfig Fingerprint { get; set; }

        [JsonProperty("knownGoodHashes")]
        public IList<KnownGoodHash> KnownGoodHashes { get; set; }

        //[JsonProperty("patches")]
        //public IList<string> Patches { get; set; }
    }
}
