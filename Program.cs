using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SysMLMCPServer.Services;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Configure services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SysMLApiService>();

var host = builder.Build();
await host.RunAsync();