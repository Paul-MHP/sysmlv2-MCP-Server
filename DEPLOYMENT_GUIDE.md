# Deployment Guide

Complete guide for deploying the SysML v2 MCP Server to Azure Functions.

## Prerequisites

- Azure subscription with permission to create Function Apps
- Access to Azure Cloud Shell (recommended) or Azure CLI locally
- SysML v2 API endpoint (default: `https://sysml-api-webapp-2024.azurewebsites.net`)

## Method 1: Automated Deployment (Recommended)

### Step 1: Open Azure Cloud Shell
1. Go to https://shell.azure.com
2. Choose Bash environment
3. Wait for the shell to initialize

### Step 2: Run Deployment Script
```bash
# Download and run the deployment script
curl -o deploy-azure.sh https://raw.githubusercontent.com/Paul-MHP/sysmlv2-MCP-Server/main/deploy-azure.sh
chmod +x deploy-azure.sh

# Edit configuration (optional)
nano deploy-azure.sh  # Modify RESOURCE_GROUP, LOCATION as needed

# Run deployment
./deploy-azure.sh
```

### Step 3: Verify Deployment
The script will output your function URL. Test it:
```bash
curl -X POST https://YOUR-FUNCTION-APP.azurewebsites.net/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":"1","method":"tools/list"}'
```

## Method 2: Manual Deployment

### Step 1: Set Configuration
```bash
# Set your configuration variables
RESOURCE_GROUP="287013_Scalable_AI_Applications"  # Change to your resource group
LOCATION="westeurope"
STORAGE_ACCOUNT="sysmlv2mcpstorage$(date +%s | tail -c 6)"
FUNCTION_APP="sysmlv2-mcp-server-$(date +%s | tail -c 6)"
SYSML_API_URL="https://sysml-api-webapp-2024.azurewebsites.net"
```

### Step 2: Create Azure Resources
```bash
# Create storage account
az storage account create \
  --name "$STORAGE_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku "Standard_LRS" \
  --kind "StorageV2"

# Create function app
az functionapp create \
  --resource-group "$RESOURCE_GROUP" \
  --consumption-plan-location "$LOCATION" \
  --runtime "dotnet-isolated" \
  --runtime-version "8" \
  --functions-version "4" \
  --name "$FUNCTION_APP" \
  --storage-account "$STORAGE_ACCOUNT" \
  --disable-app-insights false

# Configure app settings
az functionapp config appsettings set \
  --name "$FUNCTION_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --settings \
    "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" \
    "FUNCTIONS_EXTENSION_VERSION=~4" \
    "DOTNET_VERSION=8.0" \
    "SYSML_API_BASE_URL=$SYSML_API_URL"

# Enable CORS
az functionapp cors add \
  --name "$FUNCTION_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --allowed-origins "*"
```

### Step 3: Deploy Code
```bash
# Clone and build
git clone https://github.com/Paul-MHP/sysmlv2-MCP-Server.git
cd sysmlv2-MCP-Server
dotnet build --configuration Release

# Deploy using func tools
func azure functionapp publish $FUNCTION_APP --dotnet-isolated --force
```

## Common Issues & Solutions

### Issue 1: Function not responding
**Symptoms:** Function times out or returns no response

**Solutions:**
1. **Wait for cold start:** First request may take 30-60 seconds
2. **Check function exists:** Go to Azure Portal → Functions → verify `mcp` function is listed
3. **Check logs:** Azure Portal → Monitoring → Log stream

### Issue 2: "func command not found"
**Symptoms:** `func: command not found` error

**Solutions:**
```bash
# Install Azure Functions Core Tools
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
sudo mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
sudo sh -c 'echo "deb [arch=amd64,arm64,armhf signed-by=/etc/apt/trusted.gpg.d/microsoft.gpg] https://packages.microsoft.com/repos/microsoft-ubuntu-$(lsb_release -cs)-prod $(lsb_release -cs) main" > /etc/apt/sources.list.d/dotnetdev.list'
sudo apt update
sudo apt install azure-functions-core-tools-4
```

