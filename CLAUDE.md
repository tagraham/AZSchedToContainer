# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a comprehensive educational tutorial for migrating Windows executables to containerized environments on Azure Container Apps. The project demonstrates the complete journey from traditional scheduled tasks to cloud-native containerized solutions through a practical "hello world" example.

## Common Development Commands

### Phase 1: .NET Application
```bash
# Create new console application
dotnet new console -n ScheduledJobApp
cd ScheduledJobApp

# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run
dotnet run -- --environment Development --batch-size 100

# Create production build
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
```

### Phase 2: Docker Container
```bash
# Build Docker image
docker build -t scheduled-job:latest .

# Run container
docker run --rm scheduled-job:latest

# Use Docker Compose
docker compose up --build
docker compose down
```

### Phase 3: Container Testing
```bash
# Run with volume mounting
docker run --rm -v ./logs:/app/logs scheduled-job:latest

# Run integration tests
./scripts/test-container.sh

# Monitor container health
docker stats scheduled-job-dev
```

### Phase 4: Azure Deployment
```bash
# Azure setup
./azure/setup.sh

# Deploy to Azure Container Apps
az containerapp update --name app-scheduled-job --resource-group rg-scheduled-job-tutorial --image <acr-server>/scheduled-job:latest

# Check logs
az containerapp logs show --name app-scheduled-job --resource-group rg-scheduled-job-tutorial --tail 20
```

## Project Architecture

The project follows a phased approach with clear separation:

1. **ScheduledJobApp/**: Core .NET 8.0 console application with dependency injection, structured logging, and container-aware configuration
2. **Docker Configuration**: Multi-stage Dockerfile with Alpine Linux base for optimized production images
3. **CI/CD Pipeline**: GitHub Actions workflow for automated build, test, and deployment to Azure Container Apps
4. **Azure Resources**: Container Apps Environment, Container Registry, Log Analytics, and Application Insights for full observability

## Key Technical Decisions

- **Base Image**: Alpine Linux for minimal attack surface and smaller image size
- **Multi-stage Builds**: Separate build and runtime environments for security and optimization
- **Non-root User**: Container runs as `appuser` (UID 1001) for security
- **Structured Logging**: Console logging with UTC timestamps and correlation IDs
- **Health Monitoring**: Built-in health checks and resource monitoring
- **Blue-Green Deployment**: Using Container Apps revisions for zero-downtime deployments

## Important Configuration

### Environment Variables
- `DOTNET_ENVIRONMENT`: Set to Development/Staging/Production
- `TZ`: Always UTC for consistent timestamps in scheduled jobs
- `BUILD_VERSION`: Injected during CI/CD for tracking deployments

### Resource Limits
- Development: 0.25 CPU, 256MB memory
- Production: 0.5 CPU, 1GB memory
- Configurable based on workload requirements

### Volume Mounts
- `/app/logs`: Persistent logging directory
- `/app/config`: Configuration files (read-only)
- `/app/data`: Shared data directory for job outputs

## Testing Strategy

Run the test suite at each phase:
1. Unit tests for .NET application logic
2. Container integration tests (`scripts/test-container.sh`)
3. Azure deployment validation (`scripts/test-azure-deployment.sh`)

## Security Considerations

- Container images are scanned with Trivy during CI/CD
- Azure Managed Identity for service authentication
- Secrets stored in GitHub Secrets and Azure Key Vault
- Network isolation using Azure Virtual Networks when needed

## Common Troubleshooting

### Container Won't Start
- Check logs: `docker logs <container-id>`
- Verify non-root user permissions: `docker run --rm scheduled-job:latest whoami`
- Ensure volume mount permissions are correct

### Azure Deployment Issues
- Verify Azure CLI login: `az login`
- Check resource group exists: `az group show --name rg-scheduled-job-tutorial`
- Validate container registry credentials: `az acr login --name <acr-name>`

### Build Failures
- Ensure .NET 8.0 SDK is installed
- Check Docker daemon is running
- Verify network connectivity for package restoration