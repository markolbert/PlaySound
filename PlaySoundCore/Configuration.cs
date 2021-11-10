using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlaySoundCore
{
    public class Configuration
    {
        private string[] _audioExt;
        private DirectoryInfo _dirInfo;

        public string[] Extensions
        {
            get
            {
                if( _audioExt == null || _audioExt.Length == 0 )
                    _audioExt = new[] { ".wav", ".mp3" };

                return _audioExt;
            }

            set => _audioExt = value;
        }

        public FileInfo SoundFile { get; set; }

        public DirectoryInfo SoundDirectory
        {
            get
            {
                if( _dirInfo == null )
                {
                    if( SoundFile != null )
                        _dirInfo = SoundFile.Directory;
                }

                return _dirInfo;
            }

            set => _dirInfo = value;
        }

        public bool GetSoundFile( out string fileName )
        {
            fileName = null;

            if( SoundFile != null && File.Exists( SoundFile.FullName ) )
            {
                fileName = SoundFile.FullName;
                return true;
            }

            if( SoundDirectory == null || !Directory.Exists(SoundDirectory.FullName) )
                return false;

            var files = Directory.GetFiles(
                    SoundDirectory.FullName,
                    "*.*",
                    searchOption : SearchOption.AllDirectories )
                .Where( f =>
                    Extensions.Any( x => x.Equals( Path.GetExtension( f ), StringComparison.OrdinalIgnoreCase ) ) )
                .ToList();

            if( files.Count == 0 )
                return false;

            var random = new Random();

            fileName = files[ random.Next( files.Count ) ];

            return true;
        }
    }
}