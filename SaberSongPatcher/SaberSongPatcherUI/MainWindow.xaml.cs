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
using System.Windows.Documents;
using System.Windows.Media;

namespace SaberSongPatcher.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private bool isDebugMode = false;

        public MainWindow(): base()
        {
            InitializeComponent();
            Logger.Info("Select a song to use with this map");
            RenderAllLogs();

            ObservableLogTarget.CollectionChanged += (sender, e) =>
            {
                var added = e.NewItems != null ?
                    ObservableLogTarget.FilterLogs(
                        e.NewItems.Cast<ObservableLogTarget.LogEntry>().ToList(),
                        isDebugMode ? 1 : 2
                    ) : null;
                if (added != null && added.Count > 0)
                {
                    logEntriesDoc.Blocks.AddRange(added.Select(FormatLogEntry));
                    richTextBox.ScrollToEnd();
                }
            };
        }

        private Paragraph FormatLogEntry(ObservableLogTarget.LogEntry log)
        {
            Paragraph myParagraph = new Paragraph();
            if (isDebugMode)
            {
                myParagraph.Inlines.Add(new Run(log.LevelName + '\t')
                {
                    Foreground = Brushes.LightGray,
                });
            }
            
            var run = new Run(log.Message);
            Inline inline = run;
            var isSuccess = log.LevelOrdinal == 2 && log.Message.ToLower().StartsWith("success");
            if (log.LevelOrdinal == 1)
            {
                run.Foreground = Brushes.Gray;
            }
            if (isSuccess)
            {
                run.Foreground = Brushes.Green;
                inline = new Bold(run);
            }
            if (log.LevelOrdinal == 3)
            {
                run.Foreground = Brushes.Orange;
            }
            if (log.LevelOrdinal >= 4)
            {
                run.Foreground = Brushes.DarkRed;
            }
            myParagraph.Inlines.Add(inline);
            return myParagraph;
        }

        private void RenderAllLogs()
        {
            logEntriesDoc.Blocks.Clear();
            logEntriesDoc.Blocks.AddRange(ObservableLogTarget.GetLogs(isDebugMode ? 1 : 2).Select(FormatLogEntry));
        }

        private void CkDebug_CheckedChanged(object sender, RoutedEventArgs e)
        {
            isDebugMode = CkDebug.IsChecked == true;
            RenderAllLogs();
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
                BtnOpenFile.IsEnabled = false;
                Dispatcher.InvokeAsync(async () => {
                    var success = await PerformPatch(openFileDialog.FileName);
                    BtnOpenFile.IsEnabled = true;
                });
            }
        }

        private async Task<bool> PerformPatch(string inputFile)
        {
            try
            {
                var possibleConfigFolder = Path.GetDirectoryName(inputFile);
                Config config;
                try
                {
                    config = ConfigParser.ParseConfig(true, possibleConfigFolder);
                }
                catch (FileNotFoundException ex)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Title = "Select config file for song",
                        Filter = $"Audio config files (*.json)|*.json",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
                    };
                    if (openFileDialog.ShowDialog() == true)
                    {
                        config = ConfigParser.ParseConfig(true, openFileDialog.FileName);
                        possibleConfigFolder = Path.GetDirectoryName(openFileDialog.FileName);
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
                    seemsCorrect = await inputValidator.ValidateInput(inputFile, possibleConfigFolder);
                }
                catch (FileNotFoundException ex)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Title = "Select fingerprint file for song",
                        Filter = $"Fingerprint files (*.bin)|*.bin"
                    };
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
