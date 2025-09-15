# Phase 2: Docker Container Setup

> **Learning Time:** 3-4 hours
> **Skill Level:** Beginner to Intermediate
> **Prerequisites:** Phase 1 completed, Docker Desktop installed

## Learning Objectives

By the end of this phase, you will:
- Understand Docker fundamentals for .NET applications
- Create optimized multi-stage Dockerfiles
- Implement container security best practices
- Build and test containers locally
- Use Docker Compose for local development

## Step-by-Step Instructions

### Step 1: Create Optimized Dockerfile

Create a `Dockerfile` in your project root (same directory as `.csproj`):

```dockerfile
# Multi-stage build for optimized production images
# Stage 1: Build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project file and restore dependencies (for better caching)
COPY ScheduledJobApp.csproj .
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# Stage 2: Runtime environment
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime

# Create non-root user for security
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Set working directory and create logs directory
WORKDIR /app
RUN mkdir -p /app/logs && chown -R appuser:appgroup /app

# Copy built application from build stage
COPY --from=build /app .

# Switch to non-root user
USER appuser

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=""

# Configure health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD ps aux | grep -v grep | grep ScheduledJobApp || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "ScheduledJobApp.dll"]
```

### Step 2: Create .dockerignore

Create `.dockerignore` to optimize build context:

```dockerignore
# Git
.git
.gitignore
.gitattributes

# Documentation
README.md
*.md

# Visual Studio / VS Code
.vs/
.vscode/
*.user
*.suo
*.cache

# Build outputs
bin/
obj/
publish/
out/

# Dependencies
packages/
node_modules/

# OS files
.DS_Store
Thumbs.db

# Logs
logs/
*.log

# Test results
TestResults/
*.trx

# Docker
.dockerignore
Dockerfile*
docker-compose*.yml
```

### Step 3: Create Docker Compose for Development

Create `docker-compose.yml` for local testing:

```yaml
version: '3.8'

services:
  scheduled-job:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: scheduled-job-dev
    environment:
      - DOTNET_ENVIRONMENT=Development
      - TZ=UTC
    volumes:
      # Mount logs directory for local development
      - ./logs:/app/logs
    restart: unless-stopped
    # Override command for development (run once and exit)
    # Remove this line to run as daemon
    command: ["dotnet", "ScheduledJobApp.dll", "--environment", "Development"]

  # Optional: Add monitoring
  portainer:
    image: portainer/portainer-ce:alpine
    container_name: portainer
    restart: unless-stopped
    ports:
      - "9000:9000"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - portainer_data:/data
    profiles:
      - monitoring

volumes:
  portainer_data:
```

### Step 4: Build and Test Container

```bash
# Build the Docker image
docker build -t scheduled-job:latest .

# Run container once to test
docker run --rm scheduled-job:latest

# Run with custom arguments
docker run --rm scheduled-job:latest --batch-size 50

# Run interactively to debug
docker run --rm -it scheduled-job:latest sh

# Check container details
docker image inspect scheduled-job:latest
```

### Step 5: Use Docker Compose for Development

```bash
# Build and run with docker-compose
docker-compose up --build

# Run in background
docker-compose up -d

# View logs
docker-compose logs -f scheduled-job

# Stop containers
docker-compose down

# Run with monitoring tools
docker-compose --profile monitoring up -d
```

### Step 6: Container Security Hardening

Create `docker-security.md` for documentation:

```bash
# Security scan (if Docker Scout is available)
docker scout cves scheduled-job:latest

# Check for vulnerabilities
docker scan scheduled-job:latest

# Verify non-root user
docker run --rm scheduled-job:latest whoami
# Should output: appuser

# Check file permissions
docker run --rm scheduled-job:latest ls -la /app
```

## 💡 Tips and Tricks

### Multi-Stage Build Benefits
- **Smaller images:** Only runtime dependencies in final image
- **Better security:** No build tools in production image
- **Faster deployments:** Smaller images transfer faster
- **Layer caching:** Dependencies cached separately from source code

### Alpine Linux Advantages
- **Security:** Minimal attack surface
- **Size:** ~5MB base image vs ~100MB+ for Ubuntu
- **Performance:** Faster container startup
- **Memory:** Lower memory footprint

### Dockerfile Optimization
- **Layer ordering:** Put least-changing instructions first
- **Dependency caching:** Copy `.csproj` before source code
- **Multi-line commands:** Use `&&` to reduce layers
- **Clean up:** Remove unnecessary files in same layer

