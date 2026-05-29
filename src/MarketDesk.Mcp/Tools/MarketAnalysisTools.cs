using System.ComponentModel;
using System.Globalization;
using MarketDesk.Mcp.Models;
using MarketDesk.Mcp.Services;
using ModelContextProtocol.Server;

namespace MarketDesk.Mcp.Tools;

/// <summary>
/// Higher-level, read-only MCP tools that combine the raw data (positions,
/// watchlists, thesis notes, risk notes, earnings) into useful summaries.
/// All output is derived only from the local mock JSON files. None of this is
/// financial advice or a trading recommendation.
/// </summary>
[McpServerToolType]
public sealed class MarketAnalysisTools
{
    private const string Disclaimer =
        "This summary is generated from local, fictional sample data for demo " +
        "purposes only. It is not financial advice and not a recommendation to " +
        "buy, sell, or hold any security.";

    private const string JournalDisclaimer =
        "This is a journaling aid generated from local, fictional sample data. " +
        "It is not financial advice and not a recommendation to buy, sell, or " +
        "hold any security. It only restates notes you already recorded.";

    [McpServerTool(Name = "summarize_ticker_thesis")]
    [Description("Combines position, watchlist, thesis, risk, and upcoming earnings data " +
        "into a concise bull/bear summary for a single symbol.")]
    public static async Task<TickerThesisSummary> SummarizeTickerThesis(
        IMarketDataStore store,
        [Description("The ticker symbol to summarize, e.g. NVDA.")] string symbol,
        CancellationToken ct)
    {
        symbol = symbol.Trim();

        var position = await store.GetPositionAsync(symbol, ct);
        var watchlists = await store.GetWatchlistsContainingAsync(symbol, ct);
        var theses = await store.GetThesisNotesForSymbolAsync(symbol, ct);
        var risks = await store.GetRiskNotesForSymbolAsync(symbol, ct);
        var upcoming = UpcomingEarnings(await store.GetEarningsForSymbolAsync(symbol, ct));

        var held = position is not null;
        var watched = watchlists.Count > 0;

        return new TickerThesisSummary
        {
            Symbol = symbol.ToUpperInvariant(),
            CompanyName = null, // No company-name data in the local files yet.
            Status = DescribeStatus(held, watched),
            IsHeld = held,
            IsWatched = watched,
            Watchlists = watchlists,
            BullCase = theses.Count > 0
                ? [.. theses.Select(t => $"{t.Title} ({t.Conviction} conviction): {t.Thesis}")]
                : ["No thesis notes recorded for this symbol."],
            BearCase = risks.Count > 0
                ? [.. risks.Select(r => $"{r.Risk} (severity: {r.Severity})")]
                : ["No risk notes recorded; absence of notes is not absence of risk."],
            KeyRisks = [.. risks.Select(r => $"[{r.Severity}] {r.Risk} → mitigation: {r.Mitigation}")],
            UpcomingCatalysts = [.. upcoming.Select(DescribeEarnings)],
            WhatToWatchNext = BuildWatchNext(position, theses, risks, upcoming),
            Disclaimer = Disclaimer,
        };
    }

    [McpServerTool(Name = "find_upcoming_earnings_risks")]
    [Description("Lists symbols with earnings within the given window, annotated with " +
        "position/watchlist status, related risk notes, and a simple local risk level.")]
    public static async Task<IReadOnlyList<EarningsRisk>> FindUpcomingEarningsRisks(
        IMarketDataStore store,
        CancellationToken ct,
        [Description("How many days ahead to include. Defaults to 30.")] int daysAhead = 30)
    {
        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(daysAhead);

        var results = new List<EarningsRisk>();

        foreach (var earnings in await store.GetEarningsAsync(ct))
        {
            if (!TryParseDate(earnings.ReportDate, out var date)
                || date < today || date > cutoff)
            {
                continue;
            }

            var position = await store.GetPositionAsync(earnings.Symbol, ct);
            var watchlists = await store.GetWatchlistsContainingAsync(earnings.Symbol, ct);
            var risks = await store.GetRiskNotesForSymbolAsync(earnings.Symbol, ct);
            var daysUntil = (date - today).Days;

            results.Add(new EarningsRisk
            {
                Symbol = earnings.Symbol.ToUpperInvariant(),
                EarningsDate = earnings.ReportDate,
                DaysUntil = daysUntil,
                FiscalPeriod = earnings.FiscalPeriod,
                Confirmed = earnings.Confirmed,
                IsHeld = position is not null,
                IsWatched = watchlists.Count > 0,
                Watchlists = watchlists,
                RiskNotes = [.. risks.Select(r => $"[{r.Severity}] {r.Risk}")],
                RiskLevel = AssessRiskLevel(risks, daysUntil, position is not null),
            });
        }

        return [.. results.OrderBy(r => r.DaysUntil)];
    }

