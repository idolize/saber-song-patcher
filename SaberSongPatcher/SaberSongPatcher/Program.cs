using System;

using System.Threading.Tasks;
using System.Diagnostics;
using CommandLine;

namespace SaberSongPatcher
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // TODO parse arguments better (e.g. allow for operation names or flags)
            var expectedArgsNum = 3;
            if (args.Length < expectedArgsNum)
            {
                Console.WriteLine("Please enter " + expectedArgsNum + " arguments.");
                return 1;
            }
            // TODO store these on context?
            var inputFilename = args[0];
            var outputFilename = args[1];
            var masterFilename = args[3];
            
            var config = ConfigParser.ParseConfig();
            var context = new Context(config);

            try
            {
                var hashCalculator = new HashCalculator(context);
                var inputValidator = new InputValidator(context);
                var inputTransformer = new InputTransformer(context);

                context.Tracer.TraceInformation("Saving hashes...");
                hashCalculator.SaveHashesFromMaster(masterFilename);

                context.Tracer.TraceInformation("Validating audio...");
                var seemsCorrect = await inputValidator.ValidateInput(inputFilename);
                if (!seemsCorrect)
                {
                    context.Tracer.TraceInformation("Song does not seem to be correct.");
                    return 1;
                }

                context.Tracer.TraceInformation("Transforming audio...");
                await inputTransformer.TransformInput(inputFilename);

                context.Tracer.TraceInformation("Updating config...");
                ConfigParser.FlushConfigChanges(config);

                context.Tracer.TraceInformation("Done!");
                return 0;
            }
            catch (Exception ex)
            {
                context.Tracer.TraceEvent(TraceEventType.Critical, (int)Context.StatusCodes.ERROR_UNKNOWN, ex.Message);
                return 2;
            }
            finally
            {
                context.Tracer.Flush();
                context.Tracer.Close();
            }
        }
    }
}
