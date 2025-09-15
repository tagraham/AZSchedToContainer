using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace ScheduledJobApp.Tests;

public class JobExecutorTests
{
    private readonly Mock<ILogger<JobExecutor>> _mockLogger;
    private readonly AppConfiguration _configuration;

    public JobExecutorTests()
    {
        _mockLogger = new Mock<ILogger<JobExecutor>>();
        _configuration = new AppConfiguration
        {
            SleepSeconds = 1, // Short sleep for tests
            InstanceName = "TestInstance",
            EnableFileLock = false
        };
    }

    [Fact]
    public async Task ExecuteAsync_WithValidConfiguration_CompletesSuccessfully()
    {
        // Arrange
        var jobExecutor = new JobExecutor(_mockLogger.Object, _configuration);
        var cancellationToken = CancellationToken.None;

        // Act & Assert - should not throw
        await jobExecutor.ExecuteAsync(cancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        _configuration.SleepSeconds = 10; // Longer sleep to ensure cancellation happens
        var jobExecutor = new JobExecutor(_mockLogger.Object, _configuration);
        var cts = new CancellationTokenSource();

        // Act
        var executeTask = jobExecutor.ExecuteAsync(cts.Token);
        await Task.Delay(100); // Let execution start
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => executeTask);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task ExecuteAsync_WithVariousSleepDurations_CompletesInExpectedTime(int sleepSeconds)
    {
        // Arrange
        _configuration.SleepSeconds = sleepSeconds;
        var jobExecutor = new JobExecutor(_mockLogger.Object, _configuration);
        var cancellationToken = CancellationToken.None;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await jobExecutor.ExecuteAsync(cancellationToken);
        stopwatch.Stop();

        // Assert - execution time should be close to configured sleep time
        var tolerance = 500; // 500ms tolerance
        Assert.True(stopwatch.ElapsedMilliseconds >= sleepSeconds * 1000 - tolerance);
        Assert.True(stopwatch.ElapsedMilliseconds <= sleepSeconds * 1000 + tolerance);
    }

    [Fact]
    public void ExecuteAsync_WithNegativeSleepDuration_ThrowsArgumentException()
    {
        // Arrange
        _configuration.SleepSeconds = -1;
        var jobExecutor = new JobExecutor(_mockLogger.Object, _configuration);
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => jobExecutor.ExecuteAsync(cancellationToken));
    }
}