#!/bin/bash

# SysML v2 MCP Server - Azure Deployment Script
# This script deploys the complete MCP server to Azure Functions

set -e  # Exit on any error

# Configuration variables (modify as needed)
RESOURCE_GROUP="287013_Scalable_AI_Applications"  # Change to your resource group
LOCATION="westeurope"
STORAGE_ACCOUNT="sysmlv2mcpstorage$(date +%s | tail -c 6)"  # Unique suffix
FUNCTION_APP="sysmlv2-mcp-server-$(date +%s | tail -c 6)"   # Unique suffix
SYSML_API_URL="https://sysml-api-webapp-2024.azurewebsites.net"

echo "🚀 Starting SysML v2 MCP Server deployment..."
echo "════════════════════════════════════════════════════════════════"
echo "Resource Group: $RESOURCE_GROUP"
echo "Function App: $FUNCTION_APP"
echo "Storage Account: $STORAGE_ACCOUNT"
echo "SysML API URL: $SYSML_API_URL"
echo "Location: $LOCATION"
echo "════════════════════════════════════════════════════════════════"

# Step 1: Create storage account
echo ""
echo "💾 Step 1: Creating storage account..."
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

# Step 2: Create function app
echo ""
echo "⚡ Step 2: Creating Azure Function App..."
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

# Step 3: Configure environment variables
echo ""
echo "🔧 Step 3: Configuring environment variables..."
az functionapp config appsettings set \
  --name "$FUNCTION_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --settings \
    "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" \
    "FUNCTIONS_EXTENSION_VERSION=~4" \
    "DOTNET_VERSION=8.0" \
    "SYSML_API_BASE_URL=$SYSML_API_URL" \
  --output table

echo "✅ Environment variables configured"

# Step 4: Enable CORS
echo ""
echo "🌐 Step 4: Enabling CORS for web access..."
az functionapp cors add \
  --name "$FUNCTION_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --allowed-origins "*" \
  --output table

echo "✅ CORS configured"

# Step 5: Clone and build the project
echo ""
echo "📦 Step 5: Cloning repository and building project..."
if [ -d "sysmlv2-MCP-Server" ]; then
  echo "Repository already exists, updating..."
  cd sysmlv2-MCP-Server
  git pull
else
  echo "Cloning repository..."
  git clone https://github.com/Paul-MHP/sysmlv2-MCP-Server.git
  cd sysmlv2-MCP-Server
fi

echo "Building .NET project..."
dotnet build --configuration Release --verbosity minimal

if [ $? -eq 0 ]; then
  echo "✅ Project built successfully"
else
  echo "❌ Build failed"
  exit 1
fi

# Step 6: Deploy to Azure
echo ""
echo "🚀 Step 6: Deploying to Azure Functions..."
echo "This may take a few minutes..."

func azure functionapp publish "$FUNCTION_APP" --dotnet-isolated --force

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