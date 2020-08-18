using System;
using System.Threading.Tasks;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using System.IO;
using ProtoBuf;
using SoundFingerprinting.Configuration;
using SoundFingerprinting.Query;

namespace SaberSongPatcher
{
    class InputValidator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly int SECONDS_TO_ANALYZE = 10; // number of seconds to analyze from query file
        private static readonly double CONFIDENCE_THRESHOLD = 0.9; // how confident we need to be to consider it a match

        private readonly Context context;

        public InputValidator(Context context)
        {
            this.context = context;
        }

        private async Task<bool> CheckFingerprint(string queryAudioFile)
        {
            IModelService modelService = new InMemoryModelService(); // store fingerprints in RAM
            IAudioService audioService = new FFmpegAudioService();

            // Load the serialized fingerprint hashes from disk
            // https://github.com/protobuf-net/protobuf-net#3-deserialize-your-data

            Hashes fingerprints;
            try
            {
                using (var file = File.OpenRead(Context.FINGERPRINT_FILE))
                {
                    fingerprints = Serializer.Deserialize<Hashes>(file);
                }
            } catch (SystemException ex)
            {
                Logger.Error(ex, "Failed to access fingerprint file");
                return false;
            }

            // Since we only have one song this part doesn't matter
            var track = new TrackInfo("123", "Test Song", "Test Artist");
            modelService.Insert(track, fingerprints);

            double startAtSecond = context.Config.Fingerprint?.StartAtSecond ?? 0;
            Logger.Debug("Analyzing ${secsToAnalyze} seconds of audio starting at {startAtSecond} seconds",
                SECONDS_TO_ANALYZE, startAtSecond);

            // HACK soundfingerprinting library assumes current directory is always exe directory
            var queryAudioFullPath = Path.GetFullPath(queryAudioFile);
            var prevCurrentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(context.ExeDirectory);
            QueryResult queryResult;
            try
            {
                queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                     .From(queryAudioFullPath, SECONDS_TO_ANALYZE, startAtSecond)
                                                     .WithQueryConfig(new LowLatencyQueryConfiguration())
                                                     .UsingServices(modelService, audioService)
                                                     .Query();
            }
            finally
            {
                Directory.SetCurrentDirectory(prevCurrentDirectory);
            }

            var match = queryResult.BestMatch;
            if (match == null)
            {
                Logger.Debug("No fingerprint match found");
                return false;
            }

            // https://github.com/AddictedCS/soundfingerprinting/wiki/Different-Types-of-Coverage
            Logger.Debug("Match found!");
            Logger.Debug($"Confidence {match.Confidence} < {CONFIDENCE_THRESHOLD} = {match.Confidence < CONFIDENCE_THRESHOLD}");
            Logger.Debug("QueryRelativeCoverage=" + match.QueryRelativeCoverage);
            Logger.Debug("DiscreteCoverageLength=" + match.DiscreteCoverageLength);
            Logger.Debug("CoverageWithPermittedGapsLength=" + match.CoverageWithPermittedGapsLength);
            Logger.Debug("CoverageLength=" + match.CoverageLength);
            Logger.Debug("TrackStartsAt=" + match.TrackStartsAt);
            Logger.Debug("TrackMatchStartsAt=" + match.TrackMatchStartsAt);

            return match.Confidence > CONFIDENCE_THRESHOLD;
        }

        public async Task<bool> ValidateInput(string queryAudioFile)
        {
            Logger.Info("Validating audio...");
            // 1. Check against known good hashes (if any) first as a short circuit
            if (context.Config.KnownGoodHashes.Count > 0)
            {
                try
                {
                    var sha256 = HashCalculator.GetSha256(queryAudioFile);

                    foreach (var knownHash in context.Config.KnownGoodHashes)
                    {
                        if (knownHash.Type != null && Config.SHA_256_HASH.Equals(knownHash.Type.ToLower()))
                        {
                            if (knownHash.Hash.Equals(sha256))
                            {
                                // The file matches by hash so we know it is definitely valid
                                Logger.Debug("Match of SHA256 {sha256}", sha256);
                                return true;
                            }
                        }
                    }
                }
                catch (SystemException ex)
                {
                    Logger.Error(ex, "Failed to hash file");
                    return false;
                }
            }

            // 2. Read fingerprint hashes from file and check those using more advanced technique
            return await CheckFingerprint(queryAudioFile);
        }
    }
}
