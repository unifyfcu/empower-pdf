using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.IO.Directory;
using System.Linq;
using System.Threading.Tasks;
using ShellProgressBar;


namespace empower_pdf
{
    internal class Program
    {
        private const string OutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
        private static readonly string AppStarted = $"Application Start: {DateTime.Now}";
        private static readonly LoggingLevelSwitch LevelSwitch = new();

        private static TimeSpan TimeRemaining(int step, int steps, DateTime start)
        {
            var totalTime = DateTime.Now - start;
            return TimeSpan.FromSeconds((totalTime.TotalSeconds / step) * steps);
        }

        private static Task Main(string[] args)
        {
            var arguments = ConfigureArguments(args);
            ConfigureLogging(arguments);
            using var host = CreateHostBuilder(arguments).Build();
            var files = GetFiles(arguments.SourcePath);
            var stopwatch = new Stopwatch();
            var start = DateTime.Now;
            var steps = files.Length;
            var paddingLength = steps.ToString().Length;
            var step = 0;
            using var progressBar = new ProgressBar(steps, "Processing files.", new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Green,
                ForegroundColorDone = ConsoleColor.DarkGreen,
                BackgroundColor = ConsoleColor.DarkGray,
                BackgroundCharacter = '\u2591',
                ProgressCharacter = '\u2588',
                ProgressBarOnBottom = true,
                ShowEstimatedDuration = true,
                DisplayTimeInRealTime = true,
                EnableTaskBarProgress = true
            });
            foreach (var file in files)
            {
                step++;
                var fileName = file.Replace($"{arguments.SourcePath}\\", "");
                progressBar.Tick(TimeRemaining(step, steps, start), $"Step: {step.ToString().PadLeft(paddingLength)} of {steps}; Processing file: {fileName}.");
                ScopedProcess(host.Services, fileName);
            }
            progressBar.Dispose();
            stopwatch.Stop();
            Log.Information($"Application Finished: {DateTime.Now}");
            Log.Information($"Time taken: {stopwatch.Elapsed}");
            return host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(IArguments arguments) =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                    services.AddTransient(_ => arguments)
                        .AddTransient<IWatermarkContract, WatermarkService>());

        private static void ConfigureLogging(IArguments arguments)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File($"{arguments.DestinationPath}\\.log", rollingInterval: RollingInterval.Day, outputTemplate: OutputTemplate)
                .CreateLogger();
            Log.Information(AppStarted);
            Log.Information("Information output enabled.");
        }

        private static IArguments ConfigureArguments(string[] args)
        {
            IArguments arguments = Parser.Default.ParseArguments<Arguments>(args).MapResult(GetArguments, HandleParseError);
            if (!arguments.Verbose) return arguments;
            LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            Log.Verbose("Verbose output enabled.");
            return arguments;
        }

        private static Arguments GetArguments(Arguments arguments)
        {
            if (!arguments.Verbose) return arguments;
            LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            Log.Verbose("Verbose output enabled.");
            return arguments;
        }

        private static Arguments HandleParseError(IEnumerable<Error> errs)
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
            var service = provider.GetService<IWatermarkContract>();
            service?.ProcessFiles(fileName);
        }
    }
}