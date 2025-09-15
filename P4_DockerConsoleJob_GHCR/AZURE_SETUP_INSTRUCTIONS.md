# Azure Container Apps Setup Instructions

## Phase 4: Production Deployment with GitHub Container Registry (GHCR)

### Prerequisites
- Azure CLI installed locally
- GitHub repository (already have: https://github.com/tagraham/AZSchedToContainer)
- Azure subscription

---

## Step 1: Azure Infrastructure Setup

### 1.1 Login to Azure
```bash
az login
```

### 1.2 Create Resource Group
```bash
az group create \
  --name rgDCJ \
  --location eastus
```

### 1.3 Create Container Apps Environment
```bash
az containerapp env create \
  --name dockerConsoleJobEnv \
  --resource-group rgDCJ \
  --location eastus
```

### 1.4 Verify Environment
```bash
az containerapp env show \
  --name dockerConsoleJobEnv \
  --resource-group rgDCJ
```

---

## Step 2: GitHub Setup

### 2.1 Enable GitHub Container Registry
1. Go to your GitHub profile → Settings → Developer settings → Personal access tokens
2. Click "Tokens (classic)" → "Generate new token (classic)"
3. Name: "GHCR Access for Azure"
4. Select scopes:
   - `write:packages` - Upload packages to GitHub Package Registry
   - `read:packages` - Download packages from GitHub Package Registry
   - `delete:packages` - Delete packages from GitHub Package Registry (optional)
5. Click "Generate token" and save it securely

### 2.2 Test GHCR Access Locally
```bash
# Login to GitHub Container Registry
echo YOUR_GITHUB_TOKEN | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin

# Tag your image
docker tag scheduled-job:latest ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest

# Push to GHCR
docker push ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest
```

---

## Step 3: Create Azure Service Principal

### 3.1 Create Service Principal for GitHub Actions
```bash
# Create service principal and save output
az ad sp create-for-rbac \
  --name "GitHub-AZSchedToContainer" \
  --role contributor \
  --scopes /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/rgDCJ \
  --sdk-auth
```

### 3.2 Save the Output
The command outputs JSON like this:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

**SAVE THIS ENTIRE JSON OUTPUT - YOU'LL NEED IT!**

---

## Step 4: Configure GitHub Repository Secrets

### 4.1 Add Azure Credentials Secret
1. Go to https://github.com/tagraham/AZSchedToContainer
2. Click Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Name: `AZURE_CREDENTIALS`
5. Value: Paste the ENTIRE JSON output from Step 3.1
6. Click "Add secret"

### 4.2 Verify GitHub Token
The `GITHUB_TOKEN` is automatically available in GitHub Actions - no setup needed!

---

## Step 5: Initial Manual Deployment (First Time Only)

### 5.1 Build and Push Image Manually
```bash
cd P4_DockerConsoleJob_GHCR

# Build the image
docker build -t ghcr.io/tagraham/azschedtocontainer/scheduled-job:initial .

# Push to GHCR
docker push ghcr.io/tagraham/azschedtocontainer/scheduled-job:initial
```

### 5.2 Create Initial Container Apps Job
```bash
az containerapp job create \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --environment dockerConsoleJobEnv \
  --image ghcr.io/tagraham/azschedtocontainer/scheduled-job:initial \
  --trigger-type "Schedule" \
  --cron-expression "0 2 * * *" \
  --parallelism 1 \
  --replica-completion-count 1 \
  --replica-timeout 7200 \
  --cpu 0.5 \
  --memory 1.0Gi \
  --registry-server ghcr.io \
  --registry-username YOUR_GITHUB_USERNAME \
  --registry-password YOUR_GITHUB_TOKEN \
  --env-vars "ASPNETCORE_ENVIRONMENT=Production" "TZ=UTC"
```

### 5.3 Run Job Manually (Test)
```bash
az containerapp job start \
  --name dockerConsoleJob \
  --resource-group rgDCJ
```

### 5.4 Check Job Execution
```bash
# List executions
az containerapp job execution list \
  --name dockerConsoleJob \
  --resource-group rgDCJ

# Show specific execution logs
az containerapp job execution show \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --execution-name JOB_EXECUTION_NAME
```

---

## Step 6: Enable Auto-Deploy on Push

### 6.1 Push the Workflow File to GitHub
```bash
# First, commit and push P4 with the workflow file
git add P4_DockerConsoleJob_GHCR/.github
git commit -m "Add GitHub Actions workflow for auto-deploy"
git push origin main
```

### 6.2 Verify GitHub Actions is Enabled
1. Go to https://github.com/tagraham/AZSchedToContainer
2. Click "Actions" tab
3. You should see "Build and Deploy to Azure Container Apps" workflow

### 6.3 Auto-Deploy Triggers
The workflow will automatically run when:
- You push changes to `P4_DockerConsoleJob_GHCR/**` files
- You manually trigger it from GitHub Actions tab

### 6.4 Update Job After Deploy
Since the workflow creates/updates the container image, you need to update the job to use the new image:
```bash
# After GitHub Actions completes, update the job to use latest image
az containerapp job update \
  --name docker-console-job \
  --resource-group rgDCJ \
  --image ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest
```

### 6.5 Monitor Deployment
Watch the workflow progress in GitHub Actions

### 6.6 Verify in Azure
```bash
# Check job status
az containerapp job show \
  --name docker-console-job \
  --resource-group rgDCJ \
  --query "properties.provisioningState"

# Check latest execution
az containerapp job execution list \
  --name docker-console-job \
  --resource-group rgDCJ \
  --query "[0]"
```

---

## Step 7: Configure Production Schedule

### 7.1 Update Cron Expression
```bash
# Daily at 2 AM UTC
az containerapp job update \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --cron-expression "0 2 * * *"

# Every 6 hours
az containerapp job update \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --cron-expression "0 */6 * * *"

# Weekly on Mondays at 3 AM
az containerapp job update \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --cron-expression "0 3 * * 1"
```

### 7.2 Configure Job Parameters
```bash
az containerapp job update \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --args "--sleep-seconds" "600" "--enable-file-lock"
```

---

## Step 8: Monitoring and Logs

### 8.1 Enable Log Analytics
```bash
# Get environment details
az containerapp env show \
  --name dockerConsoleJobEnv \
  --resource-group rgDCJ \
  --query "properties.appLogsConfiguration"
```

### 8.2 Query Logs
```bash
# Stream logs
az containerapp job logs show \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --follow
```

### 8.3 Azure Portal Monitoring
1. Navigate to Azure Portal
2. Go to Resource Group: rgDCJ
3. Click on dockerConsoleJob
4. View "Execution history" for job runs
5. Click "Logs" for detailed execution logs

---

## Troubleshooting

### Issue: Authentication Failed
```bash
# Re-authenticate GitHub Container Registry
az containerapp job update \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --registry-server ghcr.io \
  --registry-username YOUR_GITHUB_USERNAME \
  --registry-password YOUR_GITHUB_TOKEN
```

### Issue: Job Not Running
```bash
# Check job configuration
az containerapp job show \
  --name dockerConsoleJob \
  --resource-group rgDCJ

# Manually trigger
az containerapp job start \
  --name dockerConsoleJob \
  --resource-group rgDCJ
```

### Issue: Out of Memory
```bash
# Increase memory allocation
az containerapp job update \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --memory 2.0Gi \
  --cpu 1.0
```

---

## Clean Up (When Done Testing)

### Delete Resources
```bash
# Delete Container App Job
az containerapp job delete \
  --name dockerConsoleJob \
  --resource-group rgDCJ \
  --yes

# Delete Environment
az containerapp env delete \
  --name dockerConsoleJobEnv \
  --resource-group rgDCJ \
  --yes

# Delete Resource Group
az group delete \
  --name rgDCJ \
  --yes
```

---

## Cost Estimate

### Azure Container Apps Job Pricing (East US)
- **vCPU**: $0.000013/second = ~$0.047/hour
- **Memory**: $0.0000033/GB/second = ~$0.012/GB/hour
- **Requests**: First 2 million free

### Example Monthly Cost:
- Job runs daily for 30 minutes
- 0.5 vCPU, 1GB memory
- **Monthly cost**: ~$0.90

### Compared to:
- **VM (B2s)**: ~$30/month (running 24/7)
- **Azure Functions Premium**: ~$180/month (minimum plan)

**Savings: 97% reduction in compute costs!**

---

## Next Steps

1. **Add Managed Identity** for Azure resource access
2. **Configure Azure Key Vault** for secrets
3. **Set up Application Insights** for advanced monitoring
4. **Implement retry policies** for resilience
5. **Add health checks** for better observability

---

## Support Resources

- [Azure Container Apps Jobs Documentation](https://learn.microsoft.com/en-us/azure/container-apps/jobs)
- [GitHub Container Registry Docs](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
- [GitHub Actions for Azure](https://github.com/Azure/actions)