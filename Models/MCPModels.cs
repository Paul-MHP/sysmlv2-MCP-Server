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
    public object? Result { get; init; }
    
    [JsonPropertyName("error")]
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