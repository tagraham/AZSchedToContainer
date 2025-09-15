#!/bin/bash

# P4 Deployment Script
# This deploys the containerized app to Azure Container Apps

echo "🚀 Starting deployment of P4 to Azure..."

# Build the Docker image
echo "📦 Building Docker image..."
docker build -t ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest .

if [ $? -ne 0 ]; then
    echo "❌ Docker build failed"
    exit 1
fi

# Push to GitHub Container Registry
echo "⬆️ Pushing to GitHub Container Registry..."
docker push ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest

if [ $? -ne 0 ]; then
    echo "❌ Docker push failed. Make sure you're logged in: docker login ghcr.io"
    exit 1
fi

# Update Azure Container Apps Job
echo "☁️ Updating Azure Container Apps Job..."
az containerapp job update \
    --name docker-console-job \
    --resource-group rgDCJ \
    --image ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest \
    --output none

if [ $? -ne 0 ]; then
    echo "❌ Azure update failed. Make sure you're logged in: az login"
    exit 1
fi

echo "✅ Deployment complete!"
echo ""
echo "Test your deployment:"
echo "  az containerapp job start --name docker-console-job --resource-group rgDCJ"
echo ""
echo "Check execution:"
echo "  az containerapp job execution list --name docker-console-job --resource-group rgDCJ --output table"