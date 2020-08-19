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
using Xabe.FFmpeg;

using FFmpegApi = Xabe.FFmpeg.FFmpeg;

namespace SaberSongPatcher
{
    class InputValidator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        // Number of seconds to analyze from query file
        private static readonly int SECONDS_TO_ANALYZE = 10;
        // How confident we need to be to consider it a match
        private static readonly double CONFIDENCE_THRESHOLD = 0.75; 
        private static readonly double FUZZ_FACTOR_SEC = 0.1;
        private static readonly int ALLOWED_SONG_LENGTH_DIFFERENCE_MS = 3500;

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
                Logger.Error("Make sure {file} exists in the working directory {dir}",
                    Context.FINGERPRINT_FILE, Directory.GetCurrentDirectory());
                return false;
            }

            // Since we only have one song we are querying this info doesn't matter
            var track = new TrackInfo("123", "Test Song", "Test Artist");
            modelService.Insert(track, fingerprints);

            double startAtSecond = context.Config.Fingerprint?.StartAtSecond ?? 0;
            Logger.Debug("Analyzing {secsToAnalyze} seconds of audio starting at {startAtSecond} seconds",
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
                Logger.Debug("No fingerprint match found for this audio file");
                return false;
            }

            // https://github.com/AddictedCS/soundfingerprinting/wiki/Different-Types-of-Coverage
            var meetsConfidenceThreshold = match.Confidence >= CONFIDENCE_THRESHOLD;
            var meetsCoverageThreshold = match.CoverageLength >= match.QueryLength - 2;
            var meetsTrackStartOffsetThreshold = Math.Abs(match.TrackStartsAt) - startAtSecond <= FUZZ_FACTOR_SEC;

            Logger.Debug("Match found!");
            Logger.Debug("Confidence {val} >= {threshold} = {res}",
                match.Confidence, CONFIDENCE_THRESHOLD, meetsConfidenceThreshold);
            Logger.Debug("CoverageLength {val} >= {threshold} = {res}",
                match.CoverageLength, match.QueryLength - 2, meetsCoverageThreshold);
            Logger.Debug("TrackStartsAt abs({val}) - {start} <= {threshold} = {res}",
                match.TrackStartsAt, startAtSecond, FUZZ_FACTOR_SEC, meetsTrackStartOffsetThreshold);
            Logger.Debug("QueryMatchStartsAt={0}", match.QueryMatchStartsAt);
            Logger.Debug("TrackMatchStartsAt={0}", match.TrackMatchStartsAt);
            Logger.Debug("QueryLength={0}", match.QueryLength);
            Logger.Debug("QueryRelativeCoverage={0}", match.QueryRelativeCoverage);
            Logger.Debug("DiscreteCoverageLength={0}", match.DiscreteCoverageLength);
            Logger.Debug("CoverageWithPermittedGapsLength={0}", match.CoverageWithPermittedGapsLength);

            return meetsConfidenceThreshold && meetsCoverageThreshold && meetsTrackStartOffsetThreshold;
        }

        public async Task<bool> ValidateInput(string queryAudioFile)
        {
            Logger.Info("Validating audio file is correct master track...");
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

            // 2. Verify the length of the audio is not wildly different from the master
            if (context.Config.LengthMs > 0)
            {
                IMediaInfo info = await FFmpegApi.GetMediaInfo(queryAudioFile);
                var queryLengthMs = info.Duration.TotalMilliseconds;
                var lengthDifferenceMs = Math.Abs(queryLengthMs - context.Config.LengthMs);
                if (lengthDifferenceMs > ALLOWED_SONG_LENGTH_DIFFERENCE_MS)
                {
                    Logger.Debug("Song length {queryLength}ms is too different from expected length {masterLength}ms",
                        queryLengthMs,
                        context.Config.LengthMs);
                    return false;
                }
            }

            // 3. Read fingerprint hashes from file and check those using more advanced technique
            return await CheckFingerprint(queryAudioFile);
        }
    }
}
