#!/bin/bash

# SysML v2 MCP Server - Azure Cloud Shell Deployment Script
# This script deploys the complete MCP server from Azure Cloud Shell

set -e  # Exit on any error

# Configuration variables
RESOURCE_GROUP="sysmlv2-mcp-rg"
LOCATION="East US"
STORAGE_ACCOUNT="sysmlv2mcpstorage$(date +%s | tail -c 6)"  # Last 5 digits for uniqueness
FUNCTION_APP="sysmlv2-mcp-server-$(date +%s | tail -c 6)"
SYSML_API_URL="https://sysml-api-webapp-2024.azurewebsites.net"

echo "🚀 Starting SysML v2 MCP Server deployment from Azure Cloud Shell..."
echo "════════════════════════════════════════════════════════════════"
echo "Resource Group: $RESOURCE_GROUP"
echo "Function App: $FUNCTION_APP"
echo "Storage Account: $STORAGE_ACCOUNT"
echo "SysML API URL: $SYSML_API_URL"
echo "════════════════════════════════════════════════════════════════"

# Check if we're in Azure Cloud Shell
if [ -z "$AZURE_HTTP_USER_AGENT" ]; then
    echo "⚠️  Warning: This script is designed for Azure Cloud Shell"
    echo "   Make sure you have Azure CLI and .NET 8 installed locally"
fi

# Step 1: Create resource group
echo ""
echo "📁 Step 1: Creating resource group..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output table

if [ $? -eq 0 ]; then
  echo "✅ Resource group created successfully"
else
  echo "❌ Failed to create resource group"
  exit 1
fi

# Step 2: Create storage account
echo ""
echo "💾 Step 2: Creating storage account..."
az storage account create \
  --name "$STORAGE_ACCOUNT" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku "Standard_LRS" \
  --kind "StorageV2" \
  --output table

if [ $? -eq 0 ]; then
  echo "✅ Storage account created successfully"
else
  echo "❌ Failed to create storage account"
  exit 1
fi

# Step 3: Create function app
echo ""
echo "⚡ Step 3: Creating Azure Function App..."
az functionapp create \
  --resource-group "$RESOURCE_GROUP" \
  --consumption-plan-location "$LOCATION" \
  --runtime "dotnet-isolated" \
  --runtime-version "8" \
  --functions-version "4" \
  --name "$FUNCTION_APP" \
  --storage-account "$STORAGE_ACCOUNT" \
  --disable-app-insights false \
  --output table

if [ $? -eq 0 ]; then
  echo "✅ Function app created successfully"
else
  echo "❌ Failed to create function app"
  exit 1
fi

# Step 4: Configure environment variables
echo ""
echo "🔧 Step 4: Configuring environment variables..."
az functionapp config appsettings set \
  --name "$FUNCTION_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --settings "SYSML_API_BASE_URL=$SYSML_API_URL" \
  --output table

echo "✅ Environment variables configured"

# Step 5: Enable CORS
echo ""
echo "🌐 Step 5: Enabling CORS for web access..."
az functionapp cors add \
  --name "$FUNCTION_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --allowed-origins "*" \
  --output table

echo "✅ CORS configured"

# Step 6: Install Azure Functions Core Tools (if not available)
echo ""
echo "🔧 Step 6: Checking Azure Functions Core Tools..."
if ! command -v func &> /dev/null; then
  echo "📥 Installing Azure Functions Core Tools..."
  curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
  sudo mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
  sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-$(lsb_release -cs)-prod $(lsb_release -cs) main" > /etc/apt/sources.list.d/dotnetdev.list'
  sudo apt-get update
  sudo apt-get install azure-functions-core-tools-4
else
  echo "✅ Azure Functions Core Tools already available"
fi

# Step 7: Build the project
echo ""
echo "🔨 Step 7: Building .NET project..."
dotnet build --configuration Release --verbosity minimal

if [ $? -eq 0 ]; then
  echo "✅ Project built successfully"
else
  echo "❌ Build failed"
  exit 1
fi

# Step 8: Deploy to Azure
echo ""
echo "🚀 Step 8: Deploying to Azure Functions..."
echo "This may take a few minutes..."

func azure functionapp publish "$FUNCTION_APP" --force

if [ $? -eq 0 ]; then
  echo ""
  echo "🎉 DEPLOYMENT COMPLETED SUCCESSFULLY!"
  echo "════════════════════════════════════════════════════════════════"
  echo "Your SysML v2 MCP Server is now deployed and running!"
  echo ""
  echo "📍 Function App URL: https://$FUNCTION_APP.azurewebsites.net"
  echo "🔗 MCP Endpoint: https://$FUNCTION_APP.azurewebsites.net/api/mcp"
  echo ""
  echo "🧪 Test your deployment:"
  echo "curl -X POST https://$FUNCTION_APP.azurewebsites.net/api/mcp \\"
  echo "  -H 'Content-Type: application/json' \\"
  echo "  -d '{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"tools/list\"}'"
  echo ""
  echo "📚 Next steps:"
  echo "1. Test the MCP endpoint using the curl command above"
  echo "2. Follow CLAUDE_INTEGRATION.md to connect with Claude Desktop"
  echo "3. Update your Claude configuration with the Function App URL:"
  echo "   https://$FUNCTION_APP.azurewebsites.net/api/mcp"
  echo ""
  echo "🔍 Monitor your function:"
  echo "az functionapp logs tail --name $FUNCTION_APP --resource-group $RESOURCE_GROUP"
  echo ""
  echo "🗑️  To clean up (delete all resources):"
  echo "az group delete --name $RESOURCE_GROUP --yes --no-wait"
  echo "════════════════════════════════════════════════════════════════"
else
  echo "❌ Deployment failed"
  echo "Check the error messages above and try again"
  exit 1
fi