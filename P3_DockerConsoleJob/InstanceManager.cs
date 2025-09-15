using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ScheduledJobApp;

public class InstanceManager : IInstanceManager, IDisposable
{
    private readonly ILogger<InstanceManager> _logger;
    private readonly AppConfiguration _configuration;
    private Mutex? _mutex;
    private FileStream? _lockFileStream;
    private bool _lockAcquired = false;
    private readonly string _lockFilePath;

    public InstanceManager(ILogger<InstanceManager> logger, AppConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Determine lock file path
        var tempPath = Path.GetTempPath();
        _lockFilePath = Environment.GetEnvironmentVariable("INSTANCE_LOCK_FILE")
            ?? Path.Combine(tempPath, $"{_configuration.InstanceName}.lock");
    }

    public async Task<bool> TryAcquireLockAsync()
    {
        _logger.LogInformation("Attempting to acquire instance lock...");

        if (_configuration.EnableFileLock)
        {
            return await TryAcquireFileLockAsync();
        }
        else
        {
            return TryAcquireMutex();
        }
    }

    private bool TryAcquireMutex()
    {
        try
        {
            // Create a globally unique mutex name
            string mutexName = $"Global\\{_configuration.InstanceName}-{GetMutexGuid()}";
            _logger.LogDebug("Attempting to acquire mutex: {MutexName}", mutexName);

            // Try to create and acquire the mutex
            _mutex = new Mutex(false, mutexName, out bool createdNew);

            if (createdNew)
            {
                // We created the mutex, so we have the lock
                _lockAcquired = true;
                _logger.LogInformation("Successfully acquired mutex lock (new instance)");
                return true;
            }
            else
            {
                // Mutex already exists, try to acquire it with no wait
                try
                {
                    if (_mutex.WaitOne(0))
                    {
                        _lockAcquired = true;
                        _logger.LogInformation("Successfully acquired existing mutex lock");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Another instance is already running (mutex is held by another process)");
                        return false;
                    }
                }
                catch (AbandonedMutexException)
                {
                    // The mutex was abandoned by another process, we can acquire it
                    _lockAcquired = true;
                    _logger.LogWarning("Acquired abandoned mutex (previous instance may have crashed)");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire mutex lock, falling back to file lock");
            // Fall back to file lock
            return await TryAcquireFileLockAsync().ConfigureAwait(false);
        }
    }

    private async Task<bool> TryAcquireFileLockAsync()
    {
        try
        {
            _logger.LogDebug("Attempting to acquire file lock: {LockFile}", _lockFilePath);

            // Try to create/open the lock file exclusively
            _lockFileStream = new FileStream(
                _lockFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None, // Exclusive access
                4096,
                FileOptions.DeleteOnClose); // Delete file when stream is closed

            // Write process information to the lock file
            var processInfo = $"PID: {Environment.ProcessId}\n" +
                            $"Host: {Environment.MachineName}\n" +
                            $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n";

            var bytes = System.Text.Encoding.UTF8.GetBytes(processInfo);
            await _lockFileStream.WriteAsync(bytes, 0, bytes.Length);
            await _lockFileStream.FlushAsync();

            _lockAcquired = true;
            _logger.LogInformation("Successfully acquired file lock");
            return true;
        }
        catch (IOException ex) when (ex.Message.Contains("being used by another process"))
        {
            _logger.LogWarning("Another instance is already running (lock file is held by another process)");

            // Try to read the lock file to get information about the other instance
            TryLogExistingInstanceInfo();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire file lock");
            return false;
        }
    }

    private void TryLogExistingInstanceInfo()
    {
        try
        {
            if (File.Exists(_lockFilePath))
            {
                // Try to read the lock file (may fail if locked)
                var lastWriteTime = File.GetLastWriteTimeUtc(_lockFilePath);
                var age = DateTime.UtcNow - lastWriteTime;

                _logger.LogInformation("Existing instance lock file age: {Age:hh\\:mm\\:ss}", age);

                // Check if the lock might be stale (older than 1 hour)
                if (age.TotalHours > 1)
                {
                    _logger.LogWarning("Lock file is older than 1 hour, the other instance might be hung");
                }
            }
        }
        catch
        {
            // Ignore errors when trying to read lock file info
        }
    }

    public void ReleaseLock()
    {
        if (!_lockAcquired)
        {
            return;
        }

        _logger.LogInformation("Releasing instance lock...");

        try
        {
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
                _mutex = null;
                _logger.LogInformation("Mutex lock released");
            }

            if (_lockFileStream != null)
            {
                _lockFileStream.Close();
                _lockFileStream.Dispose();
                _lockFileStream = null;
                _logger.LogInformation("File lock released");
            }

            _lockAcquired = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock");
        }
    }

    private string GetMutexGuid()
    {
        // Generate a consistent GUID based on the instance name
        // This ensures the same mutex name across runs
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(_configuration.InstanceName));
        // Take first 16 bytes for GUID creation
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes).ToString("N").Substring(0, 8);
    }

    public void Dispose()
    {
        ReleaseLock();
    }
}