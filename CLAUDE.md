# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **SysML v2 MCP (Model Context Protocol) Server** that runs as an Azure Function. It acts as a bridge between AI assistants (like Claude) and SysML v2 API systems, allowing AI to interact with SysML v2 projects and model elements through standardized MCP tools.

### Architecture
```
Claude Client ←→ MCP Protocol ←→ Azure Functions ←→ SysML v2 REST API
```

The server translates MCP tool calls into SysML API requests and formats responses for AI consumption.

## Common Development Commands

### Local Development
```bash
# Start the function app locally
func start

# Build the project
dotnet build

# Build for release
dotnet build --configuration Release
```

### Testing
```bash
# Test MCP protocol implementation
node test-mcp-protocol.js

# Test the deployed function (replace URL with actual deployment)
curl -X POST https://sysmlv2-mcp-server.azurewebsites.net/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":"1","method":"tools/list"}'
```

### Deployment
```bash
# Deploy to Azure Function App
func azure functionapp publish sysmlv2-mcp-server

# Deploy using Azure CLI
az functionapp create --resource-group sysmlv2-mcp-rg \
  --consumption-plan-location "East US" \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --name sysmlv2-mcp-server \
  --storage-account sysmlv2mcpstorage

# One-command deployment using cloud shell script
./deploy-azure-shell.sh
```

## Code Architecture

### Core Components

1. **Program.cs** - Azure Function startup configuration
   - Configures dependency injection for `HttpClient` and `SysMLApiService`
   - Sets up the function application

2. **Functions/MCPServerFunction.cs** - Main MCP protocol handler
   - Handles HTTP POST requests to `/api/mcp` endpoint
   - Implements MCP JSON-RPC 2.0 protocol
   - Routes method calls: `tools/list` and `tools/call`
   - Provides 6 MCP tools for SysML operations

3. **Services/SysMLApiService.cs** - SysML v2 API client
   - Handles all HTTP communication with SysML v2 API
   - Base URL configured via `SYSML_API_BASE_URL` environment variable
   - Default: `https://sysml-api-webapp-2024.azurewebsites.net`

4. **Models/** - Data models
   - `MCPModels.cs` - MCP protocol data structures
   - `SysMLModels.cs` - SysML v2 API data structures

### Available MCP Tools

The server provides these tools to AI assistants:

1. **list_projects** - List all SysML v2 projects
2. **get_project** - Get details of a specific project (requires: projectId)  
3. **create_project** - Create new project (requires: name, description)
4. **delete_project** - Delete project (requires: projectId)
5. **list_elements** - List elements in project (requires: projectId, optional: commitId)
6. **get_element** - Get element details (requires: projectId, elementId, optional: commitId)

### Configuration

**Environment Variables:**
- `SYSML_API_BASE_URL` - Base URL for SysML v2 API server (required)
- `AzureWebJobsStorage` - Azure storage connection string (auto-configured)
- `FUNCTIONS_WORKER_RUNTIME` - Set to `dotnet-isolated` (auto-configured)

**Local Settings (local.settings.json):**
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SYSML_API_BASE_URL": "https://sysml-api-webapp-2024.azurewebsites.net"
  }
}
```

## Important Development Notes

### MCP Protocol Implementation
- Uses JSON-RPC 2.0 specification
- All requests must include `jsonrpc: "2.0"` field
- Support for both synchronous tool calls and tool listing
- Error handling follows MCP error response format

### Error Handling Patterns
- Network errors are caught and returned as MCP error responses
- API failures from SysML service are passed through as tool execution errors
- Invalid request formats return appropriate HTTP 400 with MCP error structure
- All exceptions include descriptive messages for debugging

### Testing Approach
- **test-mcp-protocol.js** - Validates MCP protocol structure without external dependencies
- Direct HTTP testing using curl commands for integration testing
- No formal test framework - relies on manual testing and curl scripts

### Deployment Scripts
- **deploy-azure-shell.sh** - Complete Azure Cloud Shell deployment automation
- **azure.yaml** - Azure Developer CLI configuration for azd deployment
- **DEPLOYMENT.md** & **QUICK_DEPLOY.md** - Comprehensive deployment guides

### Claude Integration
The **CLAUDE_INTEGRATION.md** file contains detailed instructions for connecting this MCP server to Claude Desktop. The integration allows Claude to use natural language commands like "List all SysML projects" or "Create a new project called 'Vehicle System'".

## File Structure Patterns

- **Functions/** - Azure Function endpoints
- **Services/** - Business logic and external API clients  
- **Models/** - Data transfer objects and API models
- **deploy-\*.sh** - Deployment automation scripts
- **\*.md** - Documentation (deployment, integration, usage guides)