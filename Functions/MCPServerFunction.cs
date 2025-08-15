using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SysMLMCPServer.Models;
using SysMLMCPServer.Services;

namespace SysMLMCPServer.Functions;

public class MCPServerFunction
{
    private readonly ILogger<MCPServerFunction> _logger;
    private readonly SysMLApiService _sysmlService;
    private readonly JsonSerializerOptions _jsonOptions;

    public MCPServerFunction(ILogger<MCPServerFunction> logger, SysMLApiService sysmlService)
    {
        _logger = logger;
        _sysmlService = sysmlService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    [Function("mcp")]
    public async Task<HttpResponseData> HandleMCPRequest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mcp")] HttpRequestData req)
    {
        _logger.LogInformation("MCP request received");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var mcpRequest = JsonSerializer.Deserialize<MCPRequest>(requestBody, _jsonOptions);

            if (mcpRequest == null)
            {
                return await CreateErrorResponse(req, -1, "Invalid request format");
            }

            var response = mcpRequest.Method switch
            {
                "initialize" => await HandleInitialize(mcpRequest),
                "tools/list" => await HandleToolsList(mcpRequest),
                "tools/call" => await HandleToolCall(mcpRequest),
                _ => CreateMCPResponse(mcpRequest.Id, null, new MCPError
                {
                    Code = -32601,
                    Message = $"Method not found: {mcpRequest.Method}"
                })
            };

            var responseData = req.CreateResponse(HttpStatusCode.OK);
            responseData.Headers.Add("Content-Type", "application/json");
            await responseData.WriteStringAsync(JsonSerializer.Serialize(response, _jsonOptions));
            return responseData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            return await CreateErrorResponse(req, -1, $"Internal server error: {ex.Message}");
        }
    }

    private async Task<MCPResponse> HandleInitialize(MCPRequest request)
    {
        try
        {
            var initParams = JsonSerializer.Deserialize<MCPInitializeParams>(
                JsonSerializer.Serialize(request.Params), _jsonOptions);

            _logger.LogInformation($"MCP client initialized: {initParams?.ClientInfo?.Name ?? "Unknown"} v{initParams?.ClientInfo?.Version ?? "Unknown"}");

            var result = new MCPInitializeResult
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = new MCPServerCapabilities
                {
                    Tools = new MCPToolsCapability
                    {
                        ListChanged = false
                    }
                },
                ServerInfo = new MCPServerInfo
                {
                    Name = "SysML v2 MCP Server",
                    Version = "1.0.0"
                }
            };

            return CreateMCPResponse(request.Id, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling initialize request");
            return CreateMCPResponse(request.Id, null, new MCPError
            {
                Code = -32603,
                Message = $"Initialize failed: {ex.Message}"
            });
        }
    }

