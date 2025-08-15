using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SysMLMCPServer.Models;

namespace SysMLMCPServer.Functions;

public class OAuthFunction
{
    private readonly ILogger<OAuthFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // In production, these should be stored securely (Azure Key Vault, etc.)
    private static readonly Dictionary<string, string> ValidClients = new()
    {
        { "sysml-mcp-client", "your-client-secret-here" }
    };
    
    private static readonly Dictionary<string, string> AuthorizationCodes = new();
    private static readonly Dictionary<string, (string ClientId, DateTime Expiry)> AccessTokens = new();

    public OAuthFunction(ILogger<OAuthFunction> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    [Function("oauth-metadata")]
    public async Task<HttpResponseData> GetOAuthMetadata(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/oauth-authorization-server")] HttpRequestData req)
    {
        _logger.LogInformation("OAuth metadata request received");

        var baseUrl = $"{req.Url.Scheme}://{req.Url.Host}";
        if (req.Url.Port != 80 && req.Url.Port != 443)
        {
            baseUrl += $":{req.Url.Port}";
        }

        var metadata = new OAuthMetadata
        {
            AuthorizationEndpoint = $"{baseUrl}/api/oauth/authorize",
            TokenEndpoint = $"{baseUrl}/api/oauth/token",
            ResponseTypesSupported = new[] { "code" },
            GrantTypesSupported = new[] { "authorization_code", "client_credentials" },
            ScopesSupported = new[] { "mcp:read", "mcp:write" }
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(metadata, _jsonOptions));
        return response;
    }

    [Function("oauth-authorize")]
    public async Task<HttpResponseData> Authorize(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oauth/authorize")] HttpRequestData req)
    {
        _logger.LogInformation("OAuth authorization request received");

        var queryParams = ParseQueryString(req.Url.Query);
        var clientId = queryParams["client_id"];
        var redirectUri = queryParams["redirect_uri"];
        var responseType = queryParams["response_type"];
        var scope = queryParams["scope"];

        if (string.IsNullOrEmpty(clientId) || !ValidClients.ContainsKey(clientId))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "invalid_client", "Invalid client_id");
        }

        if (responseType != "code")
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "unsupported_response_type", "Only 'code' response type is supported");
        }

        // Generate authorization code
        var authCode = Guid.NewGuid().ToString("N")[..16];
        AuthorizationCodes[authCode] = clientId;

        // In a real implementation, you'd redirect to a login page
        // For MCP testing, we'll auto-approve and redirect
        var redirectUrl = $"{redirectUri}?code={authCode}&state={queryParams["state"]}";
        
        var response = req.CreateResponse(HttpStatusCode.Redirect);
        response.Headers.Add("Location", redirectUrl);
        return response;
    }

    [Function("oauth-token")]
    public async Task<HttpResponseData> Token(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "oauth/token")] HttpRequestData req)
    {
        _logger.LogInformation("OAuth token request received");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var tokenRequest = JsonSerializer.Deserialize<OAuthTokenRequest>(requestBody, _jsonOptions);

            if (tokenRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "invalid_request", "Invalid request format");
            }

            // Validate client credentials
            if (!ValidClients.TryGetValue(tokenRequest.ClientId, out var expectedSecret) || 
                expectedSecret != tokenRequest.ClientSecret)
            {
                return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "invalid_client", "Invalid client credentials");
            }

            string accessToken;

            if (tokenRequest.GrantType == "authorization_code")
            {
                // Validate authorization code
                if (string.IsNullOrEmpty(tokenRequest.Code) || 
                    !AuthorizationCodes.TryGetValue(tokenRequest.Code, out var codeClientId) ||
                    codeClientId != tokenRequest.ClientId)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "invalid_grant", "Invalid authorization code");
                }

                // Remove used authorization code
                AuthorizationCodes.Remove(tokenRequest.Code);
                accessToken = GenerateAccessToken(tokenRequest.ClientId);
            }
            else if (tokenRequest.GrantType == "client_credentials")
            {
                accessToken = GenerateAccessToken(tokenRequest.ClientId);
            }
            else
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "unsupported_grant_type", "Unsupported grant type");
            }

            var tokenResponse = new OAuthTokenResponse
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = "mcp:read mcp:write"
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(tokenResponse, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing token request");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "server_error", "Internal server error");
        }
    }

    private string GenerateAccessToken(string clientId)
    {
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")[..16];
        AccessTokens[token] = (clientId, DateTime.UtcNow.AddHours(1));
        return token;
    }

    public static bool ValidateAccessToken(string? token)
    {
        if (string.IsNullOrEmpty(token) || !token.StartsWith("Bearer "))
            return false;

        var actualToken = token[7..]; // Remove "Bearer " prefix
        
        if (!AccessTokens.TryGetValue(actualToken, out var tokenInfo))
            return false;

        if (tokenInfo.Expiry < DateTime.UtcNow)
        {
            AccessTokens.Remove(actualToken);
            return false;
        }

        return true;
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query)) return result;
        
        if (query.StartsWith("?")) query = query[1..];
        
        foreach (var pair in query.Split('&'))
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                result[Uri.UnescapeDataString(keyValue[0])] = Uri.UnescapeDataString(keyValue[1]);
            }
        }
        
        return result;
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string error, string description)
    {
        var errorResponse = new OAuthErrorResponse
        {
            Error = error,
            ErrorDescription = description
        };

        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(errorResponse, _jsonOptions));
        return response;
    }
}