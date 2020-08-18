﻿using System.Security.Cryptography;
using System.IO;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Emy;
using System.Collections.Generic;
using ProtoBuf;
using System.Threading.Tasks;
using SoundFingerprinting.Data;
using SoundFingerprinting.Configuration;
using System;

namespace SaberSongPatcher
{
    class HashCalculator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly Context context;

        public HashCalculator(Context context)
        {
            this.context = context;
        }

        public static string GetSha256(string inputFile)
        {
            using (SHA256 SHA256 = SHA256Managed.Create())
            {
                using (FileStream fileStream = File.OpenRead(inputFile))
                    return Convert.ToBase64String(SHA256.ComputeHash(fileStream));
            }
        }

        public async Task<bool> SaveHashesFromMaster(string masterAudioFile)
        {
            Logger.Info("Saving hashes...");
            // 1. Calculate the SHA-256 and make sure it's in the known good hashes list
            var sha256 = GetSha256(masterAudioFile);
            var knownGoodHashes = new List<Config.KnownGoodHash>(context.Config.KnownGoodHashes);
            var hash = new Config.KnownGoodHash
            {
                Type = Config.SHA_256_HASH,
                Hash = sha256
            };
            if (!knownGoodHashes.Contains(hash))
            {
                Logger.Info("Adding SHA256 hash to known good list");
                knownGoodHashes.Add(hash);
                context.Config.KnownGoodHashes = knownGoodHashes;
                context.Config.IsChanged = true;
            }

            // 2. Fingerprint the audio
            Logger.Info("Fingerprinting audio...");
            var audioService = new FFmpegAudioService();

            // HACK soundfingerprinting library assumes current directory is always exe directory
            var masterAudioFullPath = Path.GetFullPath(masterAudioFile);
            var prevCurrentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(context.ExeDirectory);
            Hashes hashedFingerprints;
            try
            {
                hashedFingerprints = await FingerprintCommandBuilder.Instance
                                            .BuildFingerprintCommand()
                                            .From(masterAudioFullPath)
                                            .WithFingerprintConfig(new LowLatencyFingerprintConfiguration())
                                            .UsingServices(audioService)
                                            .Hash();
            }
            catch (DllNotFoundException ex)
            {
                if (ex.Message.Contains(".dll"))
                {
                    Logger.Error(ex, "Unable to find ffmpeg DLLs - make sure ffmpeg files are in \\FFmpeg\\bin\\x64");
                } else
                {
                    Logger.Error(ex);
                }
                return false;
            } finally
            {
                Directory.SetCurrentDirectory(prevCurrentDirectory);
            }

            // 3. Save the fingerprint proto
            // https://github.com/protobuf-net/protobuf-net#2-serialize-your-data

            using (var file = File.Create(Context.FINGERPRINT_FILE))
            {
                Logger.Info("Serializing fingerprints...");
                Serializer.Serialize(file, hashedFingerprints);
                Logger.Info("{file} created in directory {directory}",
                    Context.FINGERPRINT_FILE, Path.GetDirectoryName(Path.GetFullPath(Context.FINGERPRINT_FILE)));
            }

            // TODO 4. Save the duration of the song to the config
            return true;
        }
    }
}
