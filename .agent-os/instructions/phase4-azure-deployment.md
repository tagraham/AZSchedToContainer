# Phase 4: Azure Container Apps Deployment

> **Learning Time:** 4-5 hours
> **Skill Level:** Intermediate to Advanced
> **Prerequisites:** Phase 1-3 completed, Azure subscription, GitHub account

## Learning Objectives

By the end of this phase, you will:
- Deploy containerized applications to Azure Container Apps
- Implement CI/CD pipelines with GitHub Actions
- Configure Azure security and networking
- Set up monitoring and logging in Azure
- Manage production deployments and rollbacks

## Step-by-Step Instructions

### Step 1: Azure Environment Setup

#### Install Required Tools
```bash
# Install Azure CLI (if not already installed)
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Install GitHub CLI (optional but recommended)
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
sudo chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg

# Login to Azure
az login

# Login to GitHub (if using gh CLI)
gh auth login
```

#### Create Azure Resources
Create `azure/setup.sh`:

```bash
#!/bin/bash
set -e

# Configuration
RESOURCE_GROUP="rg-scheduled-job-tutorial"
LOCATION="eastus"
ACR_NAME="acrscheduledjob$(date +%s)"  # Must be globally unique
CONTAINER_APP_ENV="env-scheduled-job"
CONTAINER_APP_NAME="app-scheduled-job"
LOG_ANALYTICS_WORKSPACE="logs-scheduled-job"

echo "=== Creating Azure Resources ==="

# Create resource group
echo "Creating resource group..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create Log Analytics workspace
echo "Creating Log Analytics workspace..."
az monitor log-analytics workspace create \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $LOG_ANALYTICS_WORKSPACE \
  --location $LOCATION

# Get Log Analytics workspace ID and key
LOG_ANALYTICS_WORKSPACE_ID=$(az monitor log-analytics workspace show \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $LOG_ANALYTICS_WORKSPACE \
  --query customerId -o tsv)

LOG_ANALYTICS_KEY=$(az monitor log-analytics workspace get-shared-keys \
  --resource-group $RESOURCE_GROUP \
  --workspace-name $LOG_ANALYTICS_WORKSPACE \
  --query primarySharedKey -o tsv)

# Create Container Registry
echo "Creating Azure Container Registry..."
az acr create \
  --resource-group $RESOURCE_GROUP \
  --name $ACR_NAME \
  --sku Basic \
  --admin-enabled true

# Create Container Apps environment
echo "Creating Container Apps environment..."
az containerapp env create \
  --name $CONTAINER_APP_ENV \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --logs-workspace-id $LOG_ANALYTICS_WORKSPACE_ID \
  --logs-workspace-key $LOG_ANALYTICS_KEY

echo "=== Azure Resources Created Successfully ==="
echo "Resource Group: $RESOURCE_GROUP"
echo "Container Registry: $ACR_NAME"
echo "Container App Environment: $CONTAINER_APP_ENV"
echo "Log Analytics Workspace: $LOG_ANALYTICS_WORKSPACE"

# Save configuration for later use
cat > azure-config.env << EOF
RESOURCE_GROUP=$RESOURCE_GROUP
LOCATION=$LOCATION
ACR_NAME=$ACR_NAME
CONTAINER_APP_ENV=$CONTAINER_APP_ENV
CONTAINER_APP_NAME=$CONTAINER_APP_NAME
LOG_ANALYTICS_WORKSPACE=$LOG_ANALYTICS_WORKSPACE
EOF

echo "Configuration saved to azure-config.env"
```

Run the setup script:
```bash
chmod +x azure/setup.sh
./azure/setup.sh
```

### Step 2: GitHub Repository Setup

#### Initialize Git Repository
```bash
# Initialize git if not already done
git init
git add .
git commit -m "Initial commit - containerized scheduled job"

# Create GitHub repository (using gh CLI)
gh repo create scheduled-job-tutorial --public --push

# Or manually create on GitHub and add remote
# git remote add origin https://github.com/yourusername/scheduled-job-tutorial.git
# git push -u origin main
```

