using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ScheduledJobApp;

public class JobExecutor : IJobExecutor
{
    private readonly ILogger<JobExecutor> _logger;
    private readonly AppConfiguration _configuration;
    private readonly Stopwatch _stopwatch;

    public JobExecutor(ILogger<JobExecutor> logger, AppConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _stopwatch = new Stopwatch();
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_configuration);

        var jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("=== Job Execution Started ===");
        _logger.LogInformation("Job ID: {JobId}", jobId);
        _logger.LogInformation("Start Time: {StartTime:yyyy-MM-dd HH:mm:ss} UTC", startTime);
        _logger.LogInformation("Configured Sleep Duration: {SleepSeconds} seconds", _configuration.SleepSeconds);

        try
        {
            _stopwatch.Start();

            // Log environment information
            LogEnvironmentInfo();

            // Validate configuration
            if (_configuration.SleepSeconds < 0)
            {
                throw new ArgumentException($"Invalid sleep duration: {_configuration.SleepSeconds}. Must be >= 0.");
            }

            if (_configuration.SleepSeconds > 3600)
            {
                _logger.LogWarning("Sleep duration is longer than 1 hour: {SleepSeconds} seconds", _configuration.SleepSeconds);
            }

            // Simulate work with configured sleep
            _logger.LogInformation("Starting simulated work (sleeping for {SleepSeconds} seconds)...", _configuration.SleepSeconds);

            // Use a loop to check for cancellation periodically
            var totalSleepMs = _configuration.SleepSeconds * 1000;
            var checkIntervalMs = Math.Min(1000, totalSleepMs); // Check every second or less
            var elapsed = 0;

            while (elapsed < totalSleepMs && !cancellationToken.IsCancellationRequested)
            {
                var remaining = totalSleepMs - elapsed;
                var sleepTime = Math.Min(checkIntervalMs, remaining);

                await Task.Delay(sleepTime, cancellationToken);
                elapsed += sleepTime;

                // Log progress for long-running jobs
                if (_configuration.SleepSeconds >= 10 && elapsed % 5000 == 0)
                {
                    var progress = (double)elapsed / totalSleepMs * 100;
                    _logger.LogInformation("Progress: {Progress:F1}% ({Elapsed}s / {Total}s)",
                        progress, elapsed / 1000, _configuration.SleepSeconds);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Job execution was cancelled");
                throw new OperationCanceledException("Job execution was cancelled by shutdown signal");
            }

            _stopwatch.Stop();

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            _logger.LogInformation("Simulated work completed successfully");
            _logger.LogInformation("=== Job Execution Completed ===");
            _logger.LogInformation("Job ID: {JobId}", jobId);
            _logger.LogInformation("End Time: {EndTime:yyyy-MM-dd HH:mm:ss} UTC", endTime);
            _logger.LogInformation("Duration: {Duration:hh\\:mm\\:ss\\.fff}", duration);
            _logger.LogInformation("Actual Elapsed Time: {ElapsedMs}ms", _stopwatch.ElapsedMilliseconds);
            _logger.LogInformation("Status: SUCCESS");

            // Write success marker (for container scenarios)
            await WriteCompletionMarker(jobId, duration, true);
        }
        catch (OperationCanceledException)
        {
            _stopwatch.Stop();
            _logger.LogWarning("Job execution cancelled after {ElapsedMs}ms", _stopwatch.ElapsedMilliseconds);
            await WriteCompletionMarker(jobId, TimeSpan.FromMilliseconds(_stopwatch.ElapsedMilliseconds), false);
            throw;
        }
        catch (Exception ex)
        {
            _stopwatch.Stop();
            _logger.LogError(ex, "Job execution failed after {ElapsedMs}ms: {Error}",
                _stopwatch.ElapsedMilliseconds, ex.Message);
            await WriteCompletionMarker(jobId, TimeSpan.FromMilliseconds(_stopwatch.ElapsedMilliseconds), false, ex);
            throw;
        }
    }

    private void LogEnvironmentInfo()
    {
        _logger.LogDebug("=== Environment Information ===");

        // Log environment variables that might affect execution
        var importantEnvVars = new[]
        {
            "DOTNET_ENVIRONMENT",
            "SLEEP_DURATION_SECONDS",
            "INSTANCE_LOCK_TYPE",
            "INSTANCE_LOCK_FILE"
        };

        foreach (var envVar in importantEnvVars)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(value))
            {
                _logger.LogDebug("Environment: {EnvVar}={Value}", envVar, value);
            }
        }

        // Check if sleep duration was overridden by environment variable
        var envSleep = Environment.GetEnvironmentVariable("SLEEP_DURATION_SECONDS");
        if (!string.IsNullOrEmpty(envSleep) && int.TryParse(envSleep, out int envSleepSeconds))
        {
            if (envSleepSeconds != _configuration.SleepSeconds)
            {
                _logger.LogInformation("Note: Sleep duration from command-line ({CmdSleep}s) " +
                    "differs from environment variable ({EnvSleep}s). Using command-line value.",
                    _configuration.SleepSeconds, envSleepSeconds);
            }
        }
    }

    private async Task WriteCompletionMarker(string jobId, TimeSpan duration, bool success, Exception? exception = null)
    {
        try
        {
            var logsDir = Path.Combine(Environment.CurrentDirectory, "logs");
            Directory.CreateDirectory(logsDir);

            var markerType = success ? "success" : "error";
            var markerPath = Path.Combine(logsDir, $"job-{jobId}-{markerType}.marker");

            var content = $"Job ID: {jobId}\n" +
                         $"Status: {(success ? "SUCCESS" : "FAILED")}\n" +
                         $"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                         $"Duration: {duration:hh\\:mm\\:ss\\.fff}\n" +
                         $"Sleep Configuration: {_configuration.SleepSeconds} seconds\n" +
                         $"Instance Name: {_configuration.InstanceName}\n";

            if (exception != null)
            {
                content += $"Error: {exception.Message}\n" +
                          $"Type: {exception.GetType().Name}\n";
            }

            await File.WriteAllTextAsync(markerPath, content);
            _logger.LogDebug("Completion marker written: {MarkerPath}", markerPath);
        }
        catch (Exception ex)
        {
            // Don't fail the job if we can't write the marker
            _logger.LogWarning(ex, "Failed to write completion marker");
        }
    }
}