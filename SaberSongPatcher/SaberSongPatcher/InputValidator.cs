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

namespace SaberSongPatcher
{
    class InputValidator
    {
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
                Console.WriteLine($"Failed to access fingerprint file: {ex.Message}");
                return false;
            }

            // Since we only have one song this part doesn't matter
            var track = new TrackInfo("123", "Test Song", "Test Artist");
            modelService.Insert(track, fingerprints);

            int secondsToAnalyze = 10; // number of seconds to analyze from query file
            double startAtSecond = context.Config.Fingerprint?.StartAtSecond ?? 0;

            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                 .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                 .UsingServices(modelService, audioService)
                                                 .Query();

            var match = queryResult.BestMatch;
            if (match == null)
            {
                return false;
            }

            //Console.WriteLine("Match found! Confidence=" + match.Confidence);
            //Console.WriteLine("QueryRelativeCoverage=" + match.QueryRelativeCoverage);
            //Console.WriteLine("NoGaps=" + match.NoGaps);
            //Console.WriteLine("DiscreteCoverageLength=" + match.DiscreteCoverageLength);
            //Console.WriteLine("CoverageWithPermittedGapsLength=" + match.CoverageWithPermittedGapsLength);
            //Console.WriteLine("CoverageLength=" + match.CoverageLength);
            //Console.WriteLine("TrackStartsAt=" + match.TrackStartsAt);
            //Console.WriteLine("TrackMatchStartsAt=" + match.TrackMatchStartsAt);
            return match.Confidence > 0.9;
        }

        public async Task<bool> ValidateInput(string queryAudioFile)
        {
            // 1. Check against known good hashes (if any) first as a short circuit
            if (context.Config.KnownGoodHashes.Count > 0)
            {
                try
                {
                    var sha256 = HashCalculator.GetSha256(queryAudioFile);

                    foreach (var knownHash in context.Config.KnownGoodHashes)
                    {
                        if (knownHash.Type != null && "sha256".Equals(knownHash.Type.ToLower()))
                        {
                            if (knownHash.Hash.Equals(sha256))
                            {
                                // The file matches by hash so we know it is definitely valid
                                //Console.WriteLine("Match of SHA256 " + sha256);
                                return true;
                            }
                        }
                    }
                }
                catch (SystemException ex)
                {
                    Console.WriteLine($"Failed to access file: {ex.Message}");
                    return false;
                }
            }

            // 2. Read fingerprint hashes from file and check those using more advanced technique
            return await CheckFingerprint(queryAudioFile);
        }
    }
}
