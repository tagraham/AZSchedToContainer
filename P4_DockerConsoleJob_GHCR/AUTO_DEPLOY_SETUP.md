# Deployment Options for P4

## Current Status
✅ Container Apps Job is working
✅ Manual deployment works
⚠️ Auto-deploy requires workflow in ROOT .github/workflows/

## Why GitHub Actions Needs Root Directory

**GitHub Actions limitation**: Workflows MUST be in `.github/workflows/` at repository root
- Workflows in subdirectories like `P4_DockerConsoleJob_GHCR/.github/workflows/` are NOT detected
- This is a GitHub design decision (not configurable)
- Many developers have this same frustration!

## Your Deployment Options

### Option 1: Manual Deployment (Simplest)
Perfect when you want full control over when to deploy:
```bash
cd P4_DockerConsoleJob_GHCR

# Build and tag
docker build -t ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest .

# Push to GitHub Container Registry
docker push ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest

# Update Azure
az containerapp job update \
  --name docker-console-job \
  --resource-group rgDCJ \
  --image ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest
```

### Option 2: Use the Deploy Script (Included)
```bash
cd P4_DockerConsoleJob_GHCR
./deploy.sh
```
This script handles: build → push → Azure update

### Option 3: GitHub Actions (Auto-Deploy on Push)
The dispatcher workflow at `/.github/workflows/p4-deploy-dispatcher.yml`:
- Triggers when you push changes to P4 folder
- Automatically builds and pushes to GHCR
- Shows you the Azure command to run after

To use:
1. Push your changes: `git push`
2. Check GitHub Actions tab for build status
3. When complete, run: `az containerapp job update --name docker-console-job --resource-group rgDCJ --image ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest`

### 2. Verify on GitHub
- Go to: https://github.com/tagraham/AZSchedToContainer/actions
- You should see the workflow available

### 3. How It Works
When you push changes to P4 folder:
1. GitHub Actions automatically triggers
2. Builds new Docker image
3. Pushes to ghcr.io (GitHub Container Registry)
4. Image is tagged as `latest`

### 4. Update Azure to Use New Image
After GitHub Actions completes (check the Actions tab), run:
```bash
az containerapp job update \
  --name docker-console-job \
  --resource-group rgDCJ \
  --image ghcr.io/tagraham/azschedtocontainer/scheduled-job:latest
```

## Testing Auto-Deploy

### Make a Test Change
```bash
# Edit something in P4
echo "# Test change" >> P4_DockerConsoleJob_GHCR/README.md

# Commit and push
git add P4_DockerConsoleJob_GHCR/README.md
git commit -m "Test auto-deploy"
git push origin main
```

### Watch It Deploy
1. Go to https://github.com/tagraham/AZSchedToContainer/actions
2. Watch the workflow run
3. After completion, update the Azure job (command above)
4. Test the job: `az containerapp job start --name docker-console-job --resource-group rgDCJ`

## Manual Deploy (Without Auto-Deploy)

If you prefer manual control:
```bash
# Build locally
cd P4_DockerConsoleJob_GHCR
docker build -t ghcr.io/tagraham/azschedtocontainer/scheduled-job:v1.0.1 .

# Push to GHCR
docker push ghcr.io/tagraham/azschedtocontainer/scheduled-job:v1.0.1

# Update Azure
az containerapp job update \
  --name docker-console-job \
  --resource-group rgDCJ \
  --image ghcr.io/tagraham/azschedtocontainer/scheduled-job:v1.0.1
```

## Important Notes

⚠️ **The GitHub Actions workflow is NOT active until you push it to GitHub**

⚠️ **After each GitHub Actions deployment, you must manually update the Azure job to use the new image** (the workflow builds and pushes the image, but doesn't update the Azure job)

💡 **Tip**: You could enhance the workflow to automatically update the Azure job, but that requires more Azure credentials setup.