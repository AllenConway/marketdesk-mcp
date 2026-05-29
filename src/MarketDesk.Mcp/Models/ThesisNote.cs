namespace MarketDesk.Mcp.Models;

/// <summary>
/// A free-form investment thesis the user keeps for a symbol.
/// </summary>
public sealed class ThesisNote
{
    public string Symbol { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Thesis { get; set; } = string.Empty;

    /// <summary>Conviction level, e.g. Low, Medium, High.</summary>
    public string Conviction { get; set; } = string.Empty;

    public string UpdatedOn { get; set; } = string.Empty;
}
