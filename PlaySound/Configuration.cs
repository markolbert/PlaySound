using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using J4JSoftware.Logging;

namespace PlaySoundCore
{
    public class Configuration
    {
        public bool CaseSensitiveFileSystem { get; set; }
        public bool HelpRequested { get; set; }

        public List<string> Extensions { get; set; } = new() { ".wav", ".mp3" };
        public List<string> SoundFiles { get; set; } = new();
        public string SoundDirectory { get; set; } = Environment.CurrentDirectory;

        public bool GetFileToPlay( out string? result )
        {
            result = null;

            var choices = new List<string>();

            foreach ( var individual in SoundFiles )
            {
                var filePath = Path.IsPathRooted( individual )
                                   ? individual
                                   : Path.Combine( Environment.CurrentDirectory, individual );

                if ( File.Exists( filePath ) )
                    choices.Add( individual );
            }

            var directory = Path.IsPathRooted( SoundDirectory )
                                ? SoundDirectory
                                : Path.Combine( Environment.CurrentDirectory, SoundDirectory );

            foreach ( var individual in Directory.GetFiles( directory, "*.*", SearchOption.AllDirectories )
                                                 .Where( x => Extensions.Any( y => y.Equals( Path.GetExtension( x ),
                                                                               CaseSensitiveFileSystem
                                                                                   ? StringComparison.Ordinal
                                                                                   : StringComparison
                                                                                       .OrdinalIgnoreCase ) ) )
                    )
            {
                if ( File.Exists( individual ) )
                    choices.Add( individual );
            }

            if ( choices.Any() )
            {
                var random = new Random( Guid.NewGuid().GetHashCode() );
                result = choices[ random.Next( choices.Count ) ];
            }

            return result != null;
        }
    }
}
