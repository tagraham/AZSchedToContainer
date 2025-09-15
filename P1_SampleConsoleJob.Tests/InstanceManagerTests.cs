using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace ScheduledJobApp.Tests;

public class InstanceManagerTests
{
    private readonly Mock<ILogger<InstanceManager>> _mockLogger;
    private readonly AppConfiguration _configuration;

    public InstanceManagerTests()
    {
        _mockLogger = new Mock<ILogger<InstanceManager>>();
        _configuration = new AppConfiguration
        {
            InstanceName = "TestInstance",
            EnableFileLock = false,
            SleepSeconds = 5
        };
    }

    [Fact]
    public async Task TryAcquireLockAsync_WhenNoOtherInstance_ReturnsTrue()
    {
        // Arrange
        var instanceManager = new InstanceManager(_mockLogger.Object, _configuration);

        // Act
        var result = await instanceManager.TryAcquireLockAsync();

        // Assert
        Assert.True(result);

        // Cleanup
        instanceManager.ReleaseLock();
    }

    [Fact]
    public async Task TryAcquireLockAsync_WhenFileLockEnabled_UsesFileLock()
    {
        // Arrange
        _configuration.EnableFileLock = true;
        var instanceManager = new InstanceManager(_mockLogger.Object, _configuration);

        // Act
        var result = await instanceManager.TryAcquireLockAsync();

        // Assert
        Assert.True(result);

        // Cleanup
        instanceManager.ReleaseLock();
    }

    [Fact]
    public async Task ReleaseLock_WhenLockAcquired_ReleasesSuccessfully()
    {
        // Arrange
        var instanceManager = new InstanceManager(_mockLogger.Object, _configuration);
        await instanceManager.TryAcquireLockAsync();

        // Act
        instanceManager.ReleaseLock();

        // Assert - verify we can acquire lock again after release
        var canReacquire = await instanceManager.TryAcquireLockAsync();
        Assert.True(canReacquire);

        // Cleanup
        instanceManager.ReleaseLock();
    }

    [Fact]
    public async Task Dispose_ReleasesLock()
    {
        // Arrange
        var instanceManager = new InstanceManager(_mockLogger.Object, _configuration);
        await instanceManager.TryAcquireLockAsync();

        // Act
        instanceManager.Dispose();

        // Assert - verify lock is released by trying to acquire with a new instance
        var newInstanceManager = new InstanceManager(_mockLogger.Object, _configuration);
        var canAcquire = await newInstanceManager.TryAcquireLockAsync();
        Assert.True(canAcquire);

        // Cleanup
        newInstanceManager.Dispose();
    }
}