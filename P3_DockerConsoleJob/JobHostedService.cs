using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ScheduledJobApp;

public class JobHostedService : IHostedService
{
    private readonly ILogger<JobHostedService> _logger;
    private readonly IInstanceManager _instanceManager;
    private readonly IJobExecutor _jobExecutor;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly AppConfiguration _configuration;
    private CancellationTokenSource? _cancellationTokenSource;

    public JobHostedService(
        ILogger<JobHostedService> logger,
        IInstanceManager instanceManager,
        IJobExecutor jobExecutor,
        IHostApplicationLifetime applicationLifetime,
        AppConfiguration configuration)
    {
        _logger = logger;
        _instanceManager = instanceManager;
        _jobExecutor = jobExecutor;
        _applicationLifetime = applicationLifetime;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Hosted Service starting...");

        // Create a linked token source for graceful shutdown
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Register for shutdown events
        _applicationLifetime.ApplicationStopping.Register(() =>
        {
            _logger.LogInformation("Application is stopping, initiating graceful shutdown...");
            _cancellationTokenSource?.Cancel();
        });

        // Try to acquire the instance lock
        _logger.LogInformation("Checking for existing instances...");
        bool lockAcquired = await _instanceManager.TryAcquireLockAsync();

        if (!lockAcquired)
        {
            _logger.LogError("Another instance is already running. Exiting with code 2.");
            _logger.LogInformation("=== Application Exit ===");
            _logger.LogInformation("Exit Code: 2 (Instance already running)");
            _logger.LogInformation("To override, use a different --instance-name or terminate the existing instance");

            // Stop the application with exit code 2
            Environment.ExitCode = 2;
            _applicationLifetime.StopApplication();
            return;
        }

        _logger.LogInformation("Instance lock acquired successfully, proceeding with job execution");

        // Execute the job asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await _jobExecutor.ExecuteAsync(_cancellationTokenSource.Token);
                _logger.LogInformation("Job execution completed, shutting down application");

                // Set success exit code
                Environment.ExitCode = 0;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Job execution was cancelled");
                Environment.ExitCode = 0; // Graceful shutdown is still considered success
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job execution failed with error");
                Environment.ExitCode = 1;
            }
            finally
            {
                // Release the lock
                _instanceManager.ReleaseLock();

                // Stop the application
                _applicationLifetime.StopApplication();
            }
        }, _cancellationTokenSource.Token);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Hosted Service stopping...");

        // Cancel any ongoing operations
        _cancellationTokenSource?.Cancel();

        // Release the lock if still held
        _instanceManager.ReleaseLock();

        _logger.LogInformation("Job Hosted Service stopped");
        _logger.LogInformation("=== Application Shutdown Complete ===");
        _logger.LogInformation("Final Exit Code: {ExitCode}", Environment.ExitCode);

        // Dispose of the cancellation token source
        _cancellationTokenSource?.Dispose();

        return Task.CompletedTask;
    }
}