    [McpServerTool(Name = "generate_trade_journal_entry")]
    [Description("Generates a disciplined, neutral trade-journal entry from local data. " +
        "Does not make recommendations; only restates recorded context for the action you took.")]
    public static async Task<TradeJournalEntry> GenerateTradeJournalEntry(
        IMarketDataStore store,
        [Description("The ticker symbol, e.g. AMD.")] string symbol,
        [Description("The action taken: buy, sell, hold, trim, add, or watch.")] string action,
        CancellationToken ct,
        [Description("Optional free-text reason the user wants to record.")] string? userReason = null)
    {
        symbol = symbol.Trim();

        var position = await store.GetPositionAsync(symbol, ct);
        var theses = await store.GetThesisNotesForSymbolAsync(symbol, ct);
        var risks = await store.GetRiskNotesForSymbolAsync(symbol, ct);
        var upcoming = UpcomingEarnings(await store.GetEarningsForSymbolAsync(symbol, ct));

        return new TradeJournalEntry
        {
            DateGenerated = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Symbol = symbol.ToUpperInvariant(),
            Action = action.Trim().ToLowerInvariant(),
            PositionContext = position is null
                ? "No open mock position recorded for this symbol."
                : $"Holding {position.Quantity} @ avg {position.AverageCost:0.00}; "
                  + $"last {position.LastPrice:0.00}; unrealized P/L {position.UnrealizedPnL:0.00}.",
            ThesisSummary = theses.Count > 0
                ? [.. theses.Select(t => $"{t.Title} ({t.Conviction}): {t.Thesis}")]
                : ["No thesis notes recorded."],
            Risks = risks.Count > 0
                ? [.. risks.Select(r => $"[{r.Severity}] {r.Risk}")]
                : ["No risk notes recorded."],
            UserReason = string.IsNullOrWhiteSpace(userReason) ? null : userReason.Trim(),
            WhatWouldInvalidateThesis = [.. risks.Select(r => $"If this plays out: {r.Risk}. Plan: {r.Mitigation}")],
            WhatToReviewNext = BuildWatchNext(position, theses, risks, upcoming),
            Disclaimer = JournalDisclaimer,
        };
    }

    [McpServerTool(Name = "generate_market_briefing")]
    [Description("Generates a concise personal research briefing from local data: portfolio " +
        "overview, watchlist highlights, upcoming earnings, highest-risk symbols, symbols to " +
        "review, and reflection questions. Not financial advice.")]
    public static async Task<MarketBriefing> GenerateMarketBriefing(
        IMarketDataStore store,
        CancellationToken ct,
        [Description("What to focus on: portfolio, watchlist, earnings, risks, or all. Defaults to all.")]
        string focus = "all",
        [Description("How many days ahead to include for earnings. Defaults to 30.")] int daysAhead = 30)
    {
        focus = string.IsNullOrWhiteSpace(focus) ? "all" : focus.Trim().ToLowerInvariant();
        var all = focus == "all";

        var positions = await store.GetPositionsAsync(ct);
        var watchlists = await store.GetWatchlistsAsync(ct);
        var risks = await store.GetRiskNotesAsync(ct);

        var briefing = new MarketBriefing
        {
            DateGenerated = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Focus = focus,
            DaysAhead = daysAhead,
            Disclaimer =
                "This is a personal research briefing built only from local, fictional sample " +
                "data. It is not financial advice and contains no real-time prices, news, analyst " +
                "ratings, or live market information.",
        };

        // Portfolio overview
        if (all || focus == "portfolio")
        {
            briefing.PortfolioOverview = new PortfolioOverview
            {
                PositionCount = positions.Count,
                TotalCostBasis = positions.Sum(p => p.CostBasis),
                TotalMarketValue = positions.Sum(p => p.MarketValue),
                TotalUnrealizedPnL = positions.Sum(p => p.UnrealizedPnL),
                Holdings = [.. positions
                    .OrderByDescending(p => p.MarketValue)
                    .Select(p => $"{p.Symbol}: {p.Quantity} @ avg {p.AverageCost:0.00}, "
                        + $"last {p.LastPrice:0.00}, unrealized P/L {p.UnrealizedPnL:0.00}")],
            };
        }

        // Watchlist highlights (symbols watched but not held are the interesting ones)
        if (all || focus == "watchlist")
        {
            var heldSymbols = positions
                .Select(p => p.Symbol)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            briefing.WatchlistHighlights = [.. watchlists
                .SelectMany(w => w.Items.Select(i => new { List = w.Name, i.Symbol, i.Note }))
                .Select(x => heldSymbols.Contains(x.Symbol)
                    ? $"{x.Symbol} ({x.List}, held): {x.Note}"
                    : $"{x.Symbol} ({x.List}, not held): {x.Note}")];
        }

        // Upcoming earnings within the window
        if (all || focus == "earnings")
        {
            briefing.UpcomingEarnings = [.. (await FindUpcomingEarningsRisks(store, ct, daysAhead))
                .Select(e =>
                {
                    var status = e.IsHeld ? "held" : e.IsWatched ? "watched" : "not tracked";
                    var confirmed = e.Confirmed ? "confirmed" : "tentative";
                    return $"{e.EarningsDate} ({e.DaysUntil}d) — {e.Symbol} [{status}], "
                        + $"{e.FiscalPeriod}, {confirmed}, risk: {e.RiskLevel}";
                })];
        }

        // Highest-risk symbols: combine risk-note severity with imminent earnings
        if (all || focus == "risks")
        {
            var earningsRisks = (await FindUpcomingEarningsRisks(store, ct, daysAhead))
                .ToDictionary(e => e.Symbol, e => e.RiskLevel, StringComparer.OrdinalIgnoreCase);

            briefing.HighestRiskSymbols = [.. risks
                .GroupBy(r => r.Symbol, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Symbol = g.Key.ToUpperInvariant(),
                    HighCount = g.Count(r => string.Equals(r.Severity, "High", StringComparison.OrdinalIgnoreCase)),
                    Notes = g.ToList(),
                    HasImminentEarnings = earningsRisks.ContainsKey(g.Key),
                })
                .OrderByDescending(x => x.HighCount)
                .ThenByDescending(x => x.HasImminentEarnings)
                .Select(x =>
                {
                    var catalyst = x.HasImminentEarnings ? " + earnings in window" : string.Empty;
                    var top = x.Notes
                        .OrderByDescending(r => string.Equals(r.Severity, "High", StringComparison.OrdinalIgnoreCase))
                        .First();
                    return $"{x.Symbol} [{top.Severity}{catalyst}]: {top.Risk}";
                })];
        }

