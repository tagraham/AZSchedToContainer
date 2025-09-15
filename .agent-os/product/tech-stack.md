# Technical Stack - Containerization Tutorial

> Last Updated: 2025-09-14
> Version: 1.0.0

## Application Framework

- **Framework:** .NET 8.0
- **Version:** 8.0 LTS
- **Project Type:** Console Application
- **Target Runtime:** linux-x64

### Why .NET 8.0?
- Long-term support (LTS) version
- Excellent container performance
- Native Linux support
- Rich ecosystem for enterprise applications

## Containerization

- **Container Runtime:** Docker
- **Base Image:** mcr.microsoft.com/dotnet/runtime:8.0-alpine
- **Build Image:** mcr.microsoft.com/dotnet/sdk:8.0-alpine
- **Container Orchestration:** Azure Container Apps

### Why Docker + Alpine?
- Minimal attack surface
- Smaller image sizes
- Better performance
- Industry standard for .NET containerization

## Cloud Platform

- **Primary Platform:** Microsoft Azure
- **Container Service:** Azure Container Apps
- **Container Registry:** Azure Container Registry (ACR)
- **Resource Management:** Azure Resource Manager (ARM)

### Why Azure Container Apps?
- Serverless container hosting
- Built-in scaling and load balancing
- Integrated with Azure ecosystem
- Cost-effective for scheduled workloads

## CI/CD Pipeline

- **Version Control:** Git (GitHub)
- **CI/CD Platform:** GitHub Actions
- **Workflow Triggers:** Push to main, pull requests
- **Deployment Strategy:** Blue-green via Container Apps revisions

### Why GitHub Actions?
- Native integration with GitHub
- Free for public repositories
- Extensive marketplace of actions
- Azure-specific actions available

## Development Tools

- **IDE:** Visual Studio Code (recommended)
- **CLI Tools:**
  - .NET CLI
  - Docker CLI
  - Azure CLI
  - GitHub CLI (optional)

## Monitoring and Logging

- **Application Logging:** Microsoft.Extensions.Logging
- **Container Logging:** Container Apps built-in logging
- **Monitoring:** Azure Monitor
- **Log Analytics:** Azure Log Analytics Workspace

## Security

- **Container Security:**
  - Non-root user execution
  - Minimal base images
  - Security scanning via GitHub
- **Azure Security:**
  - Managed identities
  - Azure Key Vault integration
  - Network security groups

## Local Development

- **Container Testing:** Docker Desktop
- **Local Registry:** Docker Hub (for testing)
- **Development OS:** Cross-platform (Windows, macOS, Linux)

## Dependencies

### .NET Packages
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Configuration

### Development Dependencies
- Docker Desktop
- .NET 8.0 SDK
- Azure CLI
- Git

## Architecture Patterns

- **Application Pattern:** Console application with dependency injection
- **Container Pattern:** Multi-stage Docker builds
- **Deployment Pattern:** GitOps with GitHub Actions
- **Monitoring Pattern:** Structured logging with correlation IDs