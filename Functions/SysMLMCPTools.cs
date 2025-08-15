using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using SysMLMCPServer.Services;
using System.ComponentModel;

namespace SysMLMCPServer.Functions;

public class SysMLMCPTools
{
    private readonly ILogger<SysMLMCPTools> _logger;
    private readonly SysMLApiService _sysmlService;

    public SysMLMCPTools(ILogger<SysMLMCPTools> logger, SysMLApiService sysmlService)
    {
        _logger = logger;
        _sysmlService = sysmlService;
    }

    [Function(nameof(ListProjects))]
    [Description("List all SysML v2 projects")]
    public async Task<object> ListProjects(
        [McpToolTrigger("list_projects", "List all SysML v2 projects")] ToolInvocationContext context)
    {
        try
        {
            _logger.LogInformation("Listing SysML v2 projects");
            var projects = await _sysmlService.GetProjectsAsync();
            return new { projects, count = projects.Count };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing projects");
            throw;
        }
    }

    [Function(nameof(GetProject))]
    [Description("Get details of a specific SysML v2 project")]
    public async Task<object> GetProject(
        [McpToolTrigger("get_project", "Get details of a specific SysML v2 project")] ToolInvocationContext context)
    {
        try
        {
            var projectId = context.Arguments["projectId"]?.ToString();
            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException("ProjectId is required");
            }

            _logger.LogInformation("Getting project: {ProjectId}", projectId);
            var project = await _sysmlService.GetProjectAsync(projectId);
            
            if (project == null)
            {
                throw new InvalidOperationException($"Project not found: {projectId}");
            }

            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project");
            throw;
        }
    }

    [Function(nameof(CreateProject))]
    [Description("Create a new SysML v2 project")]
    public async Task<object> CreateProject(
        [McpToolTrigger("create_project", "Create a new SysML v2 project")] ToolInvocationContext context)
    {
        try
        {
            var name = context.Arguments["name"]?.ToString();
            var description = context.Arguments["description"]?.ToString();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
            {
                throw new ArgumentException("Name and description are required");
            }

            _logger.LogInformation("Creating project: {Name}", name);
            var project = await _sysmlService.CreateProjectAsync(name, description);
            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            throw;
        }
    }

    [Function(nameof(DeleteProject))]
    [Description("Delete a SysML v2 project")]
    public async Task<object> DeleteProject(
        [McpToolTrigger("delete_project", "Delete a SysML v2 project")] ToolInvocationContext context)
    {
        try
        {
            var projectId = context.Arguments["projectId"]?.ToString();
            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException("ProjectId is required");
            }

            _logger.LogInformation("Deleting project: {ProjectId}", projectId);
            var success = await _sysmlService.DeleteProjectAsync(projectId);
            return new { success, projectId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project");
            throw;
        }
    }

    [Function(nameof(ListElements))]
    [Description("List elements in a SysML v2 project")]
    public async Task<object> ListElements(
        [McpToolTrigger("list_elements", "List elements in a SysML v2 project")] ToolInvocationContext context)
    {
        try
        {
            var projectId = context.Arguments["projectId"]?.ToString();
            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException("ProjectId is required");
            }

            var commitId = context.Arguments["commitId"]?.ToString();

            _logger.LogInformation("Listing elements for project: {ProjectId}", projectId);
            var elements = await _sysmlService.GetElementsAsync(projectId, commitId);
            return new { elements, count = elements.Count, projectId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing elements");
            throw;
        }
    }

    [Function(nameof(GetElement))]
    [Description("Get details of a specific element in a SysML v2 project")]
    public async Task<object> GetElement(
        [McpToolTrigger("get_element", "Get details of a specific element in a SysML v2 project")] ToolInvocationContext context)
    {
        try
        {
            var projectId = context.Arguments["projectId"]?.ToString();
            var elementId = context.Arguments["elementId"]?.ToString();

            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(elementId))
            {
                throw new ArgumentException("ProjectId and ElementId are required");
            }

            var commitId = context.Arguments["commitId"]?.ToString();

            _logger.LogInformation("Getting element: {ElementId} from project: {ProjectId}", elementId, projectId);
            var element = await _sysmlService.GetElementAsync(projectId, elementId, commitId);
            
            if (element == null)
            {
                throw new InvalidOperationException($"Element not found: {elementId} in project {projectId}");
            }

            return element;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting element");
            throw;
        }
    }
}