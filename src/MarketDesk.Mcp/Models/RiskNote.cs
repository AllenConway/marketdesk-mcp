namespace MarketDesk.Mcp.Models;

/// <summary>
/// A tracked risk and its mitigation for a symbol.
/// </summary>
public sealed class RiskNote
{
    public string Symbol { get; set; } = string.Empty;

    public string Risk { get; set; } = string.Empty;

    /// <summary>Severity level, e.g. Low, Medium, High.</summary>
    public string Severity { get; set; } = string.Empty;

    public string Mitigation { get; set; } = string.Empty;

    public string UpdatedOn { get; set; } = string.Empty;
}
