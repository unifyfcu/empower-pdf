using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using CommandLine;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.Windows.Forms;
using Serilog;

namespace empower_pdf
{

    internal class Program
    {
        public class Options
        {
            [Option('s', "source", Required = true, HelpText = "Set source file or path.")]
            public string Source { get; set; }

            [Option('d', "destination", Required = true, HelpText = "Set destination file or path.")]
            public string Destination { get; set; }

            [Option('w', "watermark", Required = false, HelpText = "Watermark files.", Default = false)]
            public bool Watermark { get; set; }

            [Option('t', "Text", Required = false, HelpText = "Text to use to watermark files.", Default = "Copy")]
            public string WatermarkText { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
            public bool Verbose { get; set; }
        }

        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    if (o.Verbose)
                    {
                        Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Verbose()
                            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .WriteTo.File("log.log", rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .CreateLogger();
                        Log.Information("Application started.");
                        Log.Verbose("Verbose output enabled.");
                    }
                    else
                    {
                        Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Information()
                            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .WriteTo.File("log.log", rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .CreateLogger();
                        Log.Information("Application started.");
                        Log.Information("Error output enabled.");
                    }
                    Log.Information($"Application Start: {DateTime.Now}");

                    if (!Directory.Exists(o.Source))
                    {
                        var m = $"Source directory \"{o.Source}\" does not exist!";
                        Log.Error(m);
                        throw new Exception(m);
                    }

                    if (!Directory.Exists(o.Destination))
                    {
                        var m = $"Destination directory \"{o.Destination}\" does not exist!";
                        Log.Error(m);
                        throw new Exception(m);
                    }


                    //setup our DI
                    var serviceProvider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
                        .AddLogging()
                        .AddSingleton<IWatermarkService, WatermarkService>()
                        .BuildServiceProvider();


                    //do the actual work here
                    var bar = serviceProvider.GetService<IWatermarkService>();
                    bar.DoThing(5);

                    Log.Information($"Application Finished: {DateTime.Now}");
                });
        }

        private static void WriteDocument(string dest)
        {
            var writer = new PdfWriter(dest);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);
            document.Add(new Paragraph("Hello World!"));
            document.Close();
        }
    }
}

