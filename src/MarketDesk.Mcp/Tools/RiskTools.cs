using System.ComponentModel;
using MarketDesk.Mcp.Models;
using MarketDesk.Mcp.Services;
using ModelContextProtocol.Server;

namespace MarketDesk.Mcp.Tools;

/// <summary>
/// MCP tools for querying tracked risks and their mitigations.
/// </summary>
[McpServerToolType]
public sealed class RiskTools
{
    [McpServerTool(Name = "list_risk_notes")]
    [Description("Lists all risk notes.")]
    public static Task<IReadOnlyList<RiskNote>> ListRiskNotes(IMarketDataStore store, CancellationToken ct)
        => store.GetRiskNotesAsync(ct);

    [McpServerTool(Name = "get_risk_notes_for_symbol")]
    [Description("Gets risk notes for a single ticker symbol (case-insensitive).")]
    public static Task<IReadOnlyList<RiskNote>> GetRiskNotesForSymbol(
        IMarketDataStore store,
        [Description("The ticker symbol, e.g. TSLA.")] string symbol,
        CancellationToken ct)
        => store.GetRiskNotesForSymbolAsync(symbol, ct);
}
