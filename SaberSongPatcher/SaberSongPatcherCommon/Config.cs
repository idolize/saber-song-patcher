using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SaberSongPatcher
{
    public class Config
    {
        public static readonly string CONFIG_FILE = "audio.json";
        public static readonly string CONFIG_SCHEMA_FILE = "audio.schema.json";

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
                return Hash.GetHashCode();
            }
        }

        public class PatchFadeDuration
        {
            [JsonProperty("startMs")]
            public int StartMs { get; set; } = 0;

            [JsonProperty("durationMs")]
            public int DurationMs { get; set; } = 0;
        }

        public class PatchTrimDuration
        {
            [JsonProperty("startMs")]
            public int? StartMs { get; set; }

            [JsonProperty("endMs")]
            public int? EndMs { get; set; }
        }

        public class PatchesConfig
        {
            [JsonProperty("delayStartMs")]
            public int? DelayStartMs { get; set; }

            [JsonProperty("padEndMs")]
            public int? PadEndMs { get; set; }

            [JsonProperty("trim")]
            public PatchTrimDuration Trim { get; set; }

            [JsonProperty("fadeIn")]
            public PatchFadeDuration FadeIn { get; set; }

            [JsonProperty("fadeOut")]
            public PatchFadeDuration FadeOut { get; set; }

            public bool HasPatches()
            {
                return DelayStartMs != null ||
                    PadEndMs != null ||
                    Trim != null ||
                    FadeIn != null ||
                    FadeOut != null;
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
        public FingerprintConfig Fingerprint { get; set; }

        [JsonProperty("knownGoodHashes")]
        public IList<KnownGoodHash> KnownGoodHashes { get; set; } = new List<KnownGoodHash>();

        [JsonProperty("patches")]
        public PatchesConfig Patches { get; set; } = new PatchesConfig();
    }
}
