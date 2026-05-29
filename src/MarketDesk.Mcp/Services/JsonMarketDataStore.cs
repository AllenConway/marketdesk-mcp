using System.Text.Json;
using MarketDesk.Mcp.Models;

namespace MarketDesk.Mcp.Services;

/// <summary>
/// <see cref="IMarketDataStore"/> implementation that reads the local JSON
/// files that back the market research data. Files are read on each call so
/// edits to the JSON are picked up without restarting the server.
/// </summary>
public sealed class JsonMarketDataStore : IMarketDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _dataDirectory = DataPath.Resolve();

    public Task<IReadOnlyList<Watchlist>> GetWatchlistsAsync(CancellationToken ct = default)
        => LoadAsync<Watchlist>("watchlists.json", ct);

    public Task<IReadOnlyList<Position>> GetPositionsAsync(CancellationToken ct = default)
        => LoadAsync<Position>("positions.json", ct);

    public Task<IReadOnlyList<EarningsEvent>> GetEarningsAsync(CancellationToken ct = default)
        => LoadAsync<EarningsEvent>("earnings.json", ct);

    public Task<IReadOnlyList<ThesisNote>> GetThesisNotesAsync(CancellationToken ct = default)
        => LoadAsync<ThesisNote>("thesis-notes.json", ct);

    public Task<IReadOnlyList<RiskNote>> GetRiskNotesAsync(CancellationToken ct = default)
        => LoadAsync<RiskNote>("risk-notes.json", ct);

    // --- Per-symbol convenience helpers (case-insensitive) ---

    public async Task<Position?> GetPositionAsync(string symbol, CancellationToken ct = default)
        => (await GetPositionsAsync(ct)).FirstOrDefault(p => Matches(p.Symbol, symbol));

    /// <summary>Names of the watchlists that contain the given symbol.</summary>
    public async Task<IReadOnlyList<string>> GetWatchlistsContainingAsync(string symbol, CancellationToken ct = default)
        => [.. (await GetWatchlistsAsync(ct))
            .Where(w => w.Items.Any(i => Matches(i.Symbol, symbol)))
            .Select(w => w.Name)];

    public async Task<IReadOnlyList<ThesisNote>> GetThesisNotesForSymbolAsync(string symbol, CancellationToken ct = default)
        => [.. (await GetThesisNotesAsync(ct)).Where(t => Matches(t.Symbol, symbol))];

    public async Task<IReadOnlyList<RiskNote>> GetRiskNotesForSymbolAsync(string symbol, CancellationToken ct = default)
        => [.. (await GetRiskNotesAsync(ct)).Where(r => Matches(r.Symbol, symbol))];

    public async Task<IReadOnlyList<EarningsEvent>> GetEarningsForSymbolAsync(string symbol, CancellationToken ct = default)
        => [.. (await GetEarningsAsync(ct)).Where(e => Matches(e.Symbol, symbol))];

    private static bool Matches(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private async Task<IReadOnlyList<T>> LoadAsync<T>(string fileName, CancellationToken ct)
    {
        var path = Path.Combine(_dataDirectory, fileName);
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, SerializerOptions, ct) ?? [];
    }
}
