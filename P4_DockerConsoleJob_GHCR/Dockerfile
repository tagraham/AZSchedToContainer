# Multi-stage build for optimized production images
# Stage 1: Build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project file and restore dependencies (better layer caching)
COPY ScheduledJobApp.csproj .
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet publish -c Release -o /app

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