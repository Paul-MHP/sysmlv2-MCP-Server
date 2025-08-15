#!/usr/bin/env node

const https = require('https');

const SERVER_URL = 'https://sysmlv2-mcp-server-70764.azurewebsites.net/api/mcp?auth=false';

async function forwardRequest(request) {
    return new Promise((resolve, reject) => {
        const postData = JSON.stringify(request);
        
        const options = {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        };

        const req = https.request(SERVER_URL, options, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                try {
                    resolve(JSON.parse(data));
                } catch (e) {
                    reject(new Error('Invalid JSON: ' + data));
                }
            });
        });

        req.on('error', reject);
        req.write(postData);
        req.end();
    });
}

// Process STDIO line by line
let buffer = '';
process.stdin.on('data', async (chunk) => {
    buffer += chunk;
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';
    
    for (const line of lines) {
        if (line.trim()) {
            try {
                const request = JSON.parse(line);
                const response = await forwardRequest(request);
                console.log(JSON.stringify(response));
            } catch (error) {
                const errorResponse = {
                    jsonrpc: "2.0",
                    id: null,
                    error: { code: -32603, message: error.message }
                };
                console.log(JSON.stringify(errorResponse));
            }
        }
    }
});

process.stdin.on('end', () => process.exit(0));