#### Configure GitHub Secrets
```bash
# Source the Azure configuration
source azure-config.env

# Get Azure Container Registry credentials
ACR_SERVER=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query loginServer -o tsv)
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query username -o tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query passwords[0].value -o tsv)

# Create service principal for GitHub Actions
SP_JSON=$(az ad sp create-for-rbac \
  --name "sp-scheduled-job-github" \
  --role contributor \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth)

echo "Add these secrets to your GitHub repository:"
echo "AZURE_CREDENTIALS: $SP_JSON"
echo "ACR_SERVER: $ACR_SERVER"
echo "ACR_USERNAME: $ACR_USERNAME"
echo "ACR_PASSWORD: $ACR_PASSWORD"
echo "RESOURCE_GROUP: $RESOURCE_GROUP"
echo "CONTAINER_APP_ENV: $CONTAINER_APP_ENV"
echo "CONTAINER_APP_NAME: $CONTAINER_APP_NAME"

# Using GitHub CLI to set secrets (optional)
gh secret set AZURE_CREDENTIALS --body "$SP_JSON"
gh secret set ACR_SERVER --body "$ACR_SERVER"
gh secret set ACR_USERNAME --body "$ACR_USERNAME"
gh secret set ACR_PASSWORD --body "$ACR_PASSWORD"
gh secret set RESOURCE_GROUP --body "$RESOURCE_GROUP"
gh secret set CONTAINER_APP_ENV --body "$CONTAINER_APP_ENV"
gh secret set CONTAINER_APP_NAME --body "$CONTAINER_APP_NAME"
```

### Step 3: GitHub Actions CI/CD Pipeline

Create `.github/workflows/azure-deploy.yml`:

