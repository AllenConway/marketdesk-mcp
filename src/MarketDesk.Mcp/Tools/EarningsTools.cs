using System.ComponentModel;
using System.Globalization;
using MarketDesk.Mcp.Models;
using MarketDesk.Mcp.Services;
using ModelContextProtocol.Server;

namespace MarketDesk.Mcp.Tools;

/// <summary>
/// MCP tools for querying earnings events.
/// </summary>
[McpServerToolType]
public sealed class EarningsTools
{
    [McpServerTool(Name = "list_earnings")]
    [Description("Lists all known earnings events, ordered by report date.")]
    public static async Task<IReadOnlyList<EarningsEvent>> ListEarnings(MarketDataStore store, CancellationToken ct)
        => [.. (await store.GetEarningsAsync(ct)).OrderBy(e => e.ReportDate)];

    [McpServerTool(Name = "list_earnings_for_symbol")]
    [Description("Lists earnings events for a single ticker symbol (case-insensitive).")]
    public static async Task<IReadOnlyList<EarningsEvent>> ListEarningsForSymbol(
        MarketDataStore store,
        [Description("The ticker symbol, e.g. AAPL.")] string symbol,
        CancellationToken ct)
        => [.. (await store.GetEarningsAsync(ct))
            .Where(e => string.Equals(e.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.ReportDate)];

    [McpServerTool(Name = "upcoming_earnings")]
    [Description("Lists confirmed earnings events scheduled within the given number of days from today.")]
    public static async Task<IReadOnlyList<EarningsEvent>> UpcomingEarnings(
        MarketDataStore store,
        CancellationToken ct,
        [Description("How many days ahead to include. Defaults to 30.")] int daysAhead = 30)
    {
        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(daysAhead);

        return [.. (await store.GetEarningsAsync(ct))
            .Where(e => DateTime.TryParse(
                e.ReportDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                && date.Date >= today && date.Date <= cutoff)
            .OrderBy(e => e.ReportDate)];
    }
}
