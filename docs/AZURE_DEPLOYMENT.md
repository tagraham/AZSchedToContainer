# Azure Container Apps Deployment

## Critical: Single Instance Management

**IMPORTANT**: This application MUST run only one instance at a time to prevent:
- Duplicate email processing
- Data corruption
- Race conditions
- Conflicting file operations

## Azure Container Apps Configuration

### Container App Settings
```yaml
# container-app.yaml
properties:
  configuration:
    replicaTimeout: 300
    replicaRetryLimit: 0
  template:
    scale:
      minReplicas: 0
      maxReplicas: 1  # CRITICAL: Never allow more than 1
      rules:
        - name: "manual-scale"
          custom:
            type: "external"
            metadata:
              scalerAddress: "manual"
    containers:
    - name: scheduled-job
      image: yourregistry.azurecr.io/scheduled-job:latest
      resources:
        cpu: 0.25
        memory: 0.5Gi
      env:
      - name: ASPNETCORE_ENVIRONMENT
        value: "Production"
```

### Scheduled Execution Options

#### Option 1: Azure Container Apps Jobs (Recommended)
```bash
# Create Container Apps Job for scheduled execution
az containerapp job create \
  --name scheduled-job \
  --resource-group myResourceGroup \
  --environment myContainerEnv \
  --trigger-type "Schedule" \
  --cron-expression "0 2 * * *" \
  --replica-timeout 1800 \
  --replica-retry-limit 0 \
  --parallelism 1 \
  --replica-completion-count 1 \
  --image yourregistry.azurecr.io/scheduled-job:latest
```

#### Option 2: Azure Logic Apps Trigger
- Logic App calls Container Apps REST API
- Ensures single instance execution
- Better monitoring and error handling

#### Option 3: Azure Functions Timer Trigger
- Function checks if instance is running
- Only starts container if none active
- Provides additional control layer

## GitHub Workflow Setup (Next Phase)

### Required Azure Resources
1. **Azure Container Registry (ACR)**
   - Store Docker images
   - Integrate with Container Apps

2. **Azure Container Apps Environment**
   - Managed Kubernetes environment
   - Network isolation and scaling

3. **Service Principal**
   - GitHub Actions authentication
   - Least privilege access

### GitHub Secrets Required
```
AZURE_CLIENT_ID
AZURE_CLIENT_SECRET
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
REGISTRY_LOGIN_SERVER
REGISTRY_USERNAME
REGISTRY_PASSWORD
```

### Workflow File Structure
```
.github/
└── workflows/
    ├── build-and-deploy.yml
    └── infrastructure.yml
```

## Monitoring and Alerts

### Critical Alerts
1. **Multiple Instances Running** - Immediate alert
2. **Job Failure** - Email notification
3. **Long Running Jobs** - Alert after timeout
4. **Resource Limits** - CPU/Memory thresholds

### Log Analytics Queries
```kql
// Check for multiple concurrent instances
ContainerAppConsoleLogs_CL
| where ContainerName_s == "scheduled-job"
| summarize InstanceCount = dcount(PodName_s) by bin(TimeGenerated, 1m)
| where InstanceCount > 1
```

## Security Considerations

1. **Managed Identity** - No secrets in container
2. **Network Isolation** - Private endpoints only
3. **Image Scanning** - Vulnerability assessment
4. **RBAC** - Minimal required permissions

## Cost Optimization

- **Scale to Zero** - No cost when not running
- **Consumption Plan** - Pay per execution
- **Resource Limits** - Prevent runaway costs