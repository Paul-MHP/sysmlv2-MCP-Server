using System.Text.Json.Serialization;

namespace SysMLMCPServer.Models;

public record SysMLProject
{
    [JsonPropertyName("@id")]
    public string Id { get; init; } = "";
    
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "Project";
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";
    
    [JsonPropertyName("alias")]
    public List<string> Alias { get; init; } = new();
    
    [JsonPropertyName("created")]
    public string Created { get; init; } = "";
    
    [JsonPropertyName("defaultBranch")]
    public SysMLBranch? DefaultBranch { get; init; }
}

public record SysMLBranch
{
    [JsonPropertyName("@id")]
    public string Id { get; init; } = "";
    
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "Branch";
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
}

public record SysMLElement
{
    [JsonPropertyName("@id")]
    public string Id { get; init; } = "";
    
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    
    [JsonPropertyName("qualifiedName")]
    public string QualifiedName { get; init; } = "";
    
    [JsonPropertyName("elementId")]
    public string ElementId { get; init; } = "";
}

public record SysMLCommit
{
    [JsonPropertyName("@id")]
    public string Id { get; init; } = "";
    
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "Commit";
    
    [JsonPropertyName("created")]
    public string Created { get; init; } = "";
    
    [JsonPropertyName("owningProject")]
    public string OwningProject { get; init; } = "";
}

public record CreateProjectRequest
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "Project";
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";
}