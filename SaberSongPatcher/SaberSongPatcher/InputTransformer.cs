using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Exceptions;
using System.Text;
using System.Collections.Generic;

using FFmpegApi = Xabe.FFmpeg.FFmpeg;

namespace SaberSongPatcher
{
    class InputTransformer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly string OUTPUT_EXTENSION = ".ogg"; // TODO use .egg?

        private readonly Context context;

        public InputTransformer(Context context)
        {
            this.context = context;
        }

        public async Task<bool> TransformAudio(string input, string output, string? parameters)
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
                IConversion conversion = FFmpegApi.Conversions.New()
                    .AddStream(audioStream)
                    .SetOutput(output);
                if (parameters != null)
                {
                    conversion = conversion.AddParameter(parameters);
                }
                await conversion.Start();
            } catch (ConversionException ex)
            {
                Logger.Error(ex, "Failed to transform audio");
                Logger.Debug(ex);
                return false;
            }
            Logger.Info("{file} created in directory {directory}",
                    output, Path.GetDirectoryName(Path.GetFullPath(output)));
            return true;
        }

        public static string CreateFilterStringFromParams(string filterName, IEnumerable<string> parameters)
        {
            var parametersString = string.Join(":", parameters);
            return $"{filterName}={parametersString}";
        }

        public string? ConstructFiltersStringFromPatches()
        {
            var patches = context.Config.Patches;
            if (patches.HasPatches())
            {
                Logger.Debug("Applying patches");
                
                var filters = new List<string>();
                if (patches.Trim != null && (patches.Trim.StartMs != null || patches.Trim.EndMs != null))
                {
                    // https://ffmpeg.org/ffmpeg-filters.html#atrim
                    var patch = patches.Trim;
                    var parameters = new List<string>();
                    if (patch.StartMs != null)
                    {
                        parameters.Add($"start={patch.StartMs}ms");
                    }
                    if (patch.EndMs != null)
                    {
                        parameters.Add($"end={patch.EndMs}ms");
                    }
                    filters.Add(CreateFilterStringFromParams("atrim", parameters));
                }
                if (patches.FadeIn != null)
                {
                    // https://ffmpeg.org/ffmpeg-filters.html#afade
                    var patch = patches.FadeIn;
                    filters.Add(CreateFilterStringFromParams("afade", new[] {
                        "t=in",  $"st={patch.StartMs}ms", $"d={patch.DurationMs}ms"
                    }));
                }
                if (patches.FadeOut != null)
                {
                    // https://ffmpeg.org/ffmpeg-filters.html#afade
                    var patch = patches.FadeOut;
                    filters.Add(CreateFilterStringFromParams("afade", new[] {
                        "t=out", $"st={patch.StartMs}ms", $"d={patch.DurationMs}ms"
                    }));
                }
                if (patches.DelayStartMs != null)
                {
                    // https://ffmpeg.org/ffmpeg-filters.html#adelay
                    filters.Add(CreateFilterStringFromParams("adelay", new[] {
                        $"delays={patches.DelayStartMs}|{patches.DelayStartMs}"
                    }));
                }
                if (patches.PadEndMs != null)
                {
                    // https://ffmpeg.org/ffmpeg-filters.html#apad
                    filters.Add(CreateFilterStringFromParams("apad", new[] {
                        $"pad_dur={patches.PadEndMs}ms"
                    }));
                }

                // e.g. `-af "afade=t=out:st=5:d=5,afade=t=in:st=5:d=5"`
                StringBuilder sb = new StringBuilder("-af \"");
                var filtersString = string.Join(",", filters);
                sb.Append(filtersString);
                sb.Append("\"");
                var output = sb.ToString();
                Logger.Debug("patches: {str}", output);
                return output;
            }
            Logger.Debug("No patches to apply");
            return null;
        }

        public async Task<bool> TransformInput(string input, string? output)
        {
            Logger.Info("Transforming master audio file to match output...");

            // Apply patches from config and convert to OGG
            string? parameters = ConstructFiltersStringFromPatches();

            var extension = Path.GetExtension(input);
            var fileName = Path.GetFileNameWithoutExtension(input);

            if (parameters == null && OUTPUT_EXTENSION.Equals(extension))
            {
                Logger.Debug("Audio input already in {ext} format", OUTPUT_EXTENSION);
                return true;
            }

            return await TransformAudio(input, output ?? $"{fileName}{OUTPUT_EXTENSION}", parameters);
        }
    }
}
