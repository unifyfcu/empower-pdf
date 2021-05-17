using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;

namespace empower_pdf
{
    internal class Program
    {
        public const string OutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
        public const string LogName = ".log";
        public static readonly string AppStarted = $"Application Start: {DateTime.Now}";
        public static LoggingLevelSwitch LevelSwitch = new LoggingLevelSwitch();
        //public static Options Option { get; set; }

        private static Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: OutputTemplate)
                .WriteTo.File(LogName, rollingInterval: RollingInterval.Day, outputTemplate: OutputTemplate)
                .CreateLogger();
            Log.Information(AppStarted);
            Log.Information("Information output enabled.");
            IArguments options = Parser.Default.ParseArguments<Arguments>(args).MapResult(GetOptions, HandleParseError);

            if (options.Verbose)
            {
                LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
                Log.Verbose("Verbose output enabled.");
            }

            // var options = new Options(args);

            using var host =  Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                    services.AddTransient<IArguments>(p => options)
                        .AddTransient<IWatermarkContract, WatermarkService>())
                .Build();

            var files = Directory.GetFiles(options.SourcePath);
            var time = new Stopwatch();

            using (var progress = new ProgressBar(files.Length))
            {
                time.Reset();
                time.Start();
                foreach (var file in files)
                {
                    progress.Report();
                    var fileName = file.Replace($"{options.SourcePath}\\", "");
                    ScopedProcess(host.Services, fileName);
                }
                time.Stop();
                progress.Report();
            }
            var timeForeach = time.Elapsed;


            Log.Information($"Application Finished: {DateTime.Now}");
            Log.Information($"Time taken: {timeForeach}");
            return host.RunAsync();
        }

        public static Arguments GetOptions(Arguments opts)
        {
            if (!opts.Verbose) return opts;
            LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            Log.Verbose("Verbose output enabled.");
            return opts;
        }

        public static Arguments HandleParseError(IEnumerable<Error> errs)
        {
            var enumerable = errs as Error[] ?? errs.ToArray();
            foreach (var err in enumerable)
            {
                Log.Error(err.Tag.ToString());
            }
            throw new Exception(string.Join(@",", enumerable.Select(s => s.Tag)));
        }

        private static void ScopedProcess(IServiceProvider services, string fileName)
        {
            using var serviceScope = services.CreateScope();
            var provider = serviceScope.ServiceProvider;

            //do the actual work here
            var service = provider.GetService<IWatermarkContract>();

            Debug.Assert(service != null, nameof(service) + " != null");
            service.ProcessFiles(fileName);
        }
    }
}