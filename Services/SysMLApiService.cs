using System.Text;
using System.Text.Json;
using SysMLMCPServer.Models;

namespace SysMLMCPServer.Services;

public class SysMLApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public SysMLApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _baseUrl = Environment.GetEnvironmentVariable("SYSML_API_BASE_URL") 
                   ?? "https://sysml-api-webapp-2024.azurewebsites.net";
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<List<SysMLProject>> GetProjectsAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/projects");
            return JsonSerializer.Deserialize<List<SysMLProject>>(response, _jsonOptions) ?? new List<SysMLProject>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get projects: {ex.Message}", ex);
        }
    }

    public async Task<SysMLProject?> GetProjectAsync(string projectId)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/projects/{projectId}");
            return JsonSerializer.Deserialize<SysMLProject>(response, _jsonOptions);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get project {projectId}: {ex.Message}", ex);
        }
    }

    public async Task<SysMLProject> CreateProjectAsync(string name, string description)
    {
        try
        {
            var request = new CreateProjectRequest
            {
                Name = name,
                Description = description
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/projects", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SysMLProject>(responseJson, _jsonOptions) 
                   ?? throw new Exception("Failed to deserialize created project");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create project: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteProjectAsync(string projectId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/projects/{projectId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete project {projectId}: {ex.Message}", ex);
        }
    }

    public async Task<List<SysMLElement>> GetElementsAsync(string projectId, string? commitId = null)
    {
        try
        {
            var url = commitId != null 
                ? $"{_baseUrl}/projects/{projectId}/commits/{commitId}/elements"
                : $"{_baseUrl}/projects/{projectId}/elements";
                
            var response = await _httpClient.GetStringAsync(url);
            return JsonSerializer.Deserialize<List<SysMLElement>>(response, _jsonOptions) ?? new List<SysMLElement>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get elements for project {projectId}: {ex.Message}", ex);
        }
    }

    public async Task<SysMLElement?> GetElementAsync(string projectId, string elementId, string? commitId = null)
    {
        try
        {
            var url = commitId != null
                ? $"{_baseUrl}/projects/{projectId}/commits/{commitId}/elements/{elementId}"
                : $"{_baseUrl}/projects/{projectId}/elements/{elementId}";
                
            var response = await _httpClient.GetStringAsync(url);
            return JsonSerializer.Deserialize<SysMLElement>(response, _jsonOptions);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get element {elementId}: {ex.Message}", ex);
        }
    }

    public async Task<List<SysMLCommit>> GetCommitsAsync(string projectId)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/projects/{projectId}/commits");
            return JsonSerializer.Deserialize<List<SysMLCommit>>(response, _jsonOptions) ?? new List<SysMLCommit>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get commits for project {projectId}: {ex.Message}", ex);
        }
    }

    public async Task<List<SysMLBranch>> GetBranchesAsync(string projectId)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{_baseUrl}/projects/{projectId}/branches");
            return JsonSerializer.Deserialize<List<SysMLBranch>>(response, _jsonOptions) ?? new List<SysMLBranch>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get branches for project {projectId}: {ex.Message}", ex);
        }
    }
}