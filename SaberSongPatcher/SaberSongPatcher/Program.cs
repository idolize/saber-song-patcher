using NAudio.Wave;
using System;
using System.IO;
using Xabe.FFmpeg;
using System.Linq;
using System.Threading.Tasks;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using System.Reflection;

using FFmpegApi = Xabe.FFmpeg.FFmpeg;

namespace SaberSongPatcher
{
    class Program
    {
        public static void ConvertAiffToWav(string aiffFile, string wavFile)
        {
            using (AiffFileReader reader = new AiffFileReader(aiffFile))
            {
                using (WaveFileWriter writer = new WaveFileWriter(wavFile, reader.WaveFormat))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = 0;
                    do
                    {
                        bytesRead = reader.Read(buffer, 0, buffer.Length);
                        writer.Write(buffer, 0, bytesRead);
                    } while (bytesRead > 0);
                }
            }
        }

        public static void ConvertToWav(string input, string output)
        {
            using (var reader = new MediaFoundationReader(input))
            {
                WaveFileWriter.CreateWaveFile(output, reader);
            }
        }

        public static async Task ConvertToOgg(string input, string output)
        {
            if (File.Exists(output))
            {
                return;
            }

            IMediaInfo info = await FFmpegApi.GetMediaInfo(input);
            IStream audioStream = info.AudioStreams.FirstOrDefault()
                ?.SetCodec(AudioCodec.libvorbis);

            await FFmpegApi.Conversions.New()
                .AddStream(audioStream)
                .SetOutput(output)
                .Start();
        }

        static async Task<bool> CheckFingerprint(string queryAudioFile, string sourceAudioFile)
        {
            IModelService modelService = new InMemoryModelService(); // store fingerprints in RAM
            IAudioService audioService = new FFmpegAudioService();

            // create fingerprints
            var hashedFingerprints = await FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From(sourceAudioFile)
                                        .UsingServices(audioService)
                                        .Hash();

            var track = new TrackInfo("123", "Test Song", "Test Artist");
            // store hashes in the database for later retrieval
            modelService.Insert(track, hashedFingerprints);


            // QUERY

            int secondsToAnalyze = 10; // number of seconds to analyze from query file
            int startAtSecond = 0; // start at the begining

            // query the underlying database for similar audio sub-fingerprints
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                 .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                 .UsingServices(modelService, audioService)
                                                 .Query();

            var match = queryResult.BestMatch;
            if (match == null)
            {
                Console.WriteLine("No match");
                return false;
            }
            Console.WriteLine("Match found! Confidence=" + match.Confidence);
            Console.WriteLine("QueryRelativeCoverage=" + match.QueryRelativeCoverage);
            Console.WriteLine("NoGaps=" + match.NoGaps);
            Console.WriteLine("DiscreteCoverageLength=" + match.DiscreteCoverageLength);
            Console.WriteLine("CoverageWithPermittedGapsLength=" + match.CoverageWithPermittedGapsLength);
            Console.WriteLine("CoverageLength=" + match.CoverageLength);
            Console.WriteLine("TrackStartsAt=" + match.TrackStartsAt);
            Console.WriteLine("TrackMatchStartsAt=" + match.TrackMatchStartsAt);
            return match.Confidence > 0.9;
        }

        static async Task<int> Main(string[] args)
        {
            var expectedArgsNum = 3;
            if (args.Length < expectedArgsNum)
            {
                Console.WriteLine("Please enter " + expectedArgsNum + " arguments.");
                return 1;
            }

            // Set directory where the app should look for FFmpeg executables.
            string ffmpegPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FFmpeg\\bin\\x64");
            FFmpegApi.SetExecutablesPath(ffmpegPath);

            var seemsCorrect = await CheckFingerprint(args[0], args[2]);
            if (!seemsCorrect)
            {
                Console.WriteLine("Song does not seem to be correct.");
                return 1;
            }
            
            // await ConvertToOgg(args[0], args[1]);
            Console.WriteLine("Done!");
            return 0;
        }
    }
}
