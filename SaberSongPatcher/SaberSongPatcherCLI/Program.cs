using System;
using System.Threading.Tasks;
using CommandLine;
using System.Collections.Generic;
using NLog;
using SaberSongPatcher;

namespace SaberSongPatcher.CLI
{
    class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        class Options
        {
            [Option('s', "silent", Required = false, HelpText = "Disable all console output.")]
            public bool Silent { get; set; } = false;

            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; } = false;

            [Option('c', "config", Required = false, HelpText = "Folder where audio.json config file exists.")]
            public string? ConfigDirectory { get; set; }
        }

        [Verb("patch", HelpText = "Verify, patch, and convert the input audio file for use.")]
        class PatchOptions : Options
        {
            [Option('i', "input", Required = true, HelpText = "Input song file (supports most codecs).")]
            public string InputFile { get; set; } = string.Empty;

            [Option('o', "output", Required = false, HelpText = "Name of output file (defaults to {input}.ogg).")]
            public string? OutputFile { get; set; }
        }

        [Verb("fingerprint", HelpText = "Store fingerprint and hash data for the master audio file.")]
        class FingerprintOptions : Options
        {
            [Option('m', "master", Required = true, HelpText = "Master audio file for the song.")]
            public string MasterFile { get; set; } = string.Empty;
        }

        static private void ConfigureLoggers(Options opts)
        {
            if (opts.Verbose)
            {
                // Enable Debug output on console
                LogManager.Configuration.Variables["customLevel"] = "Debug";
                LogManager.ReconfigExistingLoggers();
            } else if (opts.Silent)
            {
                // Disable all output on console
                LogManager.Configuration.Variables["customLevel"] = "Off";
                LogManager.ReconfigExistingLoggers();
            }
        }

        static async Task<int> RunFingerprintAndReturnExitCode(FingerprintOptions opts)
        {
            ConfigureLoggers(opts);
            var config = ConfigParser.ParseConfig(false, opts.ConfigDirectory);
            var context = new Context(config);
            var hashCalculator = new HashCalculator(context);

            var success = await hashCalculator.SaveHashesFromMaster(opts.MasterFile);
            if (!success)
            {
                return 1;
            }

            ConfigParser.FlushConfigChanges(context.Config, opts.ConfigDirectory);

            Logger.Info("Success! Distribute your {0} and {1} files along with your map.",
                HashCalculator.FINGERPRINT_FILE, Config.CONFIG_FILE);
            return 0;
        }

        static async Task<int> RunPatchAndReturnExitCode(PatchOptions opts)
        {
            ConfigureLoggers(opts);
            var config = ConfigParser.ParseConfig(true, opts.ConfigDirectory);
            var context = new Context(config);
            var inputValidator = new InputValidator(context);
            var inputTransformer = new InputTransformer(context);

            var seemsCorrect = await inputValidator.ValidateInput(opts.InputFile);
            if (!seemsCorrect)
            {
                Logger.Error("Input audio file does not match master audio file for this map.");
                return 1;
            }

            var success = await inputTransformer.TransformInput(opts.InputFile, opts.OutputFile);
            if (!success)
            {
                return 1;
            }

            ConfigParser.FlushConfigChanges(context.Config, opts.ConfigDirectory);

            Logger.Info("Success! Audio file now ready to use in Beat Saber.");
            return 0;
        }

        static Task<int> HandleArgParseError(IEnumerable<Error> errs)
        {
            // Error parsing command line args (information is already printed to the user)
            return Task.FromResult(3);
        }

        static async Task<int> Main(string[] args)
        {
            try
            {
                Logger.Debug("Starting...");
                // Parse command line args and run the methods
                // https://github.com/commandlineparser/commandline
                return await Parser.Default.ParseArguments<FingerprintOptions, PatchOptions>(args)
                    .MapResult(
                      (FingerprintOptions opts) => RunFingerprintAndReturnExitCode(opts),
                      (PatchOptions opts) => RunPatchAndReturnExitCode(opts),
                      HandleArgParseError);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return 2;
            }
            finally
            {
                LogManager.Shutdown();
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROMPT")))
                {
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                }
            }
        }
    }
}