### Issue 3: Build errors
**Symptoms:** `dotnet build` fails

**Solutions:**
1. **Install .NET SDK:**
   ```bash
   sudo apt install -y dotnet-sdk-8.0
   ```
2. **Clean and rebuild:**
   ```bash
   dotnet clean
   dotnet build --configuration Release
   ```

### Issue 4: Deployment fails
**Symptoms:** `func azure functionapp publish` fails

**Solutions:**
1. **Specify runtime explicitly:**
   ```bash
   func azure functionapp publish $FUNCTION_APP --dotnet-isolated --force
   ```
2. **Check Azure permissions:** Ensure you have Contributor role on Resource Group
3. **Try ZIP deployment as fallback:**
   ```bash
   wget https://github.com/Paul-MHP/sysmlv2-MCP-Server/archive/main.zip -O sysml-fixed.zip
   az webapp deploy --name "$FUNCTION_APP" --resource-group "$RESOURCE_GROUP" --src-path sysml-fixed.zip --type zip
   ```

### Issue 5: Application Insights errors
**Symptoms:** Can't add Application Insights, log stream not working

**Solutions:**
1. **Skip Application Insights:** Function will work without it
2. **Create manually if needed:**
   ```bash
   az monitor app-insights component create \
     --app "$FUNCTION_APP-insights" \
     --location "$LOCATION" \
     --resource-group "$RESOURCE_GROUP"
   ```

## Testing Your Deployment

### 1. Basic Function Test
```bash
# Test if function is responding
curl https://YOUR-FUNCTION-APP.azurewebsites.net

# Should return Azure Functions default page
```

### 2. MCP Protocol Test
```bash
# Test tools list
curl -X POST https://YOUR-FUNCTION-APP.azurewebsites.net/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":"1","method":"tools/list"}'

# Should return list of 6 MCP tools
```

### 3. SysML API Integration Test
```bash
# Test list projects
curl -X POST https://YOUR-FUNCTION-APP.azurewebsites.net/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":"2","method":"tools/call","params":{"name":"list_projects","arguments":{}}}'

# Should return list of SysML projects
```

## Monitoring & Maintenance

### View Logs
```bash
# Real-time logs
az functionapp logs tail --name YOUR-FUNCTION-APP --resource-group YOUR-RESOURCE-GROUP

# Or in Azure Portal: Function App → Monitoring → Log stream
```

### Update Deployment
```bash
# To redeploy after code changes
cd sysmlv2-MCP-Server
git pull
dotnet build --configuration Release
func azure functionapp publish YOUR-FUNCTION-APP --dotnet-isolated --force
```

### Clean Up Resources
```bash
# Delete everything (BE CAREFUL!)
az group delete --name YOUR-RESOURCE-GROUP --yes --no-wait
```

## Configuration Reference

### Required Environment Variables
- `SYSML_API_BASE_URL`: SysML v2 API endpoint
- `FUNCTIONS_WORKER_RUNTIME`: Set to `dotnet-isolated`
- `FUNCTIONS_EXTENSION_VERSION`: Set to `~4`

### Optional Configuration
- `CORS`: Set to `*` for testing, restrict for production
- Application Insights: Optional but helpful for monitoring

## Next Steps

After successful deployment:
1. **Test all MCP tools** using the curl commands above
2. **Set up Claude integration** following [CLAUDE_INTEGRATION.md](CLAUDE_INTEGRATION.md)
3. **Monitor function performance** in Azure Portal
4. **Consider upgrading to Premium plan** for production use (eliminates cold starts)

## Support

If you encounter issues:
1. Check this troubleshooting guide
2. Review Azure Functions documentation
3. Check the GitHub repository issues
4. Verify your Azure permissions and quotas