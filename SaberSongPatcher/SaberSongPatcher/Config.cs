using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace SaberSongPatcher
{
    class Config
    {
        public static readonly string SHA_256_HASH = "sha256";

        public class FingerprintConfig
        {
            [JsonProperty("startAtSecond")]
            public double StartAtSecond { get; set; } = 0;
        }

        public class KnownGoodHash : IEquatable<KnownGoodHash>
        {
            [JsonProperty("type")]
            public string Type { get; set; } = SHA_256_HASH;

            [JsonProperty("hash")]
            public string Hash { get; set; } = string.Empty;

            public override bool Equals([AllowNull] object obj)
            {
                return Equals(obj as KnownGoodHash);
            }

            public bool Equals([AllowNull] KnownGoodHash other)
            {
                return other != null &&
                       Hash == other.Hash;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Hash);
            }
        }

        [JsonIgnore]
        public bool IsChanged { get; set; } = false;

        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonProperty("lengthMs")]
        public int LengthMs { get; set; } = 0;

        [JsonProperty("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonProperty("downloadUrls")]
        public IList<string> DownloadUrls { get; set; } = new List<string>();

        [JsonProperty("fingerprint")]
        public FingerprintConfig? Fingerprint { get; set; }

        [JsonProperty("knownGoodHashes")]
        public IList<KnownGoodHash> KnownGoodHashes { get; set; } = new List<KnownGoodHash>();

        //[JsonProperty("patches")]
        //public IList<string> Patches { get; set; }
    }
}