        // Symbols that may deserve review: at a loss, high risk, or earnings imminent
        var reviewSymbols = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in positions.Where(p => p.UnrealizedPnL < 0))
        {
            reviewSymbols.Add($"{p.Symbol} — unrealized loss in mock data");
        }
        foreach (var r in risks.Where(r => string.Equals(r.Severity, "High", StringComparison.OrdinalIgnoreCase)))
        {
            reviewSymbols.Add($"{r.Symbol.ToUpperInvariant()} — high-severity risk note");
        }
        foreach (var e in (await FindUpcomingEarningsRisks(store, ct, daysAhead)).Where(e => e.DaysUntil <= 7))
        {
            reviewSymbols.Add($"{e.Symbol} — earnings within 7 days");
        }
        briefing.SymbolsToReview = [.. reviewSymbols];

        briefing.QuestionsToAskMyself =
        [
            "Has my original thesis for each holding actually changed, or just the price?",
            "Am I sized appropriately for the risk notes I've written down?",
            "For any name at a loss, what specifically would invalidate the thesis?",
            "Which upcoming earnings could move a position more than I'm comfortable with?",
            "Am I acting on recorded reasoning, or reacting to emotion and noise?",
            "What would a disciplined version of me do nothing about today?",
        ];

        return briefing;
    }

    // --- Helpers ---

    private static List<EarningsEvent> UpcomingEarnings(IReadOnlyList<EarningsEvent> events)
    {
        var today = DateTime.UtcNow.Date;
        return [.. events
            .Where(e => TryParseDate(e.ReportDate, out var d) && d >= today)
            .OrderBy(e => e.ReportDate)];
    }

    private static string DescribeStatus(bool held, bool watched) => (held, watched) switch
    {
        (true, true) => "Held and on a watchlist",
        (true, false) => "Currently held",
        (false, true) => "On a watchlist (not held)",
        _ => "Not held and not on any watchlist",
    };

    private static string DescribeEarnings(EarningsEvent e)
    {
        var confirmed = e.Confirmed ? "confirmed" : "tentative";
        return $"{e.ReportDate} — {e.FiscalPeriod} earnings ({confirmed}).";
    }

    private static List<string> BuildWatchNext(
        Position? position,
        IReadOnlyList<ThesisNote> theses,
        IReadOnlyList<RiskNote> risks,
        IReadOnlyList<EarningsEvent> upcoming)
    {
        var items = new List<string>();

        if (upcoming.Count > 0)
        {
            items.Add($"Next earnings: {upcoming[0].ReportDate} ({upcoming[0].FiscalPeriod}).");
        }

        foreach (var risk in risks.Where(r => string.Equals(r.Severity, "High", StringComparison.OrdinalIgnoreCase)))
        {
            items.Add($"High-severity risk to monitor: {risk.Risk}");
        }

        if (position is not null && position.UnrealizedPnL < 0)
        {
            items.Add("Position is currently at an unrealized loss in the mock data — revisit the exit plan.");
        }

        if (theses.Count == 0)
        {
            items.Add("No written thesis yet — consider recording one before acting.");
        }

        if (items.Count == 0)
        {
            items.Add("No specific catalysts or risks recorded; keep notes up to date.");
        }

        return items;
    }

    /// <summary>
    /// Simple, local-only risk heuristic. Considers recorded risk-note severity
    /// and how soon earnings are. Not a market risk model.
    /// </summary>
    private static string AssessRiskLevel(IReadOnlyList<RiskNote> risks, int daysUntil, bool isHeld)
    {
        var hasHigh = risks.Any(r => string.Equals(r.Severity, "High", StringComparison.OrdinalIgnoreCase));
        var hasMedium = risks.Any(r => string.Equals(r.Severity, "Medium", StringComparison.OrdinalIgnoreCase));

        // Held names with imminent earnings (<= 7 days) and a high-severity note are the most notable.
        if (hasHigh && (daysUntil <= 7 || isHeld))
        {
            return "High";
        }

        if (hasHigh || (hasMedium && daysUntil <= 7))
        {
            return "Medium";
        }

        return hasMedium ? "Medium" : "Low";
    }

    private static bool TryParseDate(string value, out DateTime date)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    // --- Output models ---

    /// <summary>Combined bull/bear summary for a single symbol.</summary>
    public sealed class TickerThesisSummary
    {
        public string Symbol { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool IsHeld { get; set; }

        public bool IsWatched { get; set; }

        public IReadOnlyList<string> Watchlists { get; set; } = [];

        public IReadOnlyList<string> BullCase { get; set; } = [];

        public IReadOnlyList<string> BearCase { get; set; } = [];

        public IReadOnlyList<string> KeyRisks { get; set; } = [];

        public IReadOnlyList<string> UpcomingCatalysts { get; set; } = [];

        public IReadOnlyList<string> WhatToWatchNext { get; set; } = [];

        public string Disclaimer { get; set; } = string.Empty;
    }

    /// <summary>An upcoming earnings event annotated with local risk context.</summary>
    public sealed class EarningsRisk
    {
        public string Symbol { get; set; } = string.Empty;

        public string EarningsDate { get; set; } = string.Empty;

        public int DaysUntil { get; set; }

        public string FiscalPeriod { get; set; } = string.Empty;

        public bool Confirmed { get; set; }

        public bool IsHeld { get; set; }

        public bool IsWatched { get; set; }

        public IReadOnlyList<string> Watchlists { get; set; } = [];

        public IReadOnlyList<string> RiskNotes { get; set; } = [];

        public string RiskLevel { get; set; } = string.Empty;
    }

    /// <summary>A neutral, disciplined trade-journal entry.</summary>
    public sealed class TradeJournalEntry
    {
        public string DateGenerated { get; set; } = string.Empty;

        public string Symbol { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string PositionContext { get; set; } = string.Empty;

        public IReadOnlyList<string> ThesisSummary { get; set; } = [];

        public IReadOnlyList<string> Risks { get; set; } = [];

        public string? UserReason { get; set; }

        public IReadOnlyList<string> WhatWouldInvalidateThesis { get; set; } = [];

        public IReadOnlyList<string> WhatToReviewNext { get; set; } = [];

        public string Disclaimer { get; set; } = string.Empty;
    }

    /// <summary>A concise personal research briefing assembled from local data.</summary>
    public sealed class MarketBriefing
    {
        public string DateGenerated { get; set; } = string.Empty;

        public string Focus { get; set; } = string.Empty;

        public int DaysAhead { get; set; }

        public PortfolioOverview? PortfolioOverview { get; set; }

        public IReadOnlyList<string> WatchlistHighlights { get; set; } = [];

        public IReadOnlyList<string> UpcomingEarnings { get; set; } = [];

        public IReadOnlyList<string> HighestRiskSymbols { get; set; } = [];

        public IReadOnlyList<string> SymbolsToReview { get; set; } = [];

        public IReadOnlyList<string> QuestionsToAskMyself { get; set; } = [];

        public string Disclaimer { get; set; } = string.Empty;
    }

    /// <summary>Aggregated mock-portfolio totals for the briefing.</summary>
    public sealed class PortfolioOverview
    {
        public int PositionCount { get; set; }

        public decimal TotalCostBasis { get; set; }

        public decimal TotalMarketValue { get; set; }

        public decimal TotalUnrealizedPnL { get; set; }

        public IReadOnlyList<string> Holdings { get; set; } = [];
    }
}