```yaml
name: Build and Deploy to Azure Container Apps

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]
  workflow_dispatch:

env:
  IMAGE_NAME: scheduled-job
  IMAGE_TAG: ${{ github.sha }}

jobs:
  build:
    runs-on: ubuntu-latest
    outputs:
      image-uri: ${{ steps.build.outputs.image-uri }}

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v3

    - name: Login to Azure Container Registry
      uses: docker/login-action@v3
      with:
        registry: ${{ secrets.ACR_SERVER }}
        username: ${{ secrets.ACR_USERNAME }}
        password: ${{ secrets.ACR_PASSWORD }}

    - name: Build and push Docker image
      id: build
      uses: docker/build-push-action@v5
      with:
        context: .
        push: true
        tags: |
          ${{ secrets.ACR_SERVER }}/${{ env.IMAGE_NAME }}:${{ env.IMAGE_TAG }}
          ${{ secrets.ACR_SERVER }}/${{ env.IMAGE_NAME }}:latest
        cache-from: type=gha
        cache-to: type=gha,mode=max
        build-args: |
          BUILD_VERSION=${{ github.run_number }}

    - name: Set image URI output
      run: echo "image-uri=${{ secrets.ACR_SERVER }}/${{ env.IMAGE_NAME }}:${{ env.IMAGE_TAG }}" >> $GITHUB_OUTPUT

  security-scan:
    runs-on: ubuntu-latest
    needs: build
    if: github.event_name == 'pull_request'

    steps:
    - name: Login to Azure Container Registry
      uses: docker/login-action@v3
      with:
        registry: ${{ secrets.ACR_SERVER }}
        username: ${{ secrets.ACR_USERNAME }}
        password: ${{ secrets.ACR_PASSWORD }}

    - name: Run security scan
      uses: aquasecurity/trivy-action@master
      with:
        image-ref: ${{ needs.build.outputs.image-uri }}
        format: 'sarif'
        output: 'trivy-results.sarif'

    - name: Upload Trivy scan results
      uses: github/codeql-action/upload-sarif@v3
      with:
        sarif_file: 'trivy-results.sarif'

  deploy-staging:
    runs-on: ubuntu-latest
    needs: build
    if: github.ref == 'refs/heads/main'
    environment: staging

    steps:
    - name: Login to Azure
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Deploy to Container Apps (Staging)
      run: |
        # Check if container app exists
        if az containerapp show --name ${{ secrets.CONTAINER_APP_NAME }}-staging --resource-group ${{ secrets.RESOURCE_GROUP }} > /dev/null 2>&1; then
          echo "Updating existing container app..."
          az containerapp update \
            --name ${{ secrets.CONTAINER_APP_NAME }}-staging \
            --resource-group ${{ secrets.RESOURCE_GROUP }} \
            --image ${{ needs.build.outputs.image-uri }} \
            --set-env-vars \
              DOTNET_ENVIRONMENT=Staging \
              BUILD_VERSION=${{ github.run_number }}
        else
          echo "Creating new container app..."
          az containerapp create \
            --name ${{ secrets.CONTAINER_APP_NAME }}-staging \
            --resource-group ${{ secrets.RESOURCE_GROUP }} \
            --environment ${{ secrets.CONTAINER_APP_ENV }} \
            --image ${{ needs.build.outputs.image-uri }} \
            --registry-server ${{ secrets.ACR_SERVER }} \
            --registry-username ${{ secrets.ACR_USERNAME }} \
            --registry-password ${{ secrets.ACR_PASSWORD }} \
            --cpu 0.25 \
            --memory 0.5Gi \
            --min-replicas 0 \
            --max-replicas 1 \
            --env-vars \
              DOTNET_ENVIRONMENT=Staging \
              BUILD_VERSION=${{ github.run_number }} \
            --ingress external \
            --target-port 8080
        fi

    - name: Get staging URL
      id: staging-url
      run: |
        STAGING_URL=$(az containerapp show \
          --name ${{ secrets.CONTAINER_APP_NAME }}-staging \
          --resource-group ${{ secrets.RESOURCE_GROUP }} \
          --query properties.configuration.ingress.fqdn -o tsv)
        echo "url=https://$STAGING_URL" >> $GITHUB_OUTPUT

    - name: Test staging deployment
      run: |
        # Wait for deployment to be ready
        sleep 30

        # Basic health check (if applicable)
        echo "Staging deployment URL: ${{ steps.staging-url.outputs.url }}"

  deploy-production:
    runs-on: ubuntu-latest
    needs: [build, deploy-staging]
    if: github.ref == 'refs/heads/main'
    environment: production

    steps:
    - name: Login to Azure
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Deploy to Container Apps (Production)
      run: |
        # Blue-green deployment using revisions
        REVISION_SUFFIX=$(date +%Y%m%d-%H%M%S)

        if az containerapp show --name ${{ secrets.CONTAINER_APP_NAME }} --resource-group ${{ secrets.RESOURCE_GROUP }} > /dev/null 2>&1; then
          echo "Updating existing container app with new revision..."
          az containerapp update \
            --name ${{ secrets.CONTAINER_APP_NAME }} \
            --resource-group ${{ secrets.RESOURCE_GROUP }} \
            --image ${{ needs.build.outputs.image-uri }} \
            --revision-suffix $REVISION_SUFFIX \
            --set-env-vars \
              DOTNET_ENVIRONMENT=Production \
              BUILD_VERSION=${{ github.run_number }}
        else
          echo "Creating new container app..."
          az containerapp create \
            --name ${{ secrets.CONTAINER_APP_NAME }} \
            --resource-group ${{ secrets.RESOURCE_GROUP }} \
            --environment ${{ secrets.CONTAINER_APP_ENV }} \
            --image ${{ needs.build.outputs.image-uri }} \
            --registry-server ${{ secrets.ACR_SERVER }} \
            --registry-username ${{ secrets.ACR_USERNAME }} \
            --registry-password ${{ secrets.ACR_PASSWORD }} \
            --cpu 0.5 \
            --memory 1Gi \
            --min-replicas 1 \
            --max-replicas 3 \
            --env-vars \
              DOTNET_ENVIRONMENT=Production \
              BUILD_VERSION=${{ github.run_number }} \
            --revision-suffix $REVISION_SUFFIX
        fi

    - name: Get production URL
      id: production-url
      run: |
        PRODUCTION_URL=$(az containerapp show \
          --name ${{ secrets.CONTAINER_APP_NAME }} \
          --resource-group ${{ secrets.RESOURCE_GROUP }} \
          --query properties.configuration.ingress.fqdn -o tsv)
        echo "url=https://$PRODUCTION_URL" >> $GITHUB_OUTPUT

    - name: Create deployment summary
      run: |
        echo "## 🚀 Deployment Summary" >> $GITHUB_STEP_SUMMARY
        echo "" >> $GITHUB_STEP_SUMMARY
        echo "**Image:** \`${{ needs.build.outputs.image-uri }}\`" >> $GITHUB_STEP_SUMMARY
        echo "**Build Number:** ${{ github.run_number }}" >> $GITHUB_STEP_SUMMARY
        echo "**Commit:** ${{ github.sha }}" >> $GITHUB_STEP_SUMMARY
        echo "" >> $GITHUB_STEP_SUMMARY
        echo "### Environments" >> $GITHUB_STEP_SUMMARY
        echo "- **Production:** ${{ steps.production-url.outputs.url }}" >> $GITHUB_STEP_SUMMARY
        echo "" >> $GITHUB_STEP_SUMMARY
        echo "### Monitoring" >> $GITHUB_STEP_SUMMARY
        echo "- View logs in Azure Portal" >> $GITHUB_STEP_SUMMARY
        echo "- Monitor application metrics" >> $GITHUB_STEP_SUMMARY
```

