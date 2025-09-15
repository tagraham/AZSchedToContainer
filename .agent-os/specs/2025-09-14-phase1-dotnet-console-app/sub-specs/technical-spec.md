# Technical Specification

This is the technical specification for the spec detailed in @.agent-os/specs/2025-09-14-phase1-dotnet-console-app/spec.md

## Technical Requirements

### Core Application Structure
- **.NET 8.0 Console Application** targeting `net8.0` framework with `linux-x64` runtime identifier
- **Dependency Injection** using `Microsoft.Extensions.Hosting` for service registration and lifecycle management
- **Structured Logging** with `Microsoft.Extensions.Logging` and console output provider configured for container environments
- **Async/Await Pattern** throughout the application for better resource utilization

### Instance Management System
- **Named Mutex Implementation** using `System.Threading.Mutex` with a globally unique name (e.g., "Global\\ScheduledJobApp-{GUID}")
- **Mutex Acquisition Logic**:
  - Try to acquire mutex with `mutex.WaitOne(0)` for immediate check
  - If acquisition fails, log "Another instance is already running" and exit with code 2
  - If acquisition succeeds, hold mutex for entire application lifetime
- **Fallback File Lock** mechanism using `FileStream` with `FileShare.None` as secondary option for environments where mutex isn't available
- **Lock File Location**: `/tmp/scheduledjobapp.lock` or configurable via environment variable

### Configuration System
- **Command-Line Arguments** using `System.CommandLine` or manual parsing:
  - `--sleep-seconds <int>`: Duration to sleep in seconds (default: 10)
  - `--instance-name <string>`: Custom instance identifier for mutex naming
  - `--enable-file-lock`: Use file-based locking instead of mutex
  - `--log-level <string>`: Set logging verbosity (Trace|Debug|Information|Warning|Error)
- **Environment Variables**:
  - `SLEEP_DURATION_SECONDS`: Override default sleep duration
  - `INSTANCE_LOCK_TYPE`: Choose between "Mutex" or "FileLock"
  - `DOTNET_ENVIRONMENT`: Development|Staging|Production

### Logging Specifications
- **Log Format**: Structured logging with timestamp, log level, and correlation ID
- **Required Log Points**:
  - Application startup with version and configuration
  - Instance lock acquisition attempt and result
  - Sleep duration configuration and start/end
  - Resource cleanup and application shutdown
  - Any errors or exceptions with full stack traces
- **Console Configuration**:
  - Disable colors for container compatibility: `options.DisableColors = true`
  - Include timestamps: `options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "`

### Process Lifecycle Management
- **Graceful Shutdown** handling for SIGTERM and SIGINT signals using `IHostApplicationLifetime`
- **Resource Cleanup**:
  - Release mutex on application exit (normal or abnormal)
  - Delete lock file if using file-based locking
  - Flush all pending log messages
- **Exit Codes**:
  - 0: Successful execution
  - 1: General error or exception
  - 2: Another instance already running
  - 3: Configuration error

### Error Handling
- **Global Exception Handler** in Main method to catch and log unhandled exceptions
- **Try-Finally Blocks** for critical resource cleanup (mutex/file lock release)
- **Timeout Protection** for sleep operation to prevent indefinite hanging
- **Validation** of command-line arguments with clear error messages

## External Dependencies

- **Microsoft.Extensions.Hosting** (Version 8.0.0) - Required for dependency injection container and application lifecycle management
- **Microsoft.Extensions.Logging** (Version 8.0.0) - Core logging abstractions and infrastructure
- **Microsoft.Extensions.Logging.Console** (Version 8.0.0) - Console logging provider for structured output
- **System.CommandLine** (Version 2.0.0-beta4) - Optional but recommended for robust command-line argument parsing

**Justification:** These packages are part of the standard .NET ecosystem and provide production-ready implementations of critical infrastructure components. They ensure consistency with cloud-native patterns and simplify the eventual containerization process.