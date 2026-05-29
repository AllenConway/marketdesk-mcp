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
// The MCP tools depend only on IMarketDataStore, so the backing store can be
// swapped via the MARKETDESK_DATA_SOURCE setting (defaults to "json"). A future
// store (for example, Azure Cosmos DB) can be wired up here behind the same
// interface without changing any tool code.
var dataSource = builder.Configuration["MARKETDESK_DATA_SOURCE"] ?? "json";

switch (dataSource.Trim().ToLowerInvariant())
{
    case "json":
        builder.Services.AddSingleton<IMarketDataStore, JsonMarketDataStore>();
        break;

    // case "cosmos":
    //     // Not implemented yet. A CosmosMarketDataStore would be registered here,
    //     // reusing a singleton CosmosClient and reading from the configured
    //     // database/containers. See README for the planned design.
    //     builder.Services.AddSingleton<IMarketDataStore, CosmosMarketDataStore>();
    //     break;

    default:
        throw new InvalidOperationException(
            $"Unknown MARKETDESK_DATA_SOURCE '{dataSource}'. Supported values: json.");
}


// Register the MCP server with the stdio transport and auto-discover every
// [McpServerToolType] class in this assembly.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
