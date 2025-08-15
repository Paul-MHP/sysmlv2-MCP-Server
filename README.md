# SysML v2 MCP Server

A Model Context Protocol (MCP) server that provides AI assistants with access to SysML v2 API functionality through Azure Functions.

## Features

- **Project Management**: List, get, create, and delete SysML v2 projects
- **Element Access**: Browse and retrieve SysML v2 model elements
- **Version Control**: Access elements from specific commits
- **MCP Protocol**: Full compatibility with Claude and other MCP clients
- **Azure Functions**: Serverless deployment for scalability

## Available MCP Tools

### Project Operations
- `list_projects` - List all SysML v2 projects
- `get_project` - Get details of a specific project
- `create_project` - Create a new project
- `delete_project` - Delete an existing project

### Element Operations  
- `list_elements` - List elements in a project (optionally from specific commit)
- `get_element` - Get details of a specific element

## Local Development

### Prerequisites
- .NET 8.0 SDK
- Azure Functions Core Tools
- SysML v2 API server running

### Setup
1. Clone the repository
2. Configure the SysML API base URL in `local.settings.json`
3. Run the function app:
   ```bash
   func start
   ```

### Testing
Send MCP requests to `http://localhost:7071/api/mcp`:

```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "tools/list"
}
```

## Azure Deployment

### Using Azure CLI
1. Login to Azure: `az login`
2. Create resource group: 
   ```bash
   az group create --name sysmlv2-mcp-rg --location "East US"
   ```
3. Deploy function app:
   ```bash
   az functionapp create --resource-group sysmlv2-mcp-rg \
     --consumption-plan-location "East US" \
     --runtime dotnet \
     --runtime-version 8 \
     --functions-version 4 \
     --name sysmlv2-mcp-server \
     --storage-account sysmlv2mcpstorage
   ```
4. Deploy code:
   ```bash
   func azure functionapp publish sysmlv2-mcp-server
   ```

### Using Azure Developer CLI
1. Initialize: `azd init`
2. Deploy: `azd deploy`

### Environment Variables
Set these in Azure Function App settings:
- `SYSML_API_BASE_URL`: Base URL of your SysML v2 API server

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