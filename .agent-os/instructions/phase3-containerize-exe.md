# Phase 3: Containerized Execution

> **Learning Time:** 2-3 hours
> **Skill Level:** Intermediate
> **Prerequisites:** Phase 1 & 2 completed, working Docker container

## Learning Objectives

By the end of this phase, you will:
- Run executables reliably inside Docker containers
- Configure persistent logging with volume mounts
- Handle container networking and communication
- Implement proper error handling and recovery
- Debug containerized applications effectively

## Step-by-Step Instructions

### Step 1: Enhanced Application Configuration

Update your `Program.cs` to be container-aware:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<JobExecutor>();
                    services.AddSingleton<ContainerHealthService>();
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
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            // Log container startup information
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            LogContainerInfo(logger);

            // Get the job executor service
            var jobExecutor = host.Services.GetRequiredService<JobExecutor>();
            var healthService = host.Services.GetRequiredService<ContainerHealthService>();

            // Start health monitoring
            _ = Task.Run(() => healthService.StartMonitoring());

            // Execute the job
            await jobExecutor.ExecuteAsync(args);

            logger.LogInformation("Application completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            var logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<Program>();
            logger.LogError(ex, "Application failed: {Error}", ex.Message);
            return 1;
        }
    }

    private static void LogContainerInfo(ILogger logger)
    {
        logger.LogInformation("=== Container Information ===");
        logger.LogInformation("Hostname: {Hostname}", Environment.MachineName);
        logger.LogInformation("Platform: {Platform}", Environment.OSVersion.Platform);
        logger.LogInformation("Working Directory: {WorkingDir}", Environment.CurrentDirectory);
        logger.LogInformation("User: {User}", Environment.UserName);
        logger.LogInformation("Process ID: {ProcessId}", Environment.ProcessId);
        logger.LogInformation("Assembly Version: {Version}",
            Assembly.GetExecutingAssembly().GetName().Version);

        // Log environment variables (excluding sensitive ones)
        var envVars = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .Where(e => !IsSensitiveEnvVar(e.Key.ToString()))
            .OrderBy(e => e.Key);

        foreach (var env in envVars)
        {
            logger.LogInformation("ENV: {Key}={Value}", env.Key, env.Value);
        }
    }

    private static bool IsSensitiveEnvVar(string key)
    {
        var sensitive = new[] { "PASSWORD", "SECRET", "TOKEN", "KEY", "CONNECTIONSTRING" };
        return sensitive.Any(s => key.ToUpperInvariant().Contains(s));
    }
}

public class ContainerHealthService
{
    private readonly ILogger<ContainerHealthService> _logger;

    public ContainerHealthService(ILogger<ContainerHealthService> logger)
    {
        _logger = logger;
    }

    public async Task StartMonitoring()
    {
        _logger.LogInformation("Health monitoring started");

        while (true)
        {
            await Task.Delay(30000); // Check every 30 seconds

            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var memoryMB = process.WorkingSet64 / 1024 / 1024;
                var cpuTime = process.TotalProcessorTime;

                _logger.LogInformation("Health Check - Memory: {MemoryMB}MB, CPU: {CpuTime}",
                    memoryMB, cpuTime);

                // Alert if memory usage is high
                if (memoryMB > 100)
                {
                    _logger.LogWarning("High memory usage detected: {MemoryMB}MB", memoryMB);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed: {Error}", ex.Message);
            }
        }
    }
}

public class JobExecutor
{
    private readonly ILogger<JobExecutor> _logger;

