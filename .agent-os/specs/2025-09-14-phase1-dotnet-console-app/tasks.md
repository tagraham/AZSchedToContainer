# Spec Tasks

These are the tasks to be completed for the spec detailed in @.agent-os/specs/2025-09-14-phase1-dotnet-console-app/spec.md

> Created: 2025-09-14
> Status: Ready for Implementation

## Tasks

- [x] 1. Set up .NET console application project structure
  - [x] 1.1 Write tests for application initialization and configuration
  - [x] 1.2 Create new .NET 8.0 console application with required NuGet packages
  - [x] 1.3 Configure dependency injection with Microsoft.Extensions.Hosting
  - [x] 1.4 Set up structured logging with console provider
  - [x] 1.5 Implement basic Program.cs with async Main method
  - [x] 1.6 Verify all tests pass

- [x] 2. Implement instance management system
  - [x] 2.1 Write tests for mutex acquisition and release scenarios
  - [x] 2.2 Create InstanceManager service with named mutex implementation
  - [x] 2.3 Implement mutex acquisition logic with immediate check (WaitOne(0))
  - [x] 2.4 Add fallback file-based locking mechanism
  - [x] 2.5 Implement proper resource cleanup in try-finally blocks
  - [x] 2.6 Configure appropriate exit codes for instance conflicts
  - [x] 2.7 Verify all tests pass

- [x] 3. Add configuration and command-line argument handling
  - [x] 3.1 Write tests for command-line argument parsing
  - [x] 3.2 Implement command-line parser for --sleep-seconds parameter
  - [x] 3.3 Add --instance-name and --enable-file-lock options
  - [x] 3.4 Configure environment variable overrides
  - [x] 3.5 Implement configuration validation with error messages
  - [x] 3.6 Verify all tests pass

- [x] 4. Implement job execution with sleep simulation
  - [x] 4.1 Write tests for job executor service
  - [x] 4.2 Create JobExecutor service with configurable sleep
  - [x] 4.3 Add comprehensive logging at all lifecycle points
  - [x] 4.4 Implement graceful shutdown handling (SIGTERM/SIGINT)
  - [x] 4.5 Add timeout protection for sleep operations
  - [x] 4.6 Verify all tests pass

- [x] 5. Final integration and testing
  - [x] 5.1 Write integration tests for complete application flow
  - [x] 5.2 Test multiple instance prevention scenarios
  - [x] 5.3 Validate all command-line arguments work correctly
  - [x] 5.4 Test graceful shutdown and resource cleanup
  - [x] 5.5 Create production build with dotnet publish
  - [x] 5.6 Document usage examples in README
  - [x] 5.7 Verify all tests pass