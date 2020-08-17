using System.Security.Cryptography;
using System.Text;
using System.IO;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Emy;
using System.Collections.Generic;
using ProtoBuf;

namespace SaberSongPatcher
{
    class HashCalculator
    {
        private readonly Context context;

        public HashCalculator(Context context)
        {
            this.context = context;
        }

        public static string GetSha256(string inputFile)
        {
            using (SHA256 mySHA256 = SHA256.Create())
            {
                FileStream fileStream = File.OpenRead(inputFile);
                // Be sure it's positioned to the beginning of the stream.
                fileStream.Position = 0;
                // Compute the hash of the fileStream.
                byte[] hashValue = mySHA256.ComputeHash(fileStream);
                fileStream.Close();

                StringBuilder sb = new StringBuilder();
                // Display the byte array in a readable format.
                for (int i = 0; i < hashValue.Length; i++)
                {
                    sb.Append($"{hashValue[i]:X2}");
                    if ((i % 4) == 3) sb.Append(" ");
                }
                return sb.ToString();
            }
        }

        public async void SaveHashesFromMaster(string masterAudioFile)
        {
            // 1. Calculate the SHA-256 and make sure it's in the known good hashes list
            var sha256 = GetSha256(masterAudioFile);
            var knownGoodHashes = new List<Config.KnownGoodHash>(context.Config.KnownGoodHashes);
            var hash = new Config.KnownGoodHash
            {
                Type = "sha256",
                Hash = sha256
            };
            if (!knownGoodHashes.Contains(hash))
            {
                knownGoodHashes.Add(hash);
                context.Config.KnownGoodHashes = knownGoodHashes;
            }

            // 2. Fingerprint the audio
            IAudioService audioService = new FFmpegAudioService();
            
            var hashedFingerprints = await FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From(masterAudioFile)
                                        .UsingServices(audioService)
                                        .Hash();

            // 3. Save the fingerprint proto
            // https://github.com/protobuf-net/protobuf-net#2-serialize-your-data

            using (var file = File.Create(Context.FINGERPRINT_FILE))
            {
                Serializer.Serialize(file, hashedFingerprints);
            }

            // TODO 4. Save the duration of the song to the config
        }
    }
}