    public JobExecutor(ILogger<JobExecutor> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(string[] args)
    {
        var startTime = DateTime.UtcNow;
        var jobId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation("=== Job Execution Started ===");
        _logger.LogInformation("Job ID: {JobId}", jobId);
        _logger.LogInformation("Start Time: {StartTime:yyyy-MM-dd HH:mm:ss} UTC", startTime);
        _logger.LogInformation("Arguments: [{Args}]", string.Join(", ", args));
        _logger.LogInformation("Container Environment: {Environment}",
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production");

        try
        {
            // Log volume mount status
            await CheckVolumeStatus();

            // Simulate processing with progress updates
            await ProcessWithProgress(jobId);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            _logger.LogInformation("=== Job Execution Completed ===");
            _logger.LogInformation("Job ID: {JobId}", jobId);
            _logger.LogInformation("End Time: {EndTime:yyyy-MM-dd HH:mm:ss} UTC", endTime);
            _logger.LogInformation("Duration: {Duration:hh\\:mm\\:ss}", duration);
            _logger.LogInformation("Status: SUCCESS");

            // Write completion marker for external monitoring
            await WriteCompletionMarker(jobId, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job execution failed: {Error}", ex.Message);
            await WriteErrorMarker(jobId, ex);
            throw;
        }
    }

    private async Task CheckVolumeStatus()
    {
        try
        {
            var logsDir = "/app/logs";
            if (Directory.Exists(logsDir))
            {
                var info = new DirectoryInfo(logsDir);
                _logger.LogInformation("Logs directory exists: {Path}", logsDir);
                _logger.LogInformation("Directory permissions: {Permissions}",
                    info.Attributes);

                // Test write access
                var testFile = Path.Combine(logsDir, $"test-{DateTime.UtcNow:yyyyMMdd-HHmmss}.tmp");
                await File.WriteAllTextAsync(testFile, "Volume mount test");
                File.Delete(testFile);

                _logger.LogInformation("Volume mount: WRITABLE");
            }
            else
            {
                _logger.LogWarning("Logs directory not found: {Path}", logsDir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Volume status check failed: {Error}", ex.Message);
        }
    }

    private async Task ProcessWithProgress(string jobId)
    {
        var batchCount = 5;
        _logger.LogInformation("Starting processing {BatchCount} batches...", batchCount);

        for (int i = 1; i <= batchCount; i++)
        {
            _logger.LogInformation("Processing batch {Current}/{Total} (Job: {JobId})",
                i, batchCount, jobId);

            // Simulate varying processing times
            var delay = Random.Shared.Next(500, 2000);
            await Task.Delay(delay);

            // Simulate occasional warnings
            if (i == 3)
            {
                _logger.LogWarning("Batch {BatchNumber} encountered {WarningCount} warnings - continuing",
                    i, Random.Shared.Next(1, 5));
            }

            var progress = (double)i / batchCount * 100;
            _logger.LogInformation("Progress: {Progress:F1}% (Batch {Current} completed)",
                progress, i);
        }

        _logger.LogInformation("All batches processed successfully");
    }

    private async Task WriteCompletionMarker(string jobId, TimeSpan duration)
    {
        try
        {
            var markerPath = $"/app/logs/job-{jobId}-success.marker";
            var content = $"Job {jobId} completed successfully at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                         $"Duration: {duration:hh\\:mm\\:ss}";

            await File.WriteAllTextAsync(markerPath, content);
            _logger.LogInformation("Success marker written: {Path}", markerPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write success marker: {Error}", ex.Message);
        }
    }

    private async Task WriteErrorMarker(string jobId, Exception exception)
    {
        try
        {
            var markerPath = $"/app/logs/job-{jobId}-error.marker";
            var content = $"Job {jobId} failed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                         $"Error: {exception.Message}\n" +
                         $"Stack Trace:\n{exception.StackTrace}";

            await File.WriteAllTextAsync(markerPath, content);
            _logger.LogInformation("Error marker written: {Path}", markerPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write error marker: {Error}", ex.Message);
        }
    }
}
```

### Step 2: Enhanced Docker Configuration

Update your `Dockerfile` for better container integration:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ScheduledJobApp.csproj .
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime

# Install debugging tools (optional for development)
RUN apk add --no-cache \
    curl \
    procps \
    htop

# Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Set working directory and create directories
WORKDIR /app
RUN mkdir -p /app/logs /app/data && \
    chown -R appuser:appgroup /app

# Copy application
COPY --from=build /app .

# Create health check script
RUN echo '#!/bin/sh\nps aux | grep -v grep | grep ScheduledJobApp || exit 1' > /app/healthcheck.sh && \
    chmod +x /app/healthcheck.sh && \
    chown appuser:appgroup /app/healthcheck.sh

# Switch to non-root user
USER appuser

# Environment variables
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=""
ENV DOTNET_EnableDiagnostics=0

# Labels for container metadata
LABEL maintainer="tutorial@example.com"
LABEL version="1.0.0"
LABEL description="Scheduled Job Container Tutorial"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD /app/healthcheck.sh

# Entry point
ENTRYPOINT ["dotnet", "ScheduledJobApp.dll"]
```

### Step 3: Advanced Docker Compose Configuration

Update `docker-compose.yml` with advanced features:

```yaml
version: '3.8'

services:
  scheduled-job:
    build:
      context: .
      dockerfile: Dockerfile
      args:
        BUILD_VERSION: ${BUILD_VERSION:-1.0.0}
    image: scheduled-job:${BUILD_VERSION:-latest}
    container_name: scheduled-job-${ENVIRONMENT:-dev}
    hostname: scheduled-job-container

    environment:
      - DOTNET_ENVIRONMENT=${ENVIRONMENT:-Development}
      - TZ=UTC
      - JOB_CONFIG=${JOB_CONFIG:-default}

    volumes:
      # Persistent logs
      - ./logs:/app/logs
      # Configuration files (if needed)
      - ./config:/app/config:ro
      # Shared data directory
      - job_data:/app/data

    # Resource limits
    deploy:
      resources:
        limits:
          memory: 256M
          cpus: '0.5'
        reservations:
          memory: 64M
          cpus: '0.1'

    # Restart policy
    restart: unless-stopped

    # Logging configuration
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

    # Health check override (optional)
    healthcheck:
      test: ["CMD", "/app/healthcheck.sh"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s

    # Networks
    networks:
      - job_network

  # Log aggregation (optional)
  log-viewer:
    image: amir20/dozzle:latest
    container_name: log-viewer
    ports:
      - "8080:8080"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
    profiles:
      - monitoring
    networks:
      - job_network

  # System monitoring (optional)
  monitoring:
    image: nicolargo/glances:alpine-latest
    container_name: system-monitor
    ports:
      - "61208:61208"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
    environment:
      - GLANCES_OPT=-w
    profiles:
      - monitoring
    networks:
      - job_network

volumes:
  job_data:
    driver: local

networks:
  job_network:
    driver: bridge
```

### Step 4: Create Environment Configuration

Create `.env` file for environment variables:

```bash
# Build configuration
BUILD_VERSION=1.0.0

# Runtime environment
ENVIRONMENT=Development
JOB_CONFIG=development

# Optional: Docker registry (for later phases)
REGISTRY_URL=
REGISTRY_USERNAME=
```

### Step 5: Testing and Validation Scripts

Create `scripts/test-container.sh`:

```bash
#!/bin/bash
set -e

echo "=== Container Integration Tests ==="

# Build image
echo "Building Docker image..."
docker build -t scheduled-job:test .

# Test 1: Basic execution
echo "Test 1: Basic execution..."
docker run --rm scheduled-job:test
if [ $? -eq 0 ]; then
    echo "✅ Basic execution passed"
else
    echo "❌ Basic execution failed"
    exit 1
fi

# Test 2: Volume mounting
echo "Test 2: Volume mounting..."
mkdir -p ./test-logs
docker run --rm -v ./test-logs:/app/logs scheduled-job:test
if [ -f "./test-logs/job-*-success.marker" ]; then
    echo "✅ Volume mounting passed"
    rm -rf ./test-logs
else
    echo "❌ Volume mounting failed"
    exit 1
fi

# Test 3: Health check
echo "Test 3: Health check..."
CONTAINER_ID=$(docker run -d scheduled-job:test sleep 120)
sleep 65  # Wait for health check to start
HEALTH_STATUS=$(docker inspect --format='{{.State.Health.Status}}' $CONTAINER_ID)
docker rm -f $CONTAINER_ID
if [ "$HEALTH_STATUS" = "healthy" ]; then
    echo "✅ Health check passed"
else
    echo "❌ Health check failed: $HEALTH_STATUS"
    exit 1
fi

# Test 4: Error handling
echo "Test 4: Error handling..."
# This would need to be implemented based on your error scenarios

echo "🎉 All container integration tests passed!"
```

### Step 6: Run and Monitor Containers

```bash
# Make test script executable
chmod +x scripts/test-container.sh

# Run integration tests
./scripts/test-container.sh

# Start with monitoring
docker-compose --profile monitoring up -d

# View logs in real-time
docker-compose logs -f scheduled-job

# Check container health
docker ps
docker inspect scheduled-job-dev | grep -A 10 Health

# Monitor resource usage
docker stats scheduled-job-dev

# Access log viewer (if monitoring profile is up)
# Open http://localhost:8080 in browser
```

## 💡 Tips and Tricks

### Container Logging Best Practices
- **Structured logs:** Use consistent formats for parsing
- **No log files:** Write to stdout/stderr, not files (unless for persistence)
- **Correlation IDs:** Include job IDs for tracking
- **Log levels:** Use appropriate levels for filtering

### Volume Management
- **Bind mounts:** Use for development (./logs:/app/logs)
- **Named volumes:** Use for production data persistence
- **Read-only mounts:** For configuration files (:ro)
- **Permissions:** Ensure container user can write to mounted volumes

### Health Checks
- **Lightweight:** Keep health checks fast and simple
- **Meaningful:** Check actual application health, not just process existence
- **Timeouts:** Set appropriate timeouts for your application
- **Dependencies:** Include external dependency checks if needed

### Resource Management
- **Memory limits:** Prevent containers from consuming all host memory
- **CPU limits:** Share CPU resources fairly
- **Restart policies:** Handle container failures appropriately
- **Log rotation:** Prevent log files from filling disk

## ⚠️ Common Pitfalls

### Volume Issues
- **Permission denied:** Container user can't write to host directories
- **Path not found:** Host paths don't exist or are mistyped
- **Data loss:** Using bind mounts instead of volumes for production data
- **SELinux/AppArmor:** Security policies blocking volume access

### Networking Problems
- **Port conflicts:** Multiple containers trying to use same ports
- **DNS resolution:** Containers can't resolve external hostnames
- **Firewall rules:** Host firewall blocking container traffic
- **Bridge networks:** Default bridge doesn't allow container-to-container communication

### Performance Issues
- **Resource starvation:** Containers competing for resources
- **I/O bottlenecks:** Too many containers writing to same volume
- **Memory leaks:** Applications not releasing memory properly
- **CPU throttling:** Containers hitting CPU limits

### Monitoring Gaps
- **Log aggregation:** Logs scattered across multiple containers
- **Health checks:** Checks passing but application not working
- **Resource monitoring:** Not tracking container resource usage
- **Alerting:** No notifications when containers fail

## ✅ Verification Steps

### 1. Container Runs Successfully
```bash
docker run --rm scheduled-job:latest
# Should complete with exit code 0
```

### 2. Volume Persistence Works
```bash
mkdir -p ./test-logs
docker run --rm -v ./test-logs:/app/logs scheduled-job:latest
ls ./test-logs/
# Should see marker files
```

### 3. Health Check Functions
```bash
docker run -d --name health-test scheduled-job:latest sleep 300
sleep 65
docker ps
# Should show healthy status
docker rm -f health-test
```

### 4. Resource Limits Enforced
```bash
docker run --memory=64m --cpus=0.1 scheduled-job:latest
# Should run within resource constraints
```

### 5. Monitoring Tools Work
```bash
docker-compose --profile monitoring up -d
curl -f http://localhost:8080
# Should return Dozzle interface
```

## 🔍 Advanced Troubleshooting

### Debug Container Issues
```bash
# Run container interactively
docker run --rm -it scheduled-job:latest sh

# Check running processes
docker exec -it <container_id> ps aux

# Monitor resource usage
docker stats <container_id>

# Inspect container configuration
docker inspect <container_id>

# Check container logs
docker logs --follow <container_id>
```

### Network Debugging
```bash
# Test connectivity from container
docker run --rm scheduled-job:latest ping google.com

# Check DNS resolution
docker run --rm scheduled-job:latest nslookup google.com

# Inspect networks
docker network ls
docker network inspect bridge
```

### Volume Debugging
```bash
# Check volume permissions
docker run --rm -v ./logs:/app/logs scheduled-job:latest ls -la /app/logs

# Test write permissions
docker run --rm -v ./logs:/app/logs scheduled-job:latest touch /app/logs/test.txt

# Check volume mounts
docker inspect <container_id> | grep -A 10 Mounts
```

## 🎯 Learning Checkpoint

Before proceeding to Phase 4, ensure you can:
- [ ] Run containers with persistent volumes
- [ ] Monitor container health and resources
- [ ] Debug containerized applications
- [ ] Handle container failures gracefully
- [ ] Understand container networking basics

## 📚 Key Concepts Learned

1. **Container Integration:** Making applications container-aware
2. **Volume Management:** Persistent data and log handling
3. **Health Monitoring:** Container health checks and monitoring
4. **Resource Management:** CPU and memory limits
5. **Troubleshooting:** Debugging containerized applications

## Next Steps

Once you've successfully completed this phase, you're ready for **Phase 4: Azure Deployment**, where you'll deploy your containerized application to Azure Container Apps with full CI/CD automation.