using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using Autofac;
using J4JSoftware.Configuration.CommandLine;
using J4JSoftware.Configuration.J4JCommandLine;
using J4JSoftware.DependencyInjection;
using J4JSoftware.DependencyInjection.host;
using J4JSoftware.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace PlaySoundCore
{
    class Program
    {
        private static IJ4JHost? _host;

        static void Main()
        {
            var hostConfig = new J4JHostConfiguration()
                             .ApplicationName( "PlaySound" )
                             .Publisher( "J4JSoftware" )
                             .AutoDetectFileSystemCaseSensitivity()
                             .LoggerInitializer( SetupLogging )
                             .FilePathTrimmer( FilePathTrimmer )
                             .AddDependencyInjectionInitializers( SetupDependencyInjection );

            hostConfig.AddCommandLineProcessing( CommandLineOperatingSystems.Windows )
                      .OptionsInitializer( SetupOptions );

            if ( hostConfig.MissingRequirements != J4JHostRequirements.AllMet )
            {
                Console.WriteLine( $"Could not create IHost. The following requirements were not met: {hostConfig.MissingRequirements.ToText()}" );
                Environment.ExitCode = -1;

                return;
            }

            _host = hostConfig.Build();

            if ( _host == null )
            {
                Console.WriteLine( "Failed to build host" );
                Environment.ExitCode = -1;

                return;
            }

            var config = _host.Services.GetService<Configuration>();
            if ( config == null )
            {
                Console.WriteLine( "Configuration info not available" );
                Environment.ExitCode = -1;

                return;
            }

            if ( config.HelpRequested || !config.GetFileToPlay( out var fileToPlay ) )
            {
                var help = new ColorHelpDisplay( _host.CommandLineLexicalElements!, _host.Options! );
                help.Display();
            }
            else
            {
                var player = new SoundPlayer( fileToPlay );
                player.PlaySync();
            }
        }

        private static void SetupLogging( IConfiguration config, J4JLoggerConfiguration loggerConfig ) =>
            loggerConfig.SerilogConfiguration
                        .WriteTo.Debug()
                        .WriteTo.Console();

        private static void SetupOptions(OptionCollection options)
        {
            options!.Bind<Configuration, List<string>>(x => x.Extensions, "x", "extensions")!
                .SetDescription("Audio file extensions to choose");

            options.Bind<Configuration, List<string>>(x => x.SoundFiles, "f", "files")!
                .SetDescription("Sound files to choose");

            options.Bind<Configuration, string>(x => x.SoundDirectory, "d", "directory")!
                .SetDescription("Directory of sound files to choose");

            options.Bind<Configuration, bool>(x => x.HelpRequested, "h", "help")!
                .SetDescription("Display help");
        }

        private static void SetupDependencyInjection(HostBuilderContext hbc, ContainerBuilder builder)
        {
            builder.Register(c =>
                {
                    var retVal = hbc.Configuration.Get<Configuration>();
                    retVal.Logger = c.Resolve<IJ4JLogger>();

                    return retVal;
                })
                .AsSelf();
        }

        private static string FilePathTrimmer( Type? loggedType,
                                               string callerName,
                                               int lineNum,
                                               string srcFilePath )
        {
            return CallingContextEnricher.DefaultFilePathTrimmer( loggedType,
                                                                 callerName,
                                                                 lineNum,
                                                                 CallingContextEnricher.RemoveProjectPath( srcFilePath,
                                                                  GetProjectPath() ) );
        }

        private static string GetProjectPath( [ CallerFilePath ] string filePath = "" )
        {
            var dirInfo = new DirectoryInfo( Path.GetDirectoryName( filePath )! );

            while ( dirInfo.Parent != null )
            {
                if ( dirInfo.EnumerateFiles( "*.csproj" ).Any() )
                    break;

                dirInfo = dirInfo.Parent;
            }

            return dirInfo.FullName;
        }
    }
}
