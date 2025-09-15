# Phase 1: Create Simple Executable

> **Learning Time:** 2-3 hours
> **Skill Level:** Beginner
> **Prerequisites:** .NET 8.0 SDK installed

## Learning Objectives

By the end of this phase, you will:
- Create a .NET 8.0 console application from scratch
- Implement proper logging with timestamps
- Handle command-line arguments
- Build a production-ready executable
- Test the application locally

## Step-by-Step Instructions

### Step 1: Project Setup

Create a new console application:

```bash
# Create project directory
mkdir ScheduledJobExample
cd ScheduledJobExample

# Create .NET console application
dotnet new console -n ScheduledJobApp
cd ScheduledJobApp
```

### Step 2: Add Required Dependencies

Update your `.csproj` file to include logging and hosting extensions:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
  </ItemGroup>

</Project>
```

### Step 3: Implement the Main Application

Replace `Program.cs` with this enterprise-ready code:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ScheduledJobApp;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Create host builder with dependency injection
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<JobExecutor>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            // Get the job executor service
            var jobExecutor = host.Services.GetRequiredService<JobExecutor>();

            // Execute the job
            await jobExecutor.ExecuteAsync(args);

            return 0; // Success
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application failed: {ex.Message}");
            return 1; // Error
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

        _logger.LogInformation("=== Job Started ===");
        _logger.LogInformation("Job ID: {JobId}", jobId);
        _logger.LogInformation("Start Time: {StartTime:yyyy-MM-dd HH:mm:ss} UTC", startTime);
        _logger.LogInformation("Arguments: {Args}", string.Join(", ", args));
        _logger.LogInformation("Environment: {Environment}", Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production");

        try
        {
            // Simulate some work
            _logger.LogInformation("Processing started...");
            await Task.Delay(2000); // Simulate 2 seconds of work

            // Simulate business logic with different message types
            _logger.LogInformation("Processing data batch 1/3...");
            await Task.Delay(1000);

            _logger.LogInformation("Processing data batch 2/3...");
            await Task.Delay(1000);

            _logger.LogWarning("Batch 2 had 3 warning items - continuing processing");

            _logger.LogInformation("Processing data batch 3/3...");
            await Task.Delay(1000);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            _logger.LogInformation("=== Job Completed Successfully ===");
            _logger.LogInformation("Job ID: {JobId}", jobId);
            _logger.LogInformation("End Time: {EndTime:yyyy-MM-dd HH:mm:ss} UTC", endTime);
            _logger.LogInformation("Duration: {Duration:hh\\:mm\\:ss}", duration);
            _logger.LogInformation("Status: SUCCESS");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job failed with error: {Error}", ex.Message);
            throw;
        }
    }
}
```

### Step 4: Build and Test Locally

```bash
# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run

# Run with arguments
dotnet run -- --environment Development --batch-size 100
```

### Step 5: Create Production Build

```bash
# Create self-contained executable
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish

# Test the published executable (on Linux/WSL)
./publish/ScheduledJobApp

# On Windows, test with:
# ./publish/ScheduledJobApp.exe
```

## 💡 Tips and Tricks

### Logging Best Practices
- **Use structured logging:** Include JobId for correlation across logs
- **Include timestamps:** Essential for scheduled job troubleshooting
- **Log start/end:** Always log when jobs begin and complete
- **Use appropriate log levels:** Info for normal flow, Warning for recoverable issues, Error for failures

### Error Handling
- **Return proper exit codes:** 0 for success, non-zero for errors
- **Catch all exceptions:** Prevent unhandled exceptions from crashing
- **Log before throwing:** Ensure errors are captured in logs

### Performance Considerations
- **Use async/await:** For I/O operations to improve container efficiency
- **Implement timeouts:** Prevent hanging jobs in production
- **Monitor memory usage:** Important for containerized environments

## ⚠️ Common Pitfalls

### Build Issues
- **Missing SDK:** Ensure .NET 8.0 SDK is installed, not just runtime
- **Wrong target framework:** Must use `net8.0` for container compatibility
- **Package version conflicts:** Use exact versions shown above

### Runtime Issues
- **Console output buffering:** Use `Console.Out.Flush()` if output appears delayed
- **Culture/timezone issues:** Always use UTC times for scheduled jobs
- **Path separators:** Use `Path.Combine()` for cross-platform compatibility

### Dependency Injection
- **Service registration:** Don't forget to register services in `ConfigureServices`
- **Lifetime management:** Use appropriate service lifetimes (Singleton for stateless services)

## ✅ Verification Steps

### 1. Application Runs Successfully
```bash
dotnet run
# Should see structured log output with timestamps
```

### 2. Arguments Are Processed
```bash
dotnet run -- --test-arg value
# Should see arguments in log output
```

### 3. Published Executable Works
```bash
./publish/ScheduledJobApp
# Should run without requiring dotnet CLI
```

### 4. Exit Codes Work Correctly
```bash
./publish/ScheduledJobApp && echo "Success: $?" || echo "Failed: $?"
# Should print "Success: 0"
```

### 5. Log Output Is Structured
Look for these elements in the output:
- Job ID for correlation
- UTC timestamps
- Clear start/end markers
- Structured information logging

## 🎯 Learning Checkpoint

Before proceeding to Phase 2, ensure you can:
- [ ] Build the application without errors
- [ ] Run the executable and see structured logs
- [ ] Understand the dependency injection setup
- [ ] Explain why we use UTC timestamps
- [ ] Identify the key enterprise patterns used (logging, DI, async/await, error handling)

## 📚 Key Concepts Learned

1. **Dependency Injection:** Using Microsoft.Extensions.Hosting for enterprise-grade DI
2. **Structured Logging:** Creating logs that are easy to parse and search
3. **Async Programming:** Using async/await for better resource utilization
4. **Error Handling:** Proper exception handling and exit codes
5. **Production Builds:** Creating self-contained executables for deployment

## Next Steps

Once you've successfully completed this phase, you're ready for **Phase 2: Docker Setup**, where you'll learn to create optimized Docker containers for .NET applications.