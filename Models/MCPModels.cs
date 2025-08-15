using System.Text.Json.Serialization;

namespace SysMLMCPServer.Models;

public record MCPRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";
    
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    
    [JsonPropertyName("method")]
    public string Method { get; init; } = "";
    
    [JsonPropertyName("params")]
    public object? Params { get; init; }
}

public record MCPResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";
    
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }
    
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MCPError? Error { get; init; }
}

public record MCPError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }
    
    [JsonPropertyName("message")]
    public string Message { get; init; } = "";
    
    [JsonPropertyName("data")]
    public object? Data { get; init; }
}

public record MCPTool
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";
    
    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; init; } = new();
}

public record MCPToolCall
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    
    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; init; }
}

public record MCPToolResult
{
    [JsonPropertyName("content")]
    public List<MCPContent> Content { get; init; } = new();
    
    [JsonPropertyName("isError")]
    public bool IsError { get; init; } = false;
}

public record MCPContent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";
    
    [JsonPropertyName("text")]
    public string Text { get; init; } = "";
}

// MCP Initialize Protocol Models
public record MCPInitializeParams
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; init; } = "";
    
    [JsonPropertyName("capabilities")]
    public MCPCapabilities Capabilities { get; init; } = new();
    
    [JsonPropertyName("clientInfo")]
    public MCPClientInfo ClientInfo { get; init; } = new();
}

public record MCPCapabilities
{
    [JsonPropertyName("tools")]
    public object? Tools { get; init; }
    
    [JsonPropertyName("resources")]
    public object? Resources { get; init; }
    
    [JsonPropertyName("prompts")]
    public object? Prompts { get; init; }
}

public record MCPClientInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    
    [JsonPropertyName("version")]
    public string Version { get; init; } = "";
}

public record MCPInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; init; } = "2024-11-05";
    
    [JsonPropertyName("capabilities")]
    public MCPServerCapabilities Capabilities { get; init; } = new();
    
    [JsonPropertyName("serverInfo")]
    public MCPServerInfo ServerInfo { get; init; } = new();
}

public record MCPServerCapabilities
{
    [JsonPropertyName("tools")]
    public MCPToolsCapability Tools { get; init; } = new();
}

public record MCPToolsCapability
{
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; init; } = false;
}

public record MCPServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "SysML v2 MCP Server";
    
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0.0";
}

// OAuth Models
public record OAuthTokenRequest
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; init; } = "";
    
    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = "";
    
    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; init; } = "";
    
    [JsonPropertyName("code")]
    public string? Code { get; init; }
    
    [JsonPropertyName("redirect_uri")]
    public string? RedirectUri { get; init; }
}

public record OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = "";
    
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";
    
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; } = 3600;
    
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }
}

public record OAuthErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = "";
    
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }
}

public record OAuthMetadata
{
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; init; } = "";
    
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; init; } = "";
    
    [JsonPropertyName("response_types_supported")]
    public string[] ResponseTypesSupported { get; init; } = new[] { "code" };
    
    [JsonPropertyName("grant_types_supported")]
    public string[] GrantTypesSupported { get; init; } = new[] { "authorization_code", "client_credentials" };
    
    [JsonPropertyName("scopes_supported")]
    public string[] ScopesSupported { get; init; } = new[] { "mcp:read", "mcp:write" };
}