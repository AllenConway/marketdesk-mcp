using MarketDesk.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// MarketDesk.Mcp is a local stdio MCP server for personal market research.
// It is launched by an MCP client (for example VS Code GitHub Copilot Chat),
// which communicates with this process over standard input/output.
var builder = Host.CreateApplicationBuilder(args);

// For stdio servers, stdout is reserved for the MCP protocol.
// All logging must go to stderr so it does not corrupt the protocol stream.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// The data store reads the local JSON files in the repository's /data folder.
builder.Services.AddSingleton<MarketDataStore>();

// Register the MCP server with the stdio transport and auto-discover every
// [McpServerToolType] class in this assembly.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
