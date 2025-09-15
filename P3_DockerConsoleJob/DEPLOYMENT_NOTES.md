# P3 Deployment Notes

## Azure Container Apps Configuration

### Critical: Single Instance Only
- **maxReplicas: 1** - Prevents duplicate processing
- **parallelism: 1** - Ensures one job at a time
- **replica-retry-limit: 0** - No automatic retries

### Recommended Deployment Pattern
Use **Azure Container Apps Jobs** with scheduled triggers:

```bash
az containerapp job create \
  --name scheduled-job \
  --trigger-type "Schedule" \
  --cron-expression "0 2 * * *" \
  --parallelism 1 \
  --replica-completion-count 1 \
  --image yourregistry.azurecr.io/scheduled-job:latest
```

### GitHub Actions Integration
- Automated build and deploy on push
- Infrastructure as Code with Bicep/ARM
- Container Registry integration
- Environment-specific deployments

## Next Phase: CI/CD Pipeline Setup