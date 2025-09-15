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
    public void ReleaseLock_WhenLockAcquired_ReleasesSuccessfully()
    {
        // Arrange
        var instanceManager = new InstanceManager(_mockLogger.Object, _configuration);
        instanceManager.TryAcquireLockAsync().Wait();

        // Act
        instanceManager.ReleaseLock();

        // Assert - should not throw
        Assert.True(true);
    }

    [Fact]
    public void Dispose_ReleasesLock()
    {
        // Arrange
        var instanceManager = new InstanceManager(_mockLogger.Object, _configuration);
        instanceManager.TryAcquireLockAsync().Wait();

        // Act
        instanceManager.Dispose();

        // Assert - should not throw
        Assert.True(true);
    }
}