### Step 4: Azure Container Apps Configuration

Create `azure/container-app-spec.yaml` for advanced configuration:

```yaml
# Container App specification for advanced scenarios
properties:
  managedEnvironmentId: /subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.App/managedEnvironments/{environment-name}
  configuration:
    secrets:
    - name: registry-password
      value: "{acr-password}"
    registries:
    - server: {acr-server}
      username: {acr-username}
      passwordSecretRef: registry-password
    ingress:
      external: false  # Set to true if you need external access
      targetPort: 8080
      traffic:
      - weight: 100
        latestRevision: true
    dapr:
      enabled: false
  template:
    containers:
    - image: {acr-server}/scheduled-job:latest
      name: scheduled-job
      env:
      - name: DOTNET_ENVIRONMENT
        value: "Production"
      - name: AZURE_CLIENT_ID
        value: "{managed-identity-client-id}"
      resources:
        cpu: 0.5
        memory: 1Gi
      probes:
      - type: Liveness
        httpGet:
          path: /health
          port: 8080
        initialDelaySeconds: 30
        periodSeconds: 30
      - type: Readiness
        httpGet:
          path: /ready
          port: 8080
        initialDelaySeconds: 5
        periodSeconds: 10
    scale:
      minReplicas: 0
      maxReplicas: 5
      rules:
      - name: cpu-scaling
        custom:
          type: cpu
          metadata:
            type: Utilization
            value: "70"
      - name: memory-scaling
        custom:
          type: memory
          metadata:
            type: Utilization
            value: "80"
```

### Step 5: Monitoring and Logging Setup

Create `azure/monitoring-setup.sh`:

```bash
#!/bin/bash
set -e

source azure-config.env

echo "=== Setting up Azure Monitoring ==="

# Create Application Insights
AI_NAME="ai-scheduled-job"
az monitor app-insights component create \
  --app $AI_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --workspace /subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.OperationalInsights/workspaces/$LOG_ANALYTICS_WORKSPACE

# Get Application Insights instrumentation key
AI_INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app $AI_NAME \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey -o tsv)

# Create alert rules
echo "Creating alert rules..."

# Alert for container app failures
az monitor metrics alert create \
  --name "ContainerApp-HighFailureRate" \
  --resource-group $RESOURCE_GROUP \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.App/containerApps/$CONTAINER_APP_NAME \
  --condition "count ConsoleErrors aggregation Total > 5" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 2 \
  --description "Alert when container app has high failure rate"

# Alert for high CPU usage
az monitor metrics alert create \
  --name "ContainerApp-HighCPU" \
  --resource-group $RESOURCE_GROUP \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.App/containerApps/$CONTAINER_APP_NAME \
  --condition "average UsageNanoCores aggregation Average > 400000000" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 3 \
  --description "Alert when CPU usage is consistently high"

echo "Monitoring setup complete!"
echo "Application Insights: $AI_NAME"
echo "Instrumentation Key: $AI_INSTRUMENTATION_KEY"
```

### Step 6: Scheduled Job Configuration

For scheduled execution, create `azure/schedule-setup.sh`:

