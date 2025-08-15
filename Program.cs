using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using SysMLMCPServer.Services;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Configure services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SysMLApiService>();

builder.Build().Run();