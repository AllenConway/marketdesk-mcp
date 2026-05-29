namespace MarketDesk.Mcp.Models;

/// <summary>
/// A scheduled or historical earnings report for a symbol.
/// </summary>
public sealed class EarningsEvent
{
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Report date in ISO format (yyyy-MM-dd).</summary>
    public string ReportDate { get; set; } = string.Empty;

    public string FiscalPeriod { get; set; } = string.Empty;

    public decimal? EpsEstimate { get; set; }

    public decimal? EpsActual { get; set; }

    public bool Confirmed { get; set; }
}