### Security Best Practices
- **Non-root user:** Never run as root in production
- **Specific base images:** Use exact versions, not `latest`
- **Minimal packages:** Only install required dependencies
- **Regular updates:** Keep base images updated

## ⚠️ Common Pitfalls

### Build Issues
- **Context size:** Large build context slows builds
- **Wrong paths:** Ensure Dockerfile is in correct directory
- **Case sensitivity:** Linux containers are case-sensitive
- **Line endings:** Use LF line endings, not CRLF

### Runtime Issues
- **File permissions:** Non-root user needs read/write access
- **Missing dependencies:** Runtime image missing required packages
- **Time zones:** Containers default to UTC
- **Signal handling:** Ensure graceful shutdown handling

### Performance Issues
- **Layer optimization:** Too many layers slow builds
- **Unnecessary rebuilds:** Poor caching invalidates layers
- **Large images:** Including unnecessary files
- **Memory limits:** Not setting appropriate resource constraints

### Development Workflow
- **Volume mounts:** Development files overriding container files
- **Port conflicts:** Multiple containers using same ports
- **Network isolation:** Containers can't communicate without proper networking

## ✅ Verification Steps

### 1. Container Builds Successfully
```bash
docker build -t scheduled-job:latest .
# Should complete without errors
```

### 2. Container Runs and Exits Cleanly
```bash
docker run --rm scheduled-job:latest
# Should see log output and exit with code 0
```

### 3. Security Configuration Works
```bash
docker run --rm scheduled-job:latest whoami
# Should output: appuser (not root)
```

### 4. Multi-stage Build Optimized
```bash
docker images | grep scheduled-job
# Image should be under 100MB
```

### 5. Docker Compose Works
```bash
docker-compose up --build
# Should build and run without errors
```

### 6. Health Check Functions
```bash
docker run -d --name test-job scheduled-job:latest sleep 300
docker ps
# Should show healthy status after 60 seconds
docker rm -f test-job
```

## 🔍 Troubleshooting Guide

### Build Failures

**Problem:** `COPY failed: file not found`
**Solution:** Check file paths are relative to build context (directory with Dockerfile)

**Problem:** `Unable to resolve dotnet restore`
**Solution:** Ensure `.csproj` file is in correct location and network connectivity

**Problem:** `Permission denied`
**Solution:** Check Docker daemon is running and user has permissions

### Runtime Failures

**Problem:** Container exits immediately
**Solution:** Check logs with `docker logs <container>` for error details

**Problem:** Application can't write logs
**Solution:** Ensure non-root user has write permissions to log directory

**Problem:** Health check failing
**Solution:** Verify application process name matches health check command

### Performance Issues

**Problem:** Slow builds
**Solution:** Optimize .dockerignore and use multi-stage builds

**Problem:** Large image size
**Solution:** Use Alpine base images and minimize installed packages

## 🎯 Learning Checkpoint

Before proceeding to Phase 3, ensure you can:
- [ ] Build Docker images successfully
- [ ] Understand multi-stage build benefits
- [ ] Explain container security practices
- [ ] Use Docker Compose for local development
- [ ] Troubleshoot common container issues

## 📚 Key Concepts Learned

1. **Multi-stage Builds:** Separating build and runtime environments
2. **Container Security:** Non-root users and minimal base images
3. **Image Optimization:** Layer caching and size reduction
4. **Local Development:** Docker Compose workflows
5. **Health Checks:** Container monitoring and lifecycle management

## Advanced Concepts (Optional)

### Build Arguments and Secrets
```dockerfile
# Build-time variables
ARG BUILD_VERSION=1.0.0
ENV VERSION=$BUILD_VERSION

# For sensitive data (requires BuildKit)
# RUN --mount=type=secret,id=mypassword cat /run/secrets/mypassword
```

### Custom Health Checks
```dockerfile
# Application-specific health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
```

### Resource Constraints
```yaml
# In docker-compose.yml
services:
  scheduled-job:
    deploy:
      resources:
        limits:
          memory: 128M
          cpus: '0.5'
```

## Next Steps

Once you've successfully completed this phase, you're ready for **Phase 3: Containerized Execution**, where you'll integrate your application with the Docker container and handle advanced scenarios like volume mounting and logging.