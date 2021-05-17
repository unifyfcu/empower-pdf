using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;
using System;
using System.IO;
using System.Linq;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace empower_pdf
{
    public class Arguments : IArguments
    {
        public static LoggingLevelSwitch LevelSwitch = new LoggingLevelSwitch();

        public Arguments() { }

        public Arguments(Arguments options)
        {
            SourcePath = options.SourcePath;
            DestinationPath = options.DestinationPath;
            WatermarkText = options.WatermarkText;
            Verbose = options.Verbose;
        }

        //public Arguments(IArguments options)
        //{
        //    SourcePath = options.SourcePath;
        //    DestinationPath = options.DestinationPath;
        //    WatermarkText = options.WatermarkText;
        //    Verbose = options.Verbose;
        //}

        public Arguments(string sourcePath, string destinationPath, string watermarkText, bool verbose)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            WatermarkText = watermarkText;
            Verbose = verbose;
        }

        [Option('s', "source", Required = true, HelpText = "Set source file or path.")]
        public string SourcePath { get; set; }

        [Option('d', "destination", Required = true, HelpText = "Set destination path.")]
        public string DestinationPath { get; set; }

        [Option('t', "Text", Required = false, HelpText = "Text to use to watermark files.", Default = "COPY")]
        public string WatermarkText { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
        public bool Verbose { get; set; }

        [Usage(ApplicationAlias = "Empower Pdf")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Normal scenario", new Arguments(@"c:\temp\source", @"c:\temp",  "COPY", true ));
                yield return new Example("Logging warnings", UnParserSettings.WithGroupSwitchesOnly(), new Arguments(@"c:\temp\source", @"c:\temp", "Copy", true));
                yield return new Example("Logging errors", new[] { UnParserSettings.WithGroupSwitchesOnly(), UnParserSettings.WithUseEqualTokenOnly() }, new Arguments(@"c:\temp\source", @"c:\temp", "COYP", true));
            }
        }
    }
}
