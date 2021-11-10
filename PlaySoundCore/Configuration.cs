using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using J4JSoftware.Logging;
using Serilog;

namespace PlaySoundCore
{
    public class Configuration
    {
        private IJ4JLogger? _logger;

        public IJ4JLogger? Logger
        {
            get => _logger;

            set
            {
                _logger = value;
                _logger?.SetLoggedType(GetType());
            }
        }

        public bool CaseSensitiveFileSystem { get; set; }
        public bool HelpRequested { get; set; }

        public List<string> Extensions { get; set; } = new() { ".wav", ".mp3" };
        public List<string> SoundFiles { get; set; } = new();
        public string SoundDirectory { get; set; } = Environment.CurrentDirectory;

        public bool GetFileToPlay(out string? result)
        {
            result = null;

            var choices = new List<string>();

            foreach (var individual in SoundFiles)
            {
                var filePath = Path.IsPathRooted(individual)
                    ? individual
                    : Path.Combine(Environment.CurrentDirectory, individual);

                if (File.Exists(filePath))
                    choices.Add(individual);
                else Logger?.Error<string>("Sound file '{0}' not found", filePath);
            }

            var directory = Path.IsPathRooted(SoundDirectory)
                ? SoundDirectory
                : Path.Combine(Environment.CurrentDirectory, SoundDirectory);

            if (!Directory.Exists(directory))
                Logger?.Error<string>("Sound directory '{0}' not found", directory);

            foreach (var individual in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                         .Where(x => Extensions.Any(y => y.Equals(
                             Path.GetExtension(x),
                             CaseSensitiveFileSystem
                                 ? StringComparison.Ordinal
                                 : StringComparison.OrdinalIgnoreCase)))
                    )
            {
                if (File.Exists(individual))
                    choices.Add(individual);
                else Logger?.Error<string>("Sound file '{0}' not found", individual);
            }

            if (!choices.Any())
                Logger?.Error("No sound files found");
            else
            {
                var random = new Random(Guid.NewGuid().GetHashCode());
                result = choices[random.Next(choices.Count)];
            }

            return result != null;
        }
    }
}