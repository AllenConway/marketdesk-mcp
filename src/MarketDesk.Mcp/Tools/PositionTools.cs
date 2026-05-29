using System.ComponentModel;
using MarketDesk.Mcp.Models;
using MarketDesk.Mcp.Services;
using ModelContextProtocol.Server;

namespace MarketDesk.Mcp.Tools;

/// <summary>
/// MCP tools for querying mock (paper) positions and basic portfolio math.
/// </summary>
[McpServerToolType]
public sealed class PositionTools
{
    [McpServerTool(Name = "list_positions")]
    [Description("Lists all mock positions, including cost basis, market value, and unrealized P/L.")]
    public static Task<IReadOnlyList<Position>> ListPositions(IMarketDataStore store, CancellationToken ct)
        => store.GetPositionsAsync(ct);

    [McpServerTool(Name = "get_position")]
    [Description("Gets a single mock position by ticker symbol (case-insensitive).")]
    public static async Task<Position?> GetPosition(
        IMarketDataStore store,
        [Description("The ticker symbol, e.g. MSFT.")] string symbol,
        CancellationToken ct)
        => (await store.GetPositionsAsync(ct))
            .FirstOrDefault(p => string.Equals(p.Symbol, symbol, StringComparison.OrdinalIgnoreCase));

    [McpServerTool(Name = "portfolio_summary")]
    [Description("Summarizes the mock portfolio: total cost basis, market value, and unrealized P/L.")]
    public static async Task<PortfolioSummary> GetPortfolioSummary(IMarketDataStore store, CancellationToken ct)
    {
        var positions = await store.GetPositionsAsync(ct);
        return new PortfolioSummary
        {
            PositionCount = positions.Count,
            TotalCostBasis = positions.Sum(p => p.CostBasis),
            TotalMarketValue = positions.Sum(p => p.MarketValue),
            TotalUnrealizedPnL = positions.Sum(p => p.UnrealizedPnL),
        };
    }

    /// <summary>Aggregated totals for the mock portfolio.</summary>
    public sealed class PortfolioSummary
    {
        public int PositionCount { get; set; }

        public decimal TotalCostBasis { get; set; }

        public decimal TotalMarketValue { get; set; }

        public decimal TotalUnrealizedPnL { get; set; }
    }
}
