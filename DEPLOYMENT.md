# Azure Deployment Guide

This guide provides step-by-step instructions for deploying the SysML v2 MCP Server to Azure Functions.

## Method 1: Azure Cloud Shell Deployment (Recommended)

Deploy directly from Azure Cloud Shell - no local tools required!

### Prerequisites
- Azure subscription with appropriate permissions
- Access to Azure Portal (portal.azure.com)

### Step 1: Open Azure Cloud Shell
1. Go to [Azure Portal](https://portal.azure.com)
2. Click the Cloud Shell icon (>_) in the top navigation bar
3. Choose **Bash** when prompted
4. Wait for Cloud Shell to initialize

### Step 2: Clone and Setup Project
```bash
# Clone the repository
git clone https://github.com/Paul-MHP/sysmlv2-MCP-Server.git
cd sysmlv2-MCP-Server

# Make scripts executable
chmod +x deploy.sh
```

### Step 3: Run Complete Deployment Script
```bash
# Create the deployment script
cat > deploy.sh << 'EOF'
#!/bin/bash

# Configuration variables
RESOURCE_GROUP="sysmlv2-mcp-rg"
LOCATION="East US"
STORAGE_ACCOUNT="sysmlv2mcpstorage$(date +%s)"
FUNCTION_APP="sysmlv2-mcp-server-$(date +%s)"
SYSML_API_URL="https://sysml-api-webapp-2024.azurewebsites.net"

echo "🚀 Starting SysML v2 MCP Server deployment..."
echo "Resource Group: $RESOURCE_GROUP"
echo "Function App: $FUNCTION_APP"
echo "Storage Account: $STORAGE_ACCOUNT"

# Step 1: Create resource group
echo "📁 Creating resource group..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION"

if [ $? -eq 0 ]; then
  echo "✅ Resource group created successfully"
else
  echo "❌ Failed to create resource group"
  exit 1
fi

# Step 2: Create storage account
echo "💾 Creating storage account..."
az storage account create \
  --name "$STORAGE_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku "Standard_LRS" \
  --kind "StorageV2"

if [ $? -eq 0 ]; then
  echo "✅ Storage account created successfully"
else
  echo "❌ Failed to create storage account"
  exit 1
fi

# Step 3: Create function app
echo "⚡ Creating Azure Function App..."
az functionapp create \
  --resource-group "$RESOURCE_GROUP" \
  --consumption-plan-location "$LOCATION" \
  --runtime "dotnet-isolated" \
  --runtime-version "8" \
  --functions-version "4" \
  --name "$FUNCTION_APP" \
  --storage-account "$STORAGE_ACCOUNT" \
  --disable-app-insights false

if [ $? -eq 0 ]; then
  echo "✅ Function app created successfully"
else
  echo "❌ Failed to create function app"
  exit 1
fi

# Step 4: Configure environment variables
echo "🔧 Configuring environment variables..."
az functionapp config appsettings set \
  --name "$FUNCTION_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --settings "SYSML_API_BASE_URL=$SYSML_API_URL"

# Step 5: Enable CORS
echo "🌐 Enabling CORS..."
az functionapp cors add \
  --name "$FUNCTION_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --allowed-origins "*"

# Step 6: Build and deploy code
echo "📦 Building and deploying code..."

# Install Azure Functions Core Tools if not available
if ! command -v func &> /dev/null; then
  echo "📥 Installing Azure Functions Core Tools..."
  npm install -g azure-functions-core-tools@4 --unsafe-perm true
fi

# Build the project
echo "🔨 Building .NET project..."
dotnet build --configuration Release

# Deploy to Azure
echo "🚀 Deploying to Azure..."
func azure functionapp publish "$FUNCTION_APP" --force

if [ $? -eq 0 ]; then
  echo "✅ Deployment completed successfully!"
  echo ""
  echo "🎉 Your MCP Server is deployed!"
  echo "Function App URL: https://$FUNCTION_APP.azurewebsites.net"
  echo "MCP Endpoint: https://$FUNCTION_APP.azurewebsites.net/api/mcp"
  echo ""
  echo "🧪 Test your deployment:"
  echo "curl -X POST https://$FUNCTION_APP.azurewebsites.net/api/mcp \\"
  echo "  -H 'Content-Type: application/json' \\"
  echo "  -d '{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"tools/list\"}'"
  echo ""
  echo "📚 Next steps:"
  echo "1. Test the MCP endpoint above"
  echo "2. Follow CLAUDE_INTEGRATION.md to connect with Claude"
  echo "3. Update your Claude configuration with the Function App URL"
else
  echo "❌ Deployment failed"
  exit 1
fi
EOF

# Make script executable and run
chmod +x deploy.sh
./deploy.sh
```

### Step 4: Test Your Deployment
After deployment completes, test the MCP server:

```bash
# Replace with your actual function app URL from the deployment output
FUNCTION_URL="https://sysmlv2-mcp-server-XXXXXX.azurewebsites.net"

# Test the MCP tools list
curl -X POST $FUNCTION_URL/api/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "1",
    "method": "tools/list"
  }'
```

Expected response:
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": {
    "tools": [
      {"name": "list_projects", "description": "List all SysML v2 projects"},
      {"name": "get_project", "description": "Get details of a specific SysML v2 project"},
      ...
    ]
  }
}
```

## Method 2: Manual Azure CLI Deployment

If you prefer step-by-step manual deployment:

### Step 1: Login and Setup
```bash
# Login to Azure
az login

