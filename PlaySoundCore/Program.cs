using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Media;
using System.Reflection.Metadata.Ecma335;

namespace PlaySoundCore
{
    class Program
    {
        static void Main( string[] args )
        {
            var command = new RootCommand()
            {
                new Option<FileInfo>(
                    new[] { "-f", "--file" },
                    description : "Sound file to play" )
                {
                    Name = "SoundFile"
                },

                new Option<DirectoryInfo>(
                    new[] { "-d", "--directory" },
                    getDefaultValue:()=>new DirectoryInfo("c:/Sounds"),
                    description : "Directory of sound files to choose" )
                {
                    Name = "SoundDirectory"
                },

                new Option<string[]>(
                    new[] { "-x", "--extensions" },
                    getDefaultValue: ()=>new []{".wav", ".mp3"},
                    description : "Valid audio file extensions" )
                {
                    Name = "Extensions"
                }
            };

            Configuration config = new Configuration();

            command.Handler = CommandHandler.Create( (Configuration c) =>
            {
                config.Extensions = c.Extensions;
                config.SoundFile = c.SoundFile;
                config.SoundDirectory = c.SoundDirectory;
            });

            var result = command.Invoke( args );

            if( !config.GetSoundFile(out var fileName))
            {
                Environment.ExitCode = 1;
                return;
            }

            var player = new SoundPlayer( fileName );
            player.PlaySync();
        }
    }
}
