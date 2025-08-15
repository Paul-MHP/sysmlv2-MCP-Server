# Claude Integration Guide

This guide explains how to integrate the SysML v2 MCP Server with Claude Desktop to enable AI-powered SysML v2 operations.

## Overview

The Model Context Protocol (MCP) allows Claude to interact with external systems through standardized tools. This integration enables Claude to:

- List and manage SysML v2 projects
- Browse model elements and their relationships
- Create new projects and elements
- Query specific commits and versions

## Prerequisites

- Claude Desktop application installed
- SysML v2 MCP Server deployed to Azure Functions
- Admin access to Claude Desktop configuration

## Integration Methods

### Method 1: Direct HTTP Integration (Recommended)

This method configures Claude to make direct HTTP requests to your Azure Function.

#### Step 1: Locate Claude Configuration File

**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
**Linux**: `~/.config/Claude/claude_desktop_config.json`

#### Step 2: Add MCP Server Configuration

Add this configuration to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "sysmlv2": {
      "command": "node",
      "args": ["-e", "
        const https = require('https');
        const readline = require('readline');
        
        const rl = readline.createInterface({
          input: process.stdin,
          output: process.stdout
        });
        
        rl.on('line', (input) => {
          const options = {
            hostname: 'sysmlv2-mcp-server.azurewebsites.net',
            port: 443,
            path: '/api/mcp',
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'Content-Length': Buffer.byteLength(input)
            }
          };
          
          const req = https.request(options, (res) => {
            res.on('data', (chunk) => {
              console.log(chunk.toString());
            });
          });
          
          req.on('error', (e) => {
            console.error(JSON.stringify({
              jsonrpc: '2.0',
              error: { code: -1, message: e.message }
            }));
          });
          
          req.write(input);
          req.end();
        });
      "]
    }
  }
}
```

#### Step 3: Restart Claude Desktop

Close and reopen Claude Desktop to load the new MCP server configuration.

### Method 2: MCP Proxy Server

For more complex scenarios, you can create a local proxy server:

#### Step 1: Create Proxy Script

Create `sysml-mcp-proxy.js`:

```javascript
#!/usr/bin/env node

const https = require('https');
const readline = require('readline');

const AZURE_FUNCTION_URL = 'https://sysmlv2-mcp-server.azurewebsites.net/api/mcp';

const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

rl.on('line', async (input) => {
  try {
    const data = JSON.stringify(JSON.parse(input));
    
    const options = {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(data)
      }
    };

    const req = https.request(AZURE_FUNCTION_URL, options, (res) => {
      let responseData = '';
      
      res.on('data', (chunk) => {
        responseData += chunk;
      });
      
      res.on('end', () => {
        console.log(responseData);
      });
    });

    req.on('error', (error) => {
      console.error(JSON.stringify({
        jsonrpc: '2.0',
        error: { code: -1, message: error.message }
      }));
    });

    req.write(data);
    req.end();
    
  } catch (error) {
    console.error(JSON.stringify({
      jsonrpc: '2.0',
      error: { code: -32700, message: 'Parse error' }
    }));
  }
});
```

#### Step 2: Configure Claude

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "sysmlv2": {
      "command": "node",
      "args": ["/path/to/sysml-mcp-proxy.js"]
    }
  }
}
```

## Testing the Integration

### Step 1: Verify MCP Server is Running

Test your Azure Function directly:

```bash
curl -X POST https://sysmlv2-mcp-server.azurewebsites.net/api/mcp \
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

### Step 2: Test in Claude Desktop

Open Claude Desktop and try these commands:

1. **List available tools**:
   ```
   What SysML tools do you have available?
   ```

2. **List projects**:
   ```
   Show me all SysML v2 projects
   ```

3. **Create a project**:
   ```
   Create a new SysML project called "Vehicle Model" with description "Automotive system model"
   ```

4. **Get project details**:
   ```
   Show me details for project [project-id]
   ```

## Usage Examples

### Project Management

```text
User: Create a new SysML project for a drone system

Claude: I'll create a new SysML project for your drone system using the create_project tool.

