# ScheduledJobApp

A .NET 8.0 console application with singleton instance management designed for containerized scheduled job execution.

## Features

- **Singleton Instance Management**: Prevents multiple instances from running simultaneously using mutex or file-based locking
- **Configurable Sleep Duration**: Simulates long-running processes with adjustable sleep time
- **Graceful Shutdown**: Handles SIGTERM/SIGINT signals for clean termination
- **Structured Logging**: Container-optimized logging with timestamps and correlation IDs
- **Command-Line Arguments**: Flexible configuration via CLI parameters
- **Exit Codes**: Meaningful exit codes for different scenarios

## Prerequisites

- .NET 8.0 SDK or Runtime
- Docker (for containerized deployment)

## Building the Application

```bash
# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Run tests
dotnet test Tests/ScheduledJobApp.Tests.csproj

# Create production build
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
```

## Running the Application

### Basic Usage

```bash
# Run with default settings (10 second sleep)
dotnet run

# Run with custom sleep duration
dotnet run -- --sleep-seconds 60

# Run with custom instance name
dotnet run -- --instance-name MyJob

# Use file-based locking instead of mutex
dotnet run -- --enable-file-lock

# Set log level
dotnet run -- --log-level Debug
```

### Command-Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `--sleep-seconds <int>` | Duration to sleep in seconds | 10 |
| `--instance-name <string>` | Custom instance identifier for mutex naming | ScheduledJobApp |
| `--enable-file-lock` | Use file-based locking instead of mutex | false |
| `--log-level <level>` | Set logging verbosity (Trace/Debug/Information/Warning/Error) | Information |

### Environment Variables

| Variable | Description |
|----------|-------------|
| `SLEEP_DURATION_SECONDS` | Override default sleep duration |
| `INSTANCE_LOCK_TYPE` | Choose between "Mutex" or "FileLock" |
| `DOTNET_ENVIRONMENT` | Set environment (Development/Staging/Production) |
| `INSTANCE_LOCK_FILE` | Custom path for lock file |

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Successful execution |
| 1 | General error or exception |
| 2 | Another instance is already running |
| 3 | Configuration error |

## Instance Management

The application implements singleton pattern to prevent concurrent executions:

### Mutex-Based Locking (Default)
- Creates a globally unique named mutex
- Immediately checks if mutex can be acquired
- If another instance holds the mutex, exits with code 2

### File-Based Locking (Alternative)
- Creates an exclusive lock file in temp directory
- Writes process information to lock file
- Automatically deletes lock file on exit

## Testing Multiple Instances

```bash
# Terminal 1: Start first instance with 60 second sleep
dotnet run -- --sleep-seconds 60

# Terminal 2: Try to start second instance (will exit with code 2)
dotnet run -- --sleep-seconds 10

# Terminal 2: Start with different instance name (will run)
dotnet run -- --instance-name OtherJob --sleep-seconds 10
```

## Graceful Shutdown

The application handles shutdown signals properly:

```bash
# Start the application
dotnet run -- --sleep-seconds 60

# In another terminal, send SIGTERM (or press Ctrl+C)
kill -TERM <process_id>
```

The application will:
1. Log shutdown initiation
2. Cancel ongoing operations
3. Release instance lock
4. Exit with appropriate code

## Logging

Logs are structured for container environments:
- Timestamps in UTC format
- No color codes (container-friendly)
- Correlation IDs for tracking
- Multiple log levels for debugging

Example log output:
```
[2025-09-14 10:30:00] info: ScheduledJobApp.Program[0]
      === Application Startup ===
[2025-09-14 10:30:00] info: ScheduledJobApp.JobHostedService[0]
      Checking for existing instances...
[2025-09-14 10:30:00] info: ScheduledJobApp.InstanceManager[0]
      Successfully acquired mutex lock (new instance)
[2025-09-14 10:30:00] info: ScheduledJobApp.JobExecutor[0]
      === Job Execution Started ===
[2025-09-14 10:30:00] info: ScheduledJobApp.JobExecutor[0]
      Job ID: a1b2c3d4
```

## Completion Markers

The application writes completion markers to the `logs` directory:
- `job-{id}-success.marker`: Successful execution
- `job-{id}-error.marker`: Failed execution

These markers contain:
- Job ID
- Status
- Timestamp
- Duration
- Configuration details
- Error information (if failed)

## Docker Usage

Build and run in Docker:

```bash
# Build Docker image
docker build -t scheduled-job:latest .

# Run container with 30 second sleep
docker run --rm scheduled-job:latest --sleep-seconds 30

# Run with volume mount for logs
docker run --rm -v $(pwd)/logs:/app/logs scheduled-job:latest

# Run with environment variables
docker run --rm -e SLEEP_DURATION_SECONDS=45 scheduled-job:latest
```

## Troubleshooting

### "Another instance is already running"
- Check if another process is running: `ps aux | grep ScheduledJobApp`
- Use different instance name: `--instance-name UniqueJob`
- Enable file lock and check lock file: `--enable-file-lock`

### Lock file issues
- Check temp directory permissions
- Verify lock file location: `/tmp/ScheduledJobApp.lock`
- Set custom lock file: `export INSTANCE_LOCK_FILE=/custom/path/app.lock`

### Mutex issues on Linux/macOS
- Named mutexes may not work in some environments
- Use file-based locking: `--enable-file-lock`

## License

This is a tutorial/example application for educational purposes.