#!/usr/bin/env node

const http = require('http');
const https = require('https');

// Your MCP server URL
const MCP_SERVER_URL = 'https://sysmlv2-mcp-server-70764.azurewebsites.net/api/mcp?auth=false';

// Simple HTTP client to proxy requests
function makeRequest(data) {
    return new Promise((resolve, reject) => {
        const postData = JSON.stringify(data);
        
        const options = {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': Buffer.byteLength(postData)
            }
        };

        const req = https.request(MCP_SERVER_URL, options, (res) => {
            let responseData = '';
            
            res.on('data', (chunk) => {
                responseData += chunk;
            });
            
            res.on('end', () => {
                try {
                    const parsedData = JSON.parse(responseData);
                    resolve(parsedData);
                } catch (error) {
                    reject(new Error('Invalid JSON response: ' + responseData));
                }
            });
        });

        req.on('error', (error) => {
            reject(error);
        });

        req.write(postData);
        req.end();
    });
}

// Handle stdin/stdout communication with Claude Desktop
process.stdin.setEncoding('utf8');

let buffer = '';

process.stdin.on('data', async (chunk) => {
    buffer += chunk;
    
    // Look for complete JSON messages (newline separated)
    const lines = buffer.split('\n');
    buffer = lines.pop(); // Keep incomplete line in buffer
    
    for (const line of lines) {
        if (line.trim()) {
            try {
                const request = JSON.parse(line);
                const response = await makeRequest(request);
                
                // Send response back to Claude Desktop
                process.stdout.write(JSON.stringify(response) + '\n');
            } catch (error) {
                // Send error response
                const errorResponse = {
                    jsonrpc: "2.0",
                    id: null,
                    error: {
                        code: -32603,
                        message: error.message
                    }
                };
                process.stdout.write(JSON.stringify(errorResponse) + '\n');
            }
        }
    }
});

process.stdin.on('end', () => {
    process.exit(0);
});

// Handle process termination
process.on('SIGINT', () => {
    process.exit(0);
});

process.on('SIGTERM', () => {
    process.exit(0);
});