    private async Task<MCPResponse> HandleToolsList(MCPRequest request)
    {
        var tools = new List<MCPTool>
        {
            new()
            {
                Name = "list_projects",
                Description = "List all SysML v2 projects",
                InputSchema = new { type = "object", properties = new { }, required = new string[] { } }
            },
            new()
            {
                Name = "get_project",
                Description = "Get details of a specific SysML v2 project",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        projectId = new { type = "string", description = "The ID of the project to retrieve" }
                    },
                    required = new[] { "projectId" }
                }
            },
            new()
            {
                Name = "create_project",
                Description = "Create a new SysML v2 project",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Name of the project" },
                        description = new { type = "string", description = "Description of the project" }
                    },
                    required = new[] { "name", "description" }
                }
            },
            new()
            {
                Name = "delete_project",
                Description = "Delete a SysML v2 project",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        projectId = new { type = "string", description = "The ID of the project to delete" }
                    },
                    required = new[] { "projectId" }
                }
            },
            new()
            {
                Name = "list_elements",
                Description = "List elements in a SysML v2 project",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        projectId = new { type = "string", description = "The ID of the project" },
                        commitId = new { type = "string", description = "Optional commit ID to get elements from specific commit" }
                    },
                    required = new[] { "projectId" }
                }
            },
            new()
            {
                Name = "get_element",
                Description = "Get details of a specific element in a SysML v2 project",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        projectId = new { type = "string", description = "The ID of the project" },
                        elementId = new { type = "string", description = "The ID of the element" },
                        commitId = new { type = "string", description = "Optional commit ID to get element from specific commit" }
                    },
                    required = new[] { "projectId", "elementId" }
                }
            }
        };

        return CreateMCPResponse(request.Id, new { tools });
    }

    private async Task<MCPResponse> HandleToolCall(MCPRequest request)
    {
        try
        {
            var toolCallParams = JsonSerializer.Deserialize<MCPToolCall>(
                JsonSerializer.Serialize(request.Params), _jsonOptions);

            if (toolCallParams == null)
            {
                return CreateMCPResponse(request.Id, null, new MCPError
                {
                    Code = -32602,
                    Message = "Invalid tool call parameters"
                });
            }

            var result = toolCallParams.Name switch
            {
                "list_projects" => await HandleListProjects(),
                "get_project" => await HandleGetProject(toolCallParams.Arguments),
                "create_project" => await HandleCreateProject(toolCallParams.Arguments),
                "delete_project" => await HandleDeleteProject(toolCallParams.Arguments),
                "list_elements" => await HandleListElements(toolCallParams.Arguments),
                "get_element" => await HandleGetElement(toolCallParams.Arguments),
                _ => new MCPToolResult
                {
                    IsError = true,
                    Content = new List<MCPContent>
                    {
                        new() { Type = "text", Text = $"Unknown tool: {toolCallParams.Name}" }
                    }
                }
            };

            return CreateMCPResponse(request.Id, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool call");
            return CreateMCPResponse(request.Id, new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Tool execution failed: {ex.Message}" }
                }
            });
        }
    }

    private async Task<MCPToolResult> HandleListProjects()
    {
        try
        {
            var projects = await _sysmlService.GetProjectsAsync();
            var projectsJson = JsonSerializer.Serialize(projects, _jsonOptions);

            return new MCPToolResult
            {
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Found {projects.Count} projects:\n{projectsJson}" }
                }
            };
        }
        catch (Exception ex)
        {
            return new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Failed to list projects: {ex.Message}" }
                }
            };
        }
    }

    private async Task<MCPToolResult> HandleGetProject(Dictionary<string, object>? arguments)
    {
        if (arguments == null || !arguments.TryGetValue("projectId", out var projectIdObj))
        {
            return new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = "Missing required parameter: projectId" }
                }
            };
        }

        try
        {
            var projectId = projectIdObj.ToString()!;
            var project = await _sysmlService.GetProjectAsync(projectId);

            if (project == null)
            {
                return new MCPToolResult
                {
                    IsError = true,
                    Content = new List<MCPContent>
                    {
                        new() { Type = "text", Text = $"Project not found: {projectId}" }
                    }
                };
            }

            var projectJson = JsonSerializer.Serialize(project, _jsonOptions);
            return new MCPToolResult
            {
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Project details:\n{projectJson}" }
                }
            };
        }
        catch (Exception ex)
        {
            return new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Failed to get project: {ex.Message}" }
                }
            };
        }
    }

    private async Task<MCPToolResult> HandleCreateProject(Dictionary<string, object>? arguments)
    {
        if (arguments == null || 
            !arguments.TryGetValue("name", out var nameObj) ||
            !arguments.TryGetValue("description", out var descObj))
        {
            return new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = "Missing required parameters: name, description" }
                }
            };
        }

        try
        {
            var name = nameObj.ToString()!;
            var description = descObj.ToString()!;
            var project = await _sysmlService.CreateProjectAsync(name, description);

            var projectJson = JsonSerializer.Serialize(project, _jsonOptions);
            return new MCPToolResult
            {
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Project created successfully:\n{projectJson}" }
                }
            };
        }
        catch (Exception ex)
        {
            return new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Failed to create project: {ex.Message}" }
                }
            };
        }
    }

    private async Task<MCPToolResult> HandleDeleteProject(Dictionary<string, object>? arguments)
    {
        if (arguments == null || !arguments.TryGetValue("projectId", out var projectIdObj))
        {
            return new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = "Missing required parameter: projectId" }
                }
            };
        }

        try
        {
            var projectId = projectIdObj.ToString()!;
            var success = await _sysmlService.DeleteProjectAsync(projectId);

            return new MCPToolResult
            {
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = success 
                        ? $"Project {projectId} deleted successfully" 
                        : $"Failed to delete project {projectId}" }
                }
            };
        }
        catch (Exception ex)
        {
            return new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Failed to delete project: {ex.Message}" }
                }
            };
        }
    }

    private async Task<MCPToolResult> HandleListElements(Dictionary<string, object>? arguments)
    {
        if (arguments == null || !arguments.TryGetValue("projectId", out var projectIdObj))
        {
            return new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = "Missing required parameter: projectId" }
                }
            };
        }

        try
        {
            var projectId = projectIdObj.ToString()!;
            arguments.TryGetValue("commitId", out var commitIdObj);
            var commitId = commitIdObj?.ToString();

            var elements = await _sysmlService.GetElementsAsync(projectId, commitId);
            var elementsJson = JsonSerializer.Serialize(elements, _jsonOptions);

            return new MCPToolResult
            {
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Found {elements.Count} elements in project {projectId}:\n{elementsJson}" }
                }
            };
        }
        catch (Exception ex)
        {
            return new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Failed to list elements: {ex.Message}" }
                }
            };
        }
    }

    private async Task<MCPToolResult> HandleGetElement(Dictionary<string, object>? arguments)
    {
        if (arguments == null || 
            !arguments.TryGetValue("projectId", out var projectIdObj) ||
            !arguments.TryGetValue("elementId", out var elementIdObj))
        {
            return new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = "Missing required parameters: projectId, elementId" }
                }
            };
        }

        try
        {
            var projectId = projectIdObj.ToString()!;
            var elementId = elementIdObj.ToString()!;
            arguments.TryGetValue("commitId", out var commitIdObj);
            var commitId = commitIdObj?.ToString();

            var element = await _sysmlService.GetElementAsync(projectId, elementId, commitId);

            if (element == null)
            {
                return new MCPToolResult
                {
                    IsError = true,
                    Content = new List<MCPContent>
                    {
                        new() { Type = "text", Text = $"Element not found: {elementId} in project {projectId}" }
                    }
                };
            }

            var elementJson = JsonSerializer.Serialize(element, _jsonOptions);
            return new MCPToolResult
            {
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Element details:\n{elementJson}" }
                }
            };
        }
        catch (Exception ex)
        {
            return new MCPToolResult
            {
                IsError = true,
                Content = new List<MCPContent>
                {
                    new() { Type = "text", Text = $"Failed to get element: {ex.Message}" }
                }
            };
        }
    }

    private MCPResponse CreateMCPResponse(string? id, object? result = null, MCPError? error = null)
    {
        return new MCPResponse
        {
            Id = id,
            Result = result,
            Error = error
        };
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, int code, string message)
    {
        var error = new MCPResponse
        {
            Error = new MCPError { Code = code, Message = message }
        };

        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(error, _jsonOptions));
        return response;
    }
}