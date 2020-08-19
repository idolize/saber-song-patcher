using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SaberSongPatcher.UI
{
    public sealed class ObservableLogTarget
    {
        public class LogEntry
        {
            public readonly string LevelName;
            public readonly string Message;
            public readonly int LevelOrdinal;

            public LogEntry(string level, string message, int ordinal)
            {
                // https://github.com/NLog/NLog/wiki/Level-Layout-Renderer#parameters
                LevelName = level;
                Message = message;
                LevelOrdinal = ordinal;
            }

            public override string ToString()
            {
                return $"{LevelName}\t{Message}";
            }
        }

        private static readonly ObservableCollection<LogEntry> list
            = new ObservableCollection<LogEntry>();

        public static event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                list.CollectionChanged += value;
            }

            remove
            {
                list.CollectionChanged -= value;
            }
        }

        public static void AddLog(string level, string message, string ordinal)
        {
            System.Diagnostics.Debug.WriteLine($"[{ordinal}] {message}");
            list.Add(new LogEntry(level, message, int.Parse(ordinal)));
        }

        public static IReadOnlyList<LogEntry> GetLogs()
        {
            return list;
        }

        public static IReadOnlyList<LogEntry> GetLogs(int level)
        {
            return list.Where(log => log.LevelOrdinal >= level).ToList();
        }

        public static void Clear()
        {
            list.Clear();
        }
    }
}
