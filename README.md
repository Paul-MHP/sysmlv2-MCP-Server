# SysML v2 MCP Server

A **Model Context Protocol (MCP) Server** that provides AI assistants with access to SysML v2 API systems through standardized MCP tools. Deployed as an Azure Function for scalable, serverless operation.

## Architecture

```
Claude Client ←→ MCP Protocol ←→ Azure Functions ←→ SysML v2 REST API
```

The server translates MCP tool calls into SysML API requests and formats responses for AI consumption.

## Available MCP Tools

The server provides these tools to AI assistants:

1. **list_projects** - List all SysML v2 projects
2. **get_project** - Get details of a specific project (requires: projectId)  
3. **create_project** - Create new project (requires: name, description)
4. **delete_project** - Delete project (requires: projectId)
5. **list_elements** - List elements in project (requires: projectId, optional: commitId)
6. **get_element** - Get element details (requires: projectId, elementId, optional: commitId)

## Quick Start

### Prerequisites

- Azure subscription with permission to create Function Apps
- Access to Azure Cloud Shell (recommended) or Azure CLI locally
- SysML v2 API endpoint (default: `https://sysml-api-webapp-2024.azurewebsites.net`)

### Deployment

1. **Open Azure Cloud Shell** at https://shell.azure.com

2. **Run the deployment script**:
   ```bash
   # Set configuration variables
   RESOURCE_GROUP="287013_Scalable_AI_Applications"  # Change to your resource group
   LOCATION="westeurope"
   STORAGE_ACCOUNT="sysmlv2mcpstorage$(date +%s | tail -c 6)"
   FUNCTION_APP="sysmlv2-mcp-server-$(date +%s | tail -c 6)"
   SYSML_API_URL="https://sysml-api-webapp-2024.azurewebsites.net"

   # Create Azure resources
   echo "🚀 Creating Azure resources..."
   az storage account create --name "$STORAGE_ACCOUNT" --resource-group "$RESOURCE_GROUP" --location "$LOCATION" --sku "Standard_LRS" --kind "StorageV2"
   az functionapp create --resource-group "$RESOURCE_GROUP" --consumption-plan-location "$LOCATION" --runtime "dotnet-isolated" --runtime-version "8" --functions-version "4" --name "$FUNCTION_APP" --storage-account "$STORAGE_ACCOUNT" --disable-app-insights false
   az functionapp config appsettings set --name "$FUNCTION_APP" --resource-group "$RESOURCE_GROUP" --settings "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" "FUNCTIONS_EXTENSION_VERSION=~4" "DOTNET_VERSION=8.0" "SYSML_API_BASE_URL=$SYSML_API_URL"
   az functionapp cors add --name "$FUNCTION_APP" --resource-group "$RESOURCE_GROUP" --allowed-origins "*"

   # Deploy the code
   echo "📦 Deploying function code..."
   git clone https://github.com/Paul-MHP/sysmlv2-MCP-Server.git
   cd sysmlv2-MCP-Server
   dotnet build --configuration Release
   func azure functionapp publish $FUNCTION_APP --dotnet-isolated --force

   echo "✅ Deployment complete!"
   echo "MCP Endpoint: https://$FUNCTION_APP.azurewebsites.net/api/mcp"
   ```

3. **Test the deployment**:
   ```bash
   # Test tools list
   curl -X POST https://YOUR-FUNCTION-APP.azurewebsites.net/api/mcp \
     -H "Content-Type: application/json" \
     -d '{"jsonrpc":"2.0","id":"1","method":"tools/list"}'

   # Test list projects
   curl -X POST https://YOUR-FUNCTION-APP.azurewebsites.net/api/mcp \
     -H "Content-Type: application/json" \
     -d '{"jsonrpc":"2.0","id":"2","method":"tools/call","params":{"name":"list_projects","arguments":{}}}'
   ```

## Local Development

### Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools v4
- Visual Studio Code (recommended)

### Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/Paul-MHP/sysmlv2-MCP-Server.git
   cd sysmlv2-MCP-Server
   ```

2. **Configure local settings**:
   ```json
   // local.settings.json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "SYSML_API_BASE_URL": "https://sysml-api-webapp-2024.azurewebsites.net"
     },
     "Host": {
       "CORS": "*"
     }
   }
   ```

3. **Start locally**:
   ```bash
   func start
   ```

4. **Test locally**:
   ```bash
   curl -X POST http://localhost:7071/api/mcp \
     -H "Content-Type: application/json" \
     -d '{"jsonrpc":"2.0","id":"1","method":"tools/list"}'
   ```

## Configuration

### Environment Variables

- **SYSML_API_BASE_URL** (required) - Base URL for SysML v2 API server
- **FUNCTIONS_WORKER_RUNTIME** - Set to `dotnet-isolated` (auto-configured)
- **FUNCTIONS_EXTENSION_VERSION** - Set to `~4` (auto-configured)
- **AzureWebJobsStorage** - Azure storage connection string (auto-configured)

### CORS

The function is configured to accept requests from any origin (`*`). For production, consider restricting this to specific domains.

## Claude Integration

### Step 1: Configure MCP Server
Add to your Claude Desktop configuration file:

**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "sysmlv2": {
      "command": "curl",
      "args": [
        "-X", "POST",
        "https://your-function-app.azurewebsites.net/api/mcp",
        "-H", "Content-Type: application/json",
        "-d", "@-"
      ]
    }
  }
}
```

### Step 2: Restart Claude Desktop
Close and reopen Claude Desktop to load the new MCP server.

### Step 3: Test Integration
Try these commands in Claude:
- "List all SysML projects"
- "Create a new project called 'Test Project'"
- "Show me the elements in project XYZ"

## API Configuration

The server connects to the SysML v2 API at:
- **Production**: `https://sysml-api-webapp-2024.azurewebsites.net`
- **Local**: `http://localhost:9000`

Configure via the `SYSML_API_BASE_URL` environment variable.

## Architecture

```
Claude Client ←→ MCP Protocol ←→ Azure Functions ←→ SysML v2 REST API
```

The MCP server acts as a translation layer, converting MCP tool calls into SysML API requests and formatting responses for AI consumption.

## Error Handling

The server provides comprehensive error handling:
- Invalid request formats return appropriate MCP error responses
- API failures are captured and returned as tool execution errors
- All errors include descriptive messages for debugging

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test locally with `func start`
5. Submit a pull request

## License

MIT License - see LICENSE file for details