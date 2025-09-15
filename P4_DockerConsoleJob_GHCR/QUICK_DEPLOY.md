# Quick Deploy Commands - Copy & Paste Ready

Replace these values:
- `YOUR_SUBSCRIPTION_ID` - Your Azure subscription ID
- `YOUR_GITHUB_USERNAME` - Your GitHub username
- `YOUR_GITHUB_TOKEN` - Your GitHub personal access token

## 1. Azure Setup (One Time)

```bash
# Login to Azure
az login

# Set subscription (if you have multiple)
az account set --subscription YOUR_SUBSCRIPTION_ID

# Create resource group
az group create --name rgDCJ --location eastus

# Create Container Apps environment
az containerapp env create \
  --name docker-console-env \
  --resource-group rgDCJ \
  --location eastus
```

## 2. Create Service Principal for GitHub

```bash
# Run this and SAVE THE OUTPUT
az ad sp create-for-rbac \
  --name "GitHub-AZSchedToContainer" \
  --role contributor \
  --scopes /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/rgDCJ \
  --sdk-auth
```

**COPY THE ENTIRE JSON OUTPUT!** You'll add it to GitHub Secrets.

## 3. GitHub Setup

1. Go to: https://github.com/tagraham/AZSchedToContainer/settings/secrets/actions
2. Click "New repository secret"
3. Name: `AZURE_CREDENTIALS`
4. Value: Paste the JSON from step 2
5. Click "Add secret"

## 4. Initial Deployment

```bash
# Navigate to P4 directory
cd P4_DockerConsoleJob_GHCR

# Build image
docker build -t ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest .

# Login to GHCR
echo YOUR_GITHUB_TOKEN | docker login ghcr.io -u tagraham --password-stdin

# Push image
docker push ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest

# Create Container Apps Job
az containerapp job create \
  --name docker-console-job \
  --resource-group rgDCJ \
  --environment docker-console-env \
  --image ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest \
  --trigger-type "Schedule" \
  --cron-expression "0 2 * * *" \
  --parallelism 1 \
  --replica-completion-count 1 \
  --replica-timeout 7200 \
  --cpu 0.5 \
  --memory 1.0Gi \
  --registry-server ghcr.io \
  --registry-username tagraham \
  --registry-password YOUR_GITHUB_TOKEN \
  --env-vars "ASPNETCORE_ENVIRONMENT=Production" "TZ=UTC"
```

## 5. Test the Job

```bash
# Run job manually
az containerapp job start \
  --name docker-console-job \
  --resource-group rgDCJ

# Check status (wait 30 seconds)
az containerapp job execution list \
  --name docker-console-job \
  --resource-group rgDCJ \
  --output table

# View logs
az containerapp logs show \
  --name docker-console-job \
  --resource-group rgDCJ \
  --type ContainerAppConsoleLogs_CL \
  --follow false
```

## 6. Cleanup (When Done)

```bash
# Delete everything
az group delete --name rgDCJ --yes --no-wait
```

## Common Issues & Fixes

### Authentication Error
```bash
# Re-authenticate registry
az containerapp job update \
  --name docker-console-job \
  --resource-group rgDCJ \
  --registry-password YOUR_GITHUB_TOKEN
```

### Job Not Starting
```bash
# Check job details
az containerapp job show \
  --name docker-console-job \
  --resource-group rgDCJ \
  --output yaml
```

### View Execution History
```bash
# Last 5 executions
az containerapp job execution list \
  --name docker-console-job \
  --resource-group rgDCJ \
  --query "[0:5].{Name:name, Start:properties.startTime, Status:properties.status}" \
  --output table
```

## Useful Portal Links

- Container Apps Job: https://portal.azure.com/#resource/subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/rgDCJ/providers/Microsoft.App/jobs/dockerConsoleJob
- Resource Group: https://portal.azure.com/#resource/subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/rgDCJ