[Tool execution: create_project with name="Drone System" and description="UAV system model"]

Project created successfully! Here are the details:
- Project ID: abc123-def456-ghi789
- Name: Drone System  
- Description: UAV system model
- Created: 2024-01-15T10:30:00Z
- Default Branch: main-branch-id
```

### Element Browsing

```text
User: What elements are in my Vehicle Model project?

Claude: I'll list the elements in your Vehicle Model project.

[Tool execution: list_elements with projectId="vehicle-model-id"]

Found 15 elements in the Vehicle Model project:
- Engine (PartUsage)
- Transmission (PartUsage) 
- Wheel (PartUsage)
- BrakingSystem (PartUsage)
- ...
```

### Version Control

```text
User: Show me elements from commit abc123 in the Vehicle Model

Claude: I'll retrieve the elements from that specific commit.

[Tool execution: list_elements with projectId="vehicle-model-id" and commitId="abc123"]

Elements from commit abc123:
- 12 elements total
- Last modified: 2024-01-10T15:45:00Z
- [Element details...]
```

## Advanced Configuration

### Custom Function App URL

If you deployed to a custom domain:

```json
{
  "mcpServers": {
    "sysmlv2": {
      "command": "node",
      "args": ["-e", "/* Update hostname to your custom domain */"]
    }
  }
}
```

### Authentication (Future)

For authenticated scenarios, modify the proxy to include authentication headers:

```javascript
const options = {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer YOUR_TOKEN',
    'Content-Length': Buffer.byteLength(data)
  }
};
```

### Error Handling

The integration includes comprehensive error handling:

- **Network errors**: Returned as MCP error responses
- **API errors**: Passed through from SysML API
- **Tool errors**: Formatted as MCP tool execution errors

## Troubleshooting

### Claude Can't Find MCP Server

1. **Check configuration file location**:
   ```bash
   # macOS
   ls -la ~/Library/Application\ Support/Claude/
   
   # Windows
   dir %APPDATA%\Claude\
   ```

2. **Validate JSON syntax**:
   ```bash
   cat claude_desktop_config.json | jq .
   ```

3. **Check Claude Desktop logs**:
   - Look for MCP server startup messages
   - Check for configuration errors

### Network Connection Issues

1. **Test Azure Function**:
   ```bash
   curl -v https://sysmlv2-mcp-server.azurewebsites.net/api/mcp
   ```

2. **Check CORS settings**:
   ```bash
   az functionapp cors show \
     --name sysmlv2-mcp-server \
     --resource-group sysmlv2-mcp-rg
   ```

3. **Verify function app status**:
   ```bash
   az functionapp show \
     --name sysmlv2-mcp-server \
     --resource-group sysmlv2-mcp-rg \
     --query state
   ```

### Tool Execution Errors

1. **Check SysML API connectivity**:
   ```bash
   curl https://sysml-api-webapp-2024.azurewebsites.net/projects
   ```

2. **Review function logs**:
   ```bash
   az webapp log tail \
     --name sysmlv2-mcp-server \
     --resource-group sysmlv2-mcp-rg
   ```

3. **Test individual tools**:
   ```bash
   curl -X POST https://sysmlv2-mcp-server.azurewebsites.net/api/mcp \
     -H "Content-Type: application/json" \
     -d '{
       "jsonrpc": "2.0",
       "id": "1",
       "method": "tools/call",
       "params": {
         "name": "list_projects",
         "arguments": {}
       }
     }'
   ```

## Best Practices

1. **Keep URLs updated**: Ensure Azure Function URL matches your deployment
2. **Monitor usage**: Use Application Insights to track MCP server usage
3. **Error handling**: Implement robust error handling in custom proxy scripts
4. **Security**: Consider authentication for production deployments
5. **Performance**: Monitor response times and optimize for frequent operations

## Support

For issues with:
- **MCP Protocol**: Check [MCP Specification](https://spec.modelcontextprotocol.io/)
- **Claude Desktop**: Consult Claude Desktop documentation
- **Azure Functions**: Review Azure Functions troubleshooting guides
- **SysML API**: Check SysML API server logs and documentation