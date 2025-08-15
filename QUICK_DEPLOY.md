# 🚀 Quick Deploy Guide - Azure Cloud Shell

Deploy the SysML v2 MCP Server in under 5 minutes using Azure Cloud Shell!

## Step 1: Open Azure Cloud Shell
1. Go to [Azure Portal](https://portal.azure.com)
2. Click the **Cloud Shell** icon (>_) in the top menu
3. Choose **Bash** when prompted
4. Wait for initialization

## Step 2: Deploy with One Command

Copy and paste this single command into Azure Cloud Shell:

```bash
curl -sSL https://raw.githubusercontent.com/Paul-MCP/sysmlv2-MCP-Server/main/deploy-azure-shell.sh | bash
```

**Alternative: Manual Clone & Deploy**
```bash
# Clone the repository
git clone https://github.com/Paul-MHP/sysmlv2-MCP-Server.git
cd sysmlv2-MCP-Server

# Run deployment script
chmod +x deploy-azure-shell.sh
./deploy-azure-shell.sh
```

## Step 3: Wait for Deployment
The script will:
- ✅ Create resource group and storage account
- ✅ Create Azure Function App with .NET 8
- ✅ Configure environment variables and CORS
- ✅ Build and deploy your MCP server
- ✅ Provide testing instructions

**Expected time: 3-5 minutes**

## Step 4: Test Your Deployment

After deployment, you'll get a test command like:
```bash
curl -X POST https://sysmlv2-mcp-server-XXXXX.azurewebsites.net/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":"1","method":"tools/list"}'
```

**Expected Response:**
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "result": {
    "tools": [
      {"name": "list_projects", "description": "List all SysML v2 projects"},
      {"name": "get_project", "description": "Get details of a specific SysML v2 project"},
      {"name": "create_project", "description": "Create a new SysML v2 project"},
      {"name": "delete_project", "description": "Delete a SysML v2 project"},
      {"name": "list_elements", "description": "List elements in a SysML v2 project"},
      {"name": "get_element", "description": "Get details of a specific element"}
    ]
  }
}
```

## Step 5: Connect to Claude

Update your Claude Desktop configuration with your new endpoint:

**File Location:**
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

**Configuration:**
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
            hostname: 'YOUR-FUNCTION-APP.azurewebsites.net',
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

Replace `YOUR-FUNCTION-APP` with your actual function app name from the deployment output.

## ✅ You're Done!

Try these commands in Claude:
- *"List all SysML projects"*
- *"Create a new project called 'Test System'"*
- *"Show me the elements in project XYZ"*

## 🔧 Troubleshooting

**Deployment Issues:**
```bash
# Check deployment status
az functionapp show --name YOUR-FUNCTION-APP --resource-group sysmlv2-mcp-rg

# View function logs
az functionapp logs tail --name YOUR-FUNCTION-APP --resource-group sysmlv2-mcp-rg
```

**Testing Issues:**
```bash
# Test SysML API connectivity
curl https://sysml-api-webapp-2024.azurewebsites.net/projects

# Test individual MCP tools
curl -X POST https://YOUR-FUNCTION-APP.azurewebsites.net/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":"1","method":"tools/call","params":{"name":"list_projects","arguments":{}}}'
```

## 🗑️ Cleanup

To remove all deployed resources:
```bash
az group delete --name sysmlv2-mcp-rg --yes --no-wait
```

## 📚 Need Help?

- **Full Documentation**: See `DEPLOYMENT.md` for detailed explanations
- **Claude Integration**: See `CLAUDE_INTEGRATION.md` for advanced setup
- **Issues**: Create an issue on the GitHub repository