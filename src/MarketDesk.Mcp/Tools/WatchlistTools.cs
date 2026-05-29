using System.ComponentModel;
using MarketDesk.Mcp.Models;
using MarketDesk.Mcp.Services;
using ModelContextProtocol.Server;

namespace MarketDesk.Mcp.Tools;

/// <summary>
/// MCP tools for querying the user's symbol watchlists.
/// </summary>
[McpServerToolType]
public sealed class WatchlistTools
{
    [McpServerTool(Name = "list_watchlists")]
    [Description("Lists all watchlists with their symbols and notes.")]
    public static IReadOnlyList<Watchlist> ListWatchlists(MarketDataStore store)
        => store.GetWatchlists();

    [McpServerTool(Name = "get_watchlist")]
    [Description("Gets a single watchlist by its name (case-insensitive).")]
    public static Watchlist? GetWatchlist(
        MarketDataStore store,
        [Description("The watchlist name to look up.")] string name)
        => store.GetWatchlists()
            .FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
}
