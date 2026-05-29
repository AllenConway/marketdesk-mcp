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
    public static IReadOnlyList<EarningsEvent> ListEarnings(MarketDataStore store)
        => [.. store.GetEarnings().OrderBy(e => e.ReportDate)];

    [McpServerTool(Name = "list_earnings_for_symbol")]
    [Description("Lists earnings events for a single ticker symbol (case-insensitive).")]
    public static IReadOnlyList<EarningsEvent> ListEarningsForSymbol(
        MarketDataStore store,
        [Description("The ticker symbol, e.g. AAPL.")] string symbol)
        => [.. store.GetEarnings()
            .Where(e => string.Equals(e.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.ReportDate)];

    [McpServerTool(Name = "upcoming_earnings")]
    [Description("Lists confirmed earnings events scheduled within the given number of days from today.")]
    public static IReadOnlyList<EarningsEvent> UpcomingEarnings(
        MarketDataStore store,
        [Description("How many days ahead to include. Defaults to 30.")] int daysAhead = 30)
    {
        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(daysAhead);

        return [.. store.GetEarnings()
            .Where(e => DateTime.TryParse(
                e.ReportDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                && date.Date >= today && date.Date <= cutoff)
            .OrderBy(e => e.ReportDate)];
    }
}
