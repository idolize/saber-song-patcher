using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Reflection;
using Xabe.FFmpeg;

using FFmpegApi = Xabe.FFmpeg.FFmpeg;


namespace SaberSongPatcher
{
    class InputTransformer
    {
        private readonly Context context;

        public InputTransformer(Context context)
        {
            this.context = context;
        }

        public static async Task ConvertToOgg(string input, string output)
        {
            if (File.Exists(output))
            {
                // Do we want to overwrite the file?
                return;
            }

            // Set directory where the app should look for FFmpeg executables
            // based on https://github.com/AddictedCS/soundfingerprinting/wiki/Supported-Audio-Formats
            string ffmpegPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FFmpeg\\bin\\x64");
            FFmpegApi.SetExecutablesPath(ffmpegPath);

            IMediaInfo info = await FFmpegApi.GetMediaInfo(input);
            IStream audioStream = info.AudioStreams.FirstOrDefault()
                ?.SetCodec(AudioCodec.libvorbis);

            await FFmpegApi.Conversions.New()
                .AddStream(audioStream)
                .SetOutput(output)
                .Start();
        }

        public async Task TransformInput(string input)
        {
            // TODO 1. Apply patches from config

            // 2. Convert to OGG
            var extension = Path.GetExtension(input);
            var fileName = Path.GetFileNameWithoutExtension(input);

            if (".ogg".Equals(extension))
            {
                // TODO if there were any patches applied we will still need to do this
                return;
            }

            await ConvertToOgg(input, $"{fileName}.ogg");
        }
    }
}
