using System;
using System.Threading.Tasks;
using System.Diagnostics;
using CommandLine;

namespace SaberSongPatcher
{
    class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        static async Task<int> Main(string[] args)
        {
            // TODO parse arguments better (e.g. allow for operation names or flags)
            var expectedArgsNum = 3;
            if (args.Length < expectedArgsNum)
            {
                Logger.Error("Please enter " + expectedArgsNum + " arguments.");
                return 1;
            }
            // TODO store these on context?
            var inputFilename = args[0];
            var outputFilename = args[1];
            var masterFilename = args[2];

            try
            {
                var config = ConfigParser.ParseConfig();
                var context = new Context(config);
                var hashCalculator = new HashCalculator(context);
                var inputValidator = new InputValidator(context);
                var inputTransformer = new InputTransformer(context);

                
                hashCalculator.SaveHashesFromMaster(masterFilename);

                Logger.Info("Validating audio...");
                var seemsCorrect = await inputValidator.ValidateInput(inputFilename);
                if (!seemsCorrect)
                {
                    Logger.Warn("Song does not match expectation for this map.");
                    return 1;
                }

                Logger.Info("Transforming audio...");
                await inputTransformer.TransformInput(inputFilename);

                ConfigParser.FlushConfigChanges(context.Config);

                Logger.Info("Done!");
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return 2;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }
    }
}