# Set your subscription (if you have multiple)
az account set --subscription "your-subscription-id"

# Create resource group
az group create \
  --name "sysmlv2-mcp-rg" \
  --location "East US"
```

### Step 2: Create Storage Account
```bash
# Create storage account (required for Azure Functions)
az storage account create \
  --name "sysmlv2mcpstorage" \
  --resource-group "sysmlv2-mcp-rg" \
  --location "East US" \
  --sku "Standard_LRS"
```

### Step 3: Create Function App
```bash
# Create the function app
az functionapp create \
  --resource-group "sysmlv2-mcp-rg" \
  --consumption-plan-location "East US" \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --name "sysmlv2-mcp-server" \
  --storage-account "sysmlv2mcpstorage" \
  --disable-app-insights false
```

### Step 4: Configure Environment Variables
```bash
# Set the SysML API base URL
az functionapp config appsettings set \
  --name "sysmlv2-mcp-server" \
  --resource-group "sysmlv2-mcp-rg" \
  --settings "SYSML_API_BASE_URL=https://sysml-api-webapp-2024.azurewebsites.net"

# Enable CORS for all origins (adjust as needed for production)
az functionapp cors add \
  --name "sysmlv2-mcp-server" \
  --resource-group "sysmlv2-mcp-rg" \
  --allowed-origins "*"
```

### Step 5: Deploy Function Code
```bash
# Build and publish the function
dotnet publish --configuration Release

# Deploy to Azure (run from project root)
func azure functionapp publish sysmlv2-mcp-server
```

### Step 6: Verify Deployment
```bash
# Test the deployment
curl -X POST https://sysmlv2-mcp-server.azurewebsites.net/api/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "1",
    "method": "tools/list"
  }'
```

## Method 2: Azure Developer CLI (azd)

### Step 1: Initialize Project
```bash
# Install Azure Developer CLI if not already installed
# https://docs.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd

# Initialize the project
azd init
```

### Step 2: Deploy
```bash
# Deploy everything (infrastructure + code)
azd deploy
```

## Method 3: Azure Portal Deployment

### Step 1: Create Function App via Portal
1. Go to Azure Portal (portal.azure.com)
2. Click "Create a resource"
3. Search for "Function App"
4. Configure:
   - **Resource Group**: Create new "sysmlv2-mcp-rg"
   - **Function App Name**: "sysmlv2-mcp-server"
   - **Runtime**: .NET
   - **Version**: 8 (LTS)
   - **Hosting Plan**: Consumption (Serverless)

### Step 2: Configure Settings
1. Go to Function App → Configuration
2. Add Application Setting:
   - **Name**: `SYSML_API_BASE_URL`
   - **Value**: `https://sysml-api-webapp-2024.azurewebsites.net`

