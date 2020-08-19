using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Exceptions;

using FFmpegApi = Xabe.FFmpeg.FFmpeg;

namespace SaberSongPatcher
{
    class InputTransformer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly Context context;

        public InputTransformer(Context context)
        {
            this.context = context;
        }

        public async Task<bool> ConvertToOgg(string input, string output)
        {
            if (File.Exists(output))
            {
                // Overwrite the file
                Logger.Debug("Deleting existing output file {output}", output);
                File.Delete(output);
            }

            IMediaInfo info = await FFmpegApi.GetMediaInfo(input);
            IStream? audioStream = info.AudioStreams.FirstOrDefault()
                ?.SetCodec(AudioCodec.libvorbis);

            if (audioStream == null)
            {
                Logger.Error("No valid audio stream in file");
                return false;
            }

            try
            {
                await FFmpegApi.Conversions.New()
                    .AddStream(audioStream)
                    .SetOutput(output)
                    .Start();
            } catch (ConversionException ex)
            {
                Logger.Error(ex, "Failed to convert to OGG");
                return false;
            }
            Logger.Info("{file} created in directory {directory}",
                    output, Path.GetDirectoryName(Path.GetFullPath(output)));
            return true;
        }

        public async Task<bool> TransformInput(string input, string? output)
        {
            Logger.Info("Transforming master audio file to match output...");

            // TODO 1. Apply patches from config

            // 2. Convert to OGG
            var extension = Path.GetExtension(input);
            var fileName = Path.GetFileNameWithoutExtension(input);

            if (".ogg".Equals(extension))
            {
                // TODO if there were any patches applied we will still need to do this
                Logger.Debug("Audio input already in OGG format");
                return true;
            }

            return await ConvertToOgg(input, output ?? $"{fileName}.ogg");
        }
    }
}
