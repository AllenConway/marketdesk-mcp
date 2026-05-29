using MarketDesk.Mcp.Models;

namespace MarketDesk.Mcp.Services;

/// <summary>
/// Abstraction over the source of market research data.
/// <para>
/// The MCP tools depend only on this interface, so the backing store can be
/// swapped without touching the tools. Today the only implementation is
/// <see cref="JsonMarketDataStore"/>, which reads the local JSON files. A
/// future implementation (for example, one backed by Azure Cosmos DB) can be
/// added behind this same contract and selected via configuration in
/// <c>Program.cs</c>.
/// </para>
/// </summary>
public interface IMarketDataStore
{
    Task<IReadOnlyList<Watchlist>> GetWatchlistsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Position>> GetPositionsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<EarningsEvent>> GetEarningsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ThesisNote>> GetThesisNotesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<RiskNote>> GetRiskNotesAsync(CancellationToken ct = default);

    // --- Per-symbol convenience helpers (case-insensitive) ---

    Task<Position?> GetPositionAsync(string symbol, CancellationToken ct = default);

    /// <summary>Names of the watchlists that contain the given symbol.</summary>
    Task<IReadOnlyList<string>> GetWatchlistsContainingAsync(string symbol, CancellationToken ct = default);

    Task<IReadOnlyList<ThesisNote>> GetThesisNotesForSymbolAsync(string symbol, CancellationToken ct = default);

    Task<IReadOnlyList<RiskNote>> GetRiskNotesForSymbolAsync(string symbol, CancellationToken ct = default);

    Task<IReadOnlyList<EarningsEvent>> GetEarningsForSymbolAsync(string symbol, CancellationToken ct = default);
}
