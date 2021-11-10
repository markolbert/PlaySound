using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading;
using Alba.CsConsoleFormat;
using Autofac;
using J4JSoftware.Configuration.CommandLine;
using J4JSoftware.Configuration.J4JCommandLine;
using J4JSoftware.DependencyInjection;
using J4JSoftware.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;

namespace PlaySoundCore
{
    class Program
    {
        internal static IHost? Host { get; private set; }
        
        private static IJ4JLogger? _buildLogger;

        static void Main(string[] args)
        {
            var hostConfig = new J4JHostConfiguration()
                .ApplicationName("PlaySound")
                .Publisher("J4JSoftware")
                .AutoDetectFileSystemCaseSensitivity()
                .LoggerInitializer(SetupLogging)
                .FilePathTrimmer(FilePathTrimmer)
                .AddDependencyInjectionInitializers(SetupDependencyInjection);

            hostConfig.AddCommandLineProcessing(CommandLineOperatingSystems.Windows)
                .OptionsInitializer(SetupOptions);

            _buildLogger = hostConfig.Logger;

            if (hostConfig.MissingRequirements != J4JHostRequirements.AllMet)
            {
                Console.WriteLine(
                    $"Could not create IHost. The following requirements were not met: {hostConfig.MissingRequirements.ToText()}");
                Environment.ExitCode = -1;

                return;
            }

            var builder = hostConfig.CreateHostBuilder();

            if (builder == null)
            {
                Console.WriteLine("Failed to create host builder.");
                Environment.ExitCode = -1;

                return;
            }

            Host = builder.Build();

            if (Host == null)
            {
                Console.WriteLine("Failed to build host");
                Environment.ExitCode = -1;

                return;
            }

            var options = Host.Services.GetService<OptionCollection>();
            if (options == null)
            {
                Console.WriteLine("Option collection not available");
                Environment.ExitCode = -1;

                return;
            }

            var hostInfo = Host.Services.GetService<J4JHostInfo>();
            if (hostInfo == null || hostInfo.CommandLineLexicalElements == null)
            {
                Console.WriteLine("J4JHostInfo or CommandLineLexicalElements not available");
                Environment.ExitCode = -1;

                return;
            }

            var config = Host.Services.GetService<Configuration>();
            if (config == null)
            {
                Console.WriteLine("Configuration info not available");
                Environment.ExitCode = -1;

                return;
            }

            if (config.HelpRequested || !config.GetFileToPlay(out var fileToPlay))
            {
                var help = new ColorHelpDisplay(hostInfo.CommandLineLexicalElements!, options);
                help.Display();
            }
            else
            {
                var player = new SoundPlayer(fileToPlay);
                player.PlaySync();
            }
        }

        private static void SetupLogging(IConfiguration config, J4JLoggerConfiguration loggerConfig)
            => loggerConfig.SerilogConfiguration
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

                    var info = c.Resolve<J4JHostInfo>();
                    retVal.CaseSensitiveFileSystem = info.CaseSensitiveFileSystem;

                    return retVal;
                })
                .AsSelf();
        }

        private static string FilePathTrimmer(
            Type? loggedType,
            string callerName,
            int lineNum,
            string srcFilePath)
        {
            return CallingContextEnricher.DefaultFilePathTrimmer(loggedType,
                callerName,
                lineNum,
                CallingContextEnricher.RemoveProjectPath(srcFilePath, GetProjectPath()));
        }

        private static string GetProjectPath([CallerFilePath] string filePath = "")
        {
            var dirInfo = new DirectoryInfo(Path.GetDirectoryName(filePath)!);

            while (dirInfo.Parent != null)
            {
                if (dirInfo.EnumerateFiles("*.csproj").Any())
                    break;

                dirInfo = dirInfo.Parent;
            }

            return dirInfo.FullName;
        }
    }
}
