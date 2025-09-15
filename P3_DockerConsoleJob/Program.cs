using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Reflection;

namespace ScheduledJobApp;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure console for container environments
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            // Parse command-line arguments
            var rootCommand = new RootCommand("Scheduled Job Application with Instance Management");

            var sleepOption = new Option<int>(
                name: "--sleep-seconds",
                description: "Duration to sleep in seconds",
                getDefaultValue: () => 10);

            var instanceNameOption = new Option<string>(
                name: "--instance-name",
                description: "Custom instance identifier for mutex naming",
                getDefaultValue: () => "ScheduledJobApp");

            var enableFileLockOption = new Option<bool>(
                name: "--enable-file-lock",
                description: "Use file-based locking instead of mutex",
                getDefaultValue: () => false);

            var logLevelOption = new Option<LogLevel>(
                name: "--log-level",
                description: "Set logging verbosity",
                getDefaultValue: () => LogLevel.Information);

            rootCommand.AddOption(sleepOption);
            rootCommand.AddOption(instanceNameOption);
            rootCommand.AddOption(enableFileLockOption);
            rootCommand.AddOption(logLevelOption);

            int sleepSeconds = 10;
            string instanceName = "ScheduledJobApp";
            bool enableFileLock = false;
            LogLevel logLevel = LogLevel.Information;

            rootCommand.SetHandler(
                (int sleep, string instance, bool fileLock, LogLevel level) =>
                {
                    sleepSeconds = sleep;
                    instanceName = instance;
                    enableFileLock = fileLock;
                    logLevel = level;
                },
                sleepOption, instanceNameOption, enableFileLockOption, logLevelOption);

            await rootCommand.InvokeAsync(args);

            // Create host builder with dependency injection
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register configuration
                    services.AddSingleton(new AppConfiguration
                    {
                        SleepSeconds = sleepSeconds,
                        InstanceName = instanceName,
                        EnableFileLock = enableFileLock
                    });

                    // Register services
                    services.AddSingleton<IInstanceManager, InstanceManager>();
                    services.AddSingleton<IJobExecutor, JobExecutor>();
                    services.AddHostedService<JobHostedService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole(options =>
                    {
                        // Optimize for container logs
                        options.DisableColors = true;
                        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                    });
                    logging.SetMinimumLevel(logLevel);
                })
                .Build();

            // Log application startup information
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            LogStartupInfo(logger);

            // Run the application
            await host.RunAsync();

            logger.LogInformation("Application completed successfully");
            return 0; // Success
        }
        catch (Exception ex)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogError(ex, "Application failed: {Error}", ex.Message);
            return 1; // Error
        }
    }

    private static void LogStartupInfo(ILogger logger)
    {
        logger.LogInformation("=== Application Startup ===");
        logger.LogInformation("Application: ScheduledJobApp");
        logger.LogInformation("Version: {Version}",
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0");
        logger.LogInformation("Hostname: {Hostname}", Environment.MachineName);
        logger.LogInformation("Platform: {Platform}", Environment.OSVersion.Platform);
        logger.LogInformation("Working Directory: {WorkingDir}", Environment.CurrentDirectory);
        logger.LogInformation("User: {User}", Environment.UserName);
        logger.LogInformation("Process ID: {ProcessId}", Environment.ProcessId);
        logger.LogInformation("Environment: {Environment}",
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production");
    }
}

// Configuration class
public class AppConfiguration
{
    public int SleepSeconds { get; set; } = 10;
    public string InstanceName { get; set; } = "ScheduledJobApp";
    public bool EnableFileLock { get; set; } = false;
}

// Service interfaces
public interface IInstanceManager
{
    Task<bool> TryAcquireLockAsync();
    void ReleaseLock();
}

public interface IJobExecutor
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}