```bash
#!/bin/bash
set -e

source azure-config.env

echo "=== Setting up Scheduled Execution ==="

# Create Logic App for scheduling (alternative to cron jobs)
LOGIC_APP_NAME="logic-scheduled-job-trigger"

# Create Logic App workflow definition
cat > logic-app-workflow.json << 'EOF'
{
  "definition": {
    "$schema": "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
    "actions": {
      "HTTP": {
        "type": "Http",
        "inputs": {
          "method": "POST",
          "uri": "https://{container-app-url}/trigger",
          "headers": {
            "Content-Type": "application/json"
          },
          "body": {
            "jobType": "scheduled",
            "triggeredBy": "logic-app",
            "timestamp": "@{utcNow()}"
          }
        }
      }
    },
    "triggers": {
      "Recurrence": {
        "type": "Recurrence",
        "recurrence": {
          "frequency": "Hour",
          "interval": 1
        }
      }
    }
  }
}
EOF

# Note: For this tutorial, we'll use Container Apps Jobs instead
echo "Creating Container Apps Job for scheduled execution..."

az containerapp job create \
  --name "job-$CONTAINER_APP_NAME" \
  --resource-group $RESOURCE_GROUP \
  --environment $CONTAINER_APP_ENV \
  --trigger-type Schedule \
  --cron-expression "0 */1 * * *" \
  --replica-timeout 1800 \
  --replica-retry-limit 3 \
  --replica-completion-count 1 \
  --parallelism 1 \
  --image $ACR_NAME.azurecr.io/scheduled-job:latest \
  --registry-server $ACR_NAME.azurecr.io \
  --registry-username $(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query username -o tsv) \
  --registry-password $(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query passwords[0].value -o tsv) \
  --cpu 0.25 \
  --memory 0.5Gi \
  --env-vars \
    DOTNET_ENVIRONMENT=Production \
    JOB_TYPE=Scheduled

echo "Scheduled job created successfully!"
echo "Job will run every hour"
```

### Step 7: Testing and Validation

Create `scripts/test-azure-deployment.sh`:

```bash
#!/bin/bash
set -e

source azure-config.env

echo "=== Testing Azure Deployment ==="

# Test 1: Check if container app is running
echo "Test 1: Container app status..."
STATUS=$(az containerapp show \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.provisioningState -o tsv)

if [ "$STATUS" = "Succeeded" ]; then
    echo "✅ Container app is running"
else
    echo "❌ Container app status: $STATUS"
    exit 1
fi

# Test 2: Check logs
echo "Test 2: Checking recent logs..."
az containerapp logs show \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --tail 10

# Test 3: Check revisions
echo "Test 3: Checking revisions..."
az containerapp revision list \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query '[].{Name:name,Active:properties.active,Traffic:properties.trafficWeight}' \
  --output table

# Test 4: Trigger manual job execution (if job exists)
echo "Test 4: Triggering manual job execution..."
if az containerapp job show --name "job-$CONTAINER_APP_NAME" --resource-group $RESOURCE_GROUP > /dev/null 2>&1; then
    az containerapp job start \
      --name "job-$CONTAINER_APP_NAME" \
      --resource-group $RESOURCE_GROUP
    echo "✅ Manual job execution triggered"
else
    echo "ℹ️  Container app job not found (may not be configured)"
fi

echo "🎉 Azure deployment tests completed!"
```

## 💡 Tips and Tricks

### Azure Container Apps Best Practices
- **Resource sizing:** Start small and scale based on monitoring
- **Revision management:** Use blue-green deployments for zero downtime
- **Environment separation:** Use different environments for staging/production
- **Secrets management:** Use Azure Key Vault for sensitive configuration

### CI/CD Optimization
- **Cache layers:** Use Docker layer caching to speed up builds
- **Parallel jobs:** Run tests and security scans in parallel
- **Environment protection:** Require approvals for production deployments
- **Rollback strategy:** Keep previous revisions for quick rollbacks

### Monitoring and Observability
- **Structured logging:** Use consistent log formats for better querying
- **Correlation IDs:** Track requests across service boundaries
- **Custom metrics:** Expose application-specific metrics
- **Alerting:** Set up proactive alerts for critical failures

### Security Considerations
- **Managed identities:** Use Azure AD for service authentication
- **Network isolation:** Use virtual networks for secure communication
- **Image scanning:** Scan container images for vulnerabilities
- **Secrets rotation:** Implement automatic secret rotation

## ⚠️ Common Pitfalls

