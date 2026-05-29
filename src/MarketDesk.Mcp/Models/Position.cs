namespace MarketDesk.Mcp.Models;

/// <summary>
/// A mock (paper) position. No live brokerage data is used.
/// </summary>
public sealed class Position
{
    public string Symbol { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal AverageCost { get; set; }

    /// <summary>A static, manually maintained reference price for learning purposes.</summary>
    public decimal LastPrice { get; set; }

    public string OpenedOn { get; set; } = string.Empty;

    public decimal MarketValue => Quantity * LastPrice;

    public decimal CostBasis => Quantity * AverageCost;

    public decimal UnrealizedPnL => MarketValue - CostBasis;
}
