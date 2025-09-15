# Phase 4: Production Deployment with GitHub Container Registry

This phase demonstrates production deployment using GitHub Actions, GitHub Container Registry (GHCR), and Azure Container Apps.

## What's New in P4

- **GitHub Actions Workflow** - Automated CI/CD pipeline
- **GitHub Container Registry** - Free private container storage
- **Azure Container Apps Jobs** - Serverless scheduled execution
- **Production Configuration** - Environment variables, secrets, monitoring

## Deployment Options

### Option 1: Manual Deployment (Simple & Direct)
```bash
cd P4_DockerConsoleJob_GHCR

# Build and push
docker build -t ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest .
docker push ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest

# Update Azure
az containerapp job update \
  --name docker-console-job \
  --resource-group rgDCJ \
  --image ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest
```

### Option 2: Use Deploy Script
```bash
cd P4_DockerConsoleJob_GHCR
./deploy.sh
```

### Option 3: GitHub Actions Auto-Deploy
**Note**: GitHub Actions workflows MUST be in `.github/workflows/` at repository ROOT.
- The dispatcher workflow at `/.github/workflows/p4-deploy-dispatcher.yml` watches this folder
- Any push to P4 folder triggers automatic build and push to GHCR
- You still need to run the Azure update command after

## Quick Start

### 1. Follow Azure Setup
See `AZURE_SETUP_INSTRUCTIONS.md` for complete step-by-step guide.

### 2. Key Commands
```bash
# Create Azure resources
az group create --name rgDCJ --location eastus
az containerapp env create --name dockerConsoleJobEnv --resource-group rgDCJ --location eastus

# Deploy manually (first time)
docker build -t ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest .
docker push ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest

# Create Container Apps Job
az containerapp job create \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --environment dockerConsoleJobEnv \
  --image ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest \
  --trigger-type "Schedule" \
  --cron-expression "0 2 * * *" \
  --parallelism 1 \
  --replica-completion-count 1
```

## Architecture

```
GitHub Repo → GitHub Actions → GHCR → Azure Container Apps Job
     ↓              ↓            ↓              ↓
  [Code]      [Build&Test]   [Store]      [Schedule&Run]
```

## Key Benefits

1. **Zero Infrastructure Management** - Serverless execution
2. **Cost Optimization** - Pay only when job runs (~$0.90/month)
3. **Built-in Scheduling** - Cron expressions, no external scheduler
4. **Automatic Single Instance** - Platform guarantees no duplicates
5. **Private Container Registry** - Free with GitHub

## Files Structure

```
P4_DockerConsoleJob_GHCR/
├── .github/
│   └── workflows/
│       └── deploy-to-azure.yml    # CI/CD pipeline
├── AZURE_SETUP_INSTRUCTIONS.md    # Step-by-step guide
├── Dockerfile                      # Container definition
├── Program.cs                      # Application code
├── docker-compose.yml              # Local testing
└── README.md                       # This file
```

## Monitoring

```bash
# View job executions
az containerapp job execution list \
  --name dockerConsoleJob \
  --resource-group rgDCJ

# Stream logs
az containerapp job logs show \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --follow
```

## Cost Comparison

| Solution | Monthly Cost | Notes |
|----------|-------------|-------|
| Windows VM | $30-100 | Running 24/7 |
| Azure Functions | $180+ | Premium plan required |
| Container Apps Job | $0.90 | Runs 30 min/day |

**97% cost reduction!**

## Next Steps

- Add Azure Key Vault for secrets
- Configure Application Insights
- Set up alerts for failures
- Implement retry policies

## Deployment Status
- Last tested: Successfully deployed to Azure Container Apps
- Auto-build: Enabled via GitHub Actions