### Step 3: Deploy Code
```bash
# Deploy using func CLI
func azure functionapp publish sysmlv2-mcp-server
```

## Post-Deployment Configuration

### Enable Application Insights (Recommended)
```bash
# Create Application Insights resource
az monitor app-insights component create \
  --app "sysmlv2-mcp-insights" \
  --location "East US" \
  --resource-group "sysmlv2-mcp-rg"

# Link to Function App
az functionapp config appsettings set \
  --name "sysmlv2-mcp-server" \
  --resource-group "sysmlv2-mcp-rg" \
  --settings "APPINSIGHTS_INSTRUMENTATIONKEY=$(az monitor app-insights component show --app sysmlv2-mcp-insights --resource-group sysmlv2-mcp-rg --query instrumentationKey -o tsv)"
```

### Configure Custom Domain (Optional)
```bash
# Add custom domain
az functionapp config hostname add \
  --hostname "your-domain.com" \
  --name "sysmlv2-mcp-server" \
  --resource-group "sysmlv2-mcp-rg"
```

### Setup SSL Certificate (Optional)
```bash
# Create managed certificate
az functionapp config ssl create \
  --hostname "your-domain.com" \
  --name "sysmlv2-mcp-server" \
  --resource-group "sysmlv2-mcp-rg"
```

## Environment Variables Reference

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `SYSML_API_BASE_URL` | Yes | - | Base URL of SysML v2 API server |
| `FUNCTIONS_WORKER_RUNTIME` | Yes | `dotnet-isolated` | Function runtime |
| `AzureWebJobsStorage` | Yes | Auto-configured | Storage connection string |

## Monitoring and Logs

### View Function Logs
```bash
# Stream logs
az webapp log tail \
  --name "sysmlv2-mcp-server" \
  --resource-group "sysmlv2-mcp-rg"

# Download logs
az webapp log download \
  --name "sysmlv2-mcp-server" \
  --resource-group "sysmlv2-mcp-rg"
```

### Application Insights Queries
Use these KQL queries in Application Insights:

```kql
// Recent function executions
requests
| where timestamp > ago(1h)
| project timestamp, name, success, duration, resultCode

// Function errors
exceptions
| where timestamp > ago(24h)
| project timestamp, type, outerMessage, operation_Name
```

## Troubleshooting

### Common Issues

1. **Function not responding**
   - Check Application Insights for errors
   - Verify environment variables are set
   - Ensure SysML API is accessible

2. **CORS errors**
   - Configure CORS settings in Function App
   - Check allowed origins

3. **Memory/timeout issues**
   - Consider upgrading to Premium plan for larger workloads
   - Optimize API calls and response sizes

### Diagnostic Commands
```bash
# Check function app status
az functionapp show \
  --name "sysmlv2-mcp-server" \
  --resource-group "sysmlv2-mcp-rg" \
  --query "state"

# Test connectivity to SysML API
az functionapp config appsettings list \
  --name "sysmlv2-mcp-server" \
  --resource-group "sysmlv2-mcp-rg" \
  --query "[?name=='SYSML_API_BASE_URL']"
```

## Scaling Considerations

### Consumption Plan (Default)
- Automatically scales based on demand
- Pay only for execution time
- 5-minute timeout limit
- Suitable for most MCP workloads

### Premium Plan (For High Volume)
```bash
# Create Premium plan if needed
az functionapp plan create \
  --name "sysmlv2-mcp-premium" \
  --resource-group "sysmlv2-mcp-rg" \
  --location "East US" \
  --sku EP1

# Update function app to use Premium plan
az functionapp update \
  --name "sysmlv2-mcp-server" \
  --resource-group "sysmlv2-mcp-rg" \
  --plan "sysmlv2-mcp-premium"
```

## Security Best Practices

1. **API Keys**: Consider implementing API key authentication
2. **CORS**: Restrict origins in production environments
3. **Network**: Use VNet integration for private API access
4. **Secrets**: Store sensitive configuration in Key Vault

## Cleanup

To remove all resources:
```bash
# Delete the entire resource group
az group delete \
  --name "sysmlv2-mcp-rg" \
  --yes --no-wait
```