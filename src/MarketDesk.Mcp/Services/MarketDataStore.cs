using System.Text.Json;
using MarketDesk.Mcp.Models;

namespace MarketDesk.Mcp.Services;

/// <summary>
/// Reads the local JSON files that back the market research data.
/// Files are read on each call so edits to the JSON are picked up without
/// restarting the server.
/// </summary>
public sealed class MarketDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _dataDirectory = DataPath.Resolve();

    public IReadOnlyList<Watchlist> GetWatchlists() => Load<Watchlist>("watchlists.json");

    public IReadOnlyList<Position> GetPositions() => Load<Position>("positions.json");

    public IReadOnlyList<EarningsEvent> GetEarnings() => Load<EarningsEvent>("earnings.json");

    public IReadOnlyList<ThesisNote> GetThesisNotes() => Load<ThesisNote>("thesis-notes.json");

    public IReadOnlyList<RiskNote> GetRiskNotes() => Load<RiskNote>("risk-notes.json");

    // --- Per-symbol convenience helpers (case-insensitive) ---

    public Position? GetPosition(string symbol)
        => GetPositions().FirstOrDefault(p => Matches(p.Symbol, symbol));

    /// <summary>Names of the watchlists that contain the given symbol.</summary>
    public IReadOnlyList<string> GetWatchlistsContaining(string symbol)
        => [.. GetWatchlists()
            .Where(w => w.Items.Any(i => Matches(i.Symbol, symbol)))
            .Select(w => w.Name)];

    public IReadOnlyList<ThesisNote> GetThesisNotesForSymbol(string symbol)
        => [.. GetThesisNotes().Where(t => Matches(t.Symbol, symbol))];

    public IReadOnlyList<RiskNote> GetRiskNotesForSymbol(string symbol)
        => [.. GetRiskNotes().Where(r => Matches(r.Symbol, symbol))];

    public IReadOnlyList<EarningsEvent> GetEarningsForSymbol(string symbol)
        => [.. GetEarnings().Where(e => Matches(e.Symbol, symbol))];

    private static bool Matches(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private List<T> Load<T>(string fileName)
    {
        var path = Path.Combine(_dataDirectory, fileName);
        if (!File.Exists(path))
        {
            return [];
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<List<T>>(stream, SerializerOptions) ?? [];
    }
}
