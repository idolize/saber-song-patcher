using NAudio.Wave;
using System;
using System.IO;

using System.Threading.Tasks;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Builder;
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

            var hashCalculator = new HashCalculator(context);
            var inputValidator = new InputValidator(context);
            var inputTransformer = new InputTransformer(context);

            Console.WriteLine("Saving hashes...");
            hashCalculator.SaveHashesFromMaster(masterFilename);

            Console.WriteLine("Validating audio...");
            var seemsCorrect = await inputValidator.ValidateInput(inputFilename);
            if (!seemsCorrect)
            {
                Console.WriteLine("Song does not seem to be correct.");
                return 1;
            }

            Console.WriteLine("Transforming audio...");
            await inputTransformer.TransformInput(inputFilename);

            Console.WriteLine("Updating config...");
            ConfigParser.FlushConfigChanges(config);

            Console.WriteLine("Done!");
            return 0;
        }
    }
}