### Azure Resource Issues
- **Naming conflicts:** Container registry names must be globally unique
- **Region availability:** Not all regions support Container Apps
- **Quota limits:** Check subscription limits for containers and cores
- **Resource dependencies:** Log Analytics workspace required for Container Apps

### GitHub Actions Issues
- **Secret management:** Ensure all required secrets are configured
- **Service principal permissions:** SP needs contributor access to resource group
- **Workflow triggers:** Be careful with workflow trigger conditions
- **Rate limiting:** GitHub Actions has usage limits

### Container Apps Specific
- **Cold starts:** First request after idle period may be slow
- **Scale to zero:** Understand implications of min replicas = 0
- **Ingress configuration:** External ingress required for HTTP triggers
- **Log retention:** Configure appropriate log retention policies

### Deployment Issues
- **Image pull failures:** Ensure container registry credentials are correct
- **Environment variables:** Missing or incorrect environment configuration
- **Health checks:** Configure appropriate startup and liveness probes
- **Revision management:** Understand traffic splitting between revisions

## ✅ Verification Steps

### 1. Azure Resources Created
```bash
source azure-config.env
az group show --name $RESOURCE_GROUP
# Should show resource group with all resources
```

### 2. GitHub Actions Pipeline Runs
```bash
gh run list --limit 5
# Should show successful workflow runs
```

### 3. Container App Deployed
```bash
az containerapp show --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP
# Should show running container app
```

### 4. Logs Available
```bash
az containerapp logs show --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --tail 20
# Should show application logs
```

### 5. Monitoring Configured
```bash
az monitor metrics alert list --resource-group $RESOURCE_GROUP
# Should show configured alert rules
```

## 🔍 Advanced Troubleshooting

### Container App Issues
```bash
# Check container app status
az containerapp show --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --query properties.provisioningState

# Check recent deployments
az containerapp revision list --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP

# Check environment status
az containerapp env show --name $CONTAINER_APP_ENV --resource-group $RESOURCE_GROUP

# View detailed logs
az containerapp logs show --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --follow
```

### GitHub Actions Debugging
```bash
# Check workflow status
gh run list --workflow=azure-deploy.yml

# View workflow logs
gh run view --log

# Check repository secrets
gh secret list
```

### Azure Registry Issues
```bash
# Test registry login
az acr login --name $ACR_NAME

# List images
az acr repository list --name $ACR_NAME

# Check repository tags
az acr repository show-tags --name $ACR_NAME --repository scheduled-job
```

## 🎯 Learning Checkpoint

Congratulations! You've successfully completed all phases. You should now be able to:
- [ ] Deploy containerized applications to Azure Container Apps
- [ ] Implement complete CI/CD pipelines
- [ ] Monitor and troubleshoot cloud deployments
- [ ] Manage production environments securely
- [ ] Scale and optimize containerized workloads

## 📚 Key Concepts Mastered

1. **Cloud Deployment:** Azure Container Apps configuration and management
2. **CI/CD Pipelines:** Automated build, test, and deployment workflows
3. **Infrastructure as Code:** Azure resource management and configuration
4. **Security:** Azure AD, managed identities, and secret management
5. **Monitoring:** Application insights, logging, and alerting
6. **Production Operations:** Blue-green deployments, scaling, and rollbacks

## 🎉 Congratulations!

You've successfully completed the entire containerization tutorial! You now have:

- A production-ready .NET application
- Optimized Docker containerization
- Complete CI/CD automation
- Cloud-native deployment on Azure
- Comprehensive monitoring and alerting

## Next Steps for Further Learning

1. **Advanced Container Patterns:** Learn about sidecar containers, init containers
2. **Microservices Architecture:** Break applications into smaller services
3. **Service Mesh:** Implement Istio or Linkerd for advanced networking
4. **GitOps:** Implement ArgoCD for declarative deployments
5. **Advanced Monitoring:** Implement distributed tracing with OpenTelemetry
6. **Multi-cloud Deployments:** Deploy to AWS ECS or Google Cloud Run

## 📖 Additional Resources

- [Azure Container Apps Documentation](https://docs.microsoft.com/en-us/azure/container-apps/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [.NET Container Images](https://docs.microsoft.com/en-us/dotnet/core/docker/)
- [Container Security Best Practices](https://docs.microsoft.com/en-us/azure/container-instances/container-instances-image-security)