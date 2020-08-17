using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Reflection;
using Xabe.FFmpeg;

using FFmpegApi = Xabe.FFmpeg.FFmpeg;
using System.Diagnostics;
using Xabe.FFmpeg.Exceptions;

namespace SaberSongPatcher
{
    class InputTransformer
    {
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
                context.Tracer.TraceInformation($"Deleting existing output file '{output}'");
                File.Delete(output);
            }

            // Set directory where the app should look for FFmpeg executables
            // based on https://github.com/AddictedCS/soundfingerprinting/wiki/Supported-Audio-Formats
            string ffmpegPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "FFmpeg\\bin\\x64");
            FFmpegApi.SetExecutablesPath(ffmpegPath);

            IMediaInfo info = await FFmpegApi.GetMediaInfo(input);
            IStream? audioStream = info.AudioStreams.FirstOrDefault()
                ?.SetCodec(AudioCodec.libvorbis);

            if (audioStream == null)
            {
                context.Tracer.TraceEvent(TraceEventType.Error, (int)Context.StatusCodes.ERROR_NO_AUDIO_STREAM, "No valid audio stream in file");
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
                context.Tracer.TraceEvent(TraceEventType.Error, (int)Context.StatusCodes.ERROR_FFMPEG_FAILED, ex.Message);
                return false;
            }
            return true;
        }

        public async Task<bool> TransformInput(string input)
        {
            // TODO 1. Apply patches from config

            // 2. Convert to OGG
            var extension = Path.GetExtension(input);
            var fileName = Path.GetFileNameWithoutExtension(input);

            if (".ogg".Equals(extension))
            {
                // TODO if there were any patches applied we will still need to do this
                return true;
            }

            return await ConvertToOgg(input, $"{fileName}.ogg");
        }
    }
}
