//using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using NLog;
using System.IO;

namespace SaberSongPatcher.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly int logLevel = 2;

        public MainWindow(): base()
        {
            InitializeComponent();
            txtEditor.Text = string.Join("\n", ObservableLogTarget.GetLogs(logLevel));
            ObservableLogTarget.CollectionChanged += (sender, e) =>
            {
                txtEditor.Text = string.Join("\n", ObservableLogTarget.GetLogs(logLevel));
            };
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            
            OpenFileDialog openFileDialog = new OpenFileDialog();
            var supportedExtensions = new[]
            {
                "mp3",
                "m4a",
                "ogg",
                "wav",
                "flac",
                "aiff",
                "wma",
            };
            var audioExtensions = string.Join(";", supportedExtensions.Select(ext => $"*.{ext}"));
            openFileDialog.Title = "Select master song audio file";
            openFileDialog.Filter = $"Audio files ({audioExtensions})|{audioExtensions}|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            if (openFileDialog.ShowDialog() == true)
            {
                // Begin patching
                Dispatcher.InvokeAsync(() => PerformPatch(openFileDialog.FileName));
            }
        }

        private async Task<bool> PerformPatch(string inputFile)
        {
            try
            {
                Config config = null;
                try
                {
                    config = ConfigParser.ParseConfig(true);
                }
                catch (FileNotFoundException ex)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Title = "Select config file for song";
                    openFileDialog.Filter = $"Audio config files (*.json)|*.json";
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    if (openFileDialog.ShowDialog() == true)
                    {
                        config = ConfigParser.ParseConfig(openFileDialog.FileName, true);
                    }
                    else
                    {
                        throw ex;
                    }
                }
                
                var context = new Context(config);
                var inputValidator = new InputValidator(context);
                var inputTransformer = new InputTransformer(context);

                bool? seemsCorrect;
                try
                {
                    seemsCorrect = await inputValidator.ValidateInput(inputFile);
                }
                catch (FileNotFoundException ex)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Title = "Select fingerprint file for song";
                    openFileDialog.Filter = $"Fingerprint files (*.bin)|*.bin";
                    if (openFileDialog.ShowDialog() == true)
                    {
                        seemsCorrect = await inputValidator.ValidateInput(inputFile, openFileDialog.FileName);
                    }
                    else
                    {
                        throw ex;
                    }
                }
                if (!(bool)seemsCorrect)
                {
                    Logger.Error("Input audio file does not match master audio file for this map.");
                    return false;
                }

                var success = await inputTransformer.TransformInput(inputFile);
                if (!success)
                {
                    return false;
                }

                ConfigParser.FlushConfigChanges(context.Config);

                Logger.Info("Success! Audio file now ready to use in Beat Saber.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                LogManager.Flush();
                return false;
            }
        }
    }
}
