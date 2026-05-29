using System.ComponentModel;
using MarketDesk.Mcp.Models;
using MarketDesk.Mcp.Services;
using ModelContextProtocol.Server;

namespace MarketDesk.Mcp.Tools;

/// <summary>
/// MCP tools for querying investment thesis notes.
/// </summary>
[McpServerToolType]
public sealed class ThesisTools
{
    [McpServerTool(Name = "list_thesis_notes")]
    [Description("Lists all investment thesis notes.")]
    public static Task<IReadOnlyList<ThesisNote>> ListThesisNotes(IMarketDataStore store, CancellationToken ct)
        => store.GetThesisNotesAsync(ct);

    [McpServerTool(Name = "get_thesis_notes_for_symbol")]
    [Description("Gets thesis notes for a single ticker symbol (case-insensitive).")]
    public static Task<IReadOnlyList<ThesisNote>> GetThesisNotesForSymbol(
        IMarketDataStore store,
        [Description("The ticker symbol, e.g. NVDA.")] string symbol,
        CancellationToken ct)
        => store.GetThesisNotesForSymbolAsync(symbol, ct);
}
