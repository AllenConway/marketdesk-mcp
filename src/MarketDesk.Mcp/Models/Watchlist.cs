namespace MarketDesk.Mcp.Models;

/// <summary>
/// A named collection of ticker symbols the user is tracking.
/// </summary>
public sealed class Watchlist
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<WatchlistItem> Items { get; set; } = [];
}

/// <summary>
/// A single symbol inside a <see cref="Watchlist"/>.
/// </summary>
public sealed class WatchlistItem
{
    public string Symbol { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}
