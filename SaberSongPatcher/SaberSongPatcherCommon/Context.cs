﻿using System;
using System.IO;
using System.Reflection;

using FFmpegApi = Xabe.FFmpeg.FFmpeg;

namespace SaberSongPatcher
{
    public class Context
    {
        public Config Config { get; set; }

        public string OrigWorkingDirectory { get; }

        public string ExeDirectory { get; }

        public string FFmpegRootPath { get; }

        public Context()
        {
            Config = new Config();
            OrigWorkingDirectory = Directory.GetCurrentDirectory();
            ExeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            FFmpegRootPath = Path.Combine(ExeDirectory, "FFmpeg\\bin", Environment.Is64BitProcess ? "x64" : "x86");
            // Set directory where the app should look for FFmpeg executables
            // based on https://github.com/AddictedCS/soundfingerprinting/wiki/Supported-Audio-Formats
            FFmpegApi.SetExecutablesPath(FFmpegRootPath);
        }

        public Context(Config config) : this()
        {
            Config = config;
        }
    }
}
