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
    public static Task<IReadOnlyList<Watchlist>> ListWatchlists(MarketDataStore store, CancellationToken ct)
        => store.GetWatchlistsAsync(ct);

    [McpServerTool(Name = "get_watchlist")]
    [Description("Gets a single watchlist by its name (case-insensitive).")]
    public static async Task<Watchlist?> GetWatchlist(
        MarketDataStore store,
        [Description("The watchlist name to look up.")] string name,
        CancellationToken ct)
        => (await store.GetWatchlistsAsync(ct))
            .FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
}
