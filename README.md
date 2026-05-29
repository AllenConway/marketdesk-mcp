# MarketDesk.Mcp

A small, local **Model Context Protocol (MCP) server** written in C# (.NET 10) that turns a set of local JSON files into a personal market-research assistant for **VS Code GitHub Copilot Chat**.

It is intentionally simple and dependency-light: **no external stock APIs, no authentication, no databases, no cloud services, and no live brokerage data**. All data lives in plain JSON files under [`data/`](data/), so you can read and edit everything by hand.

> ⚠️ **All data in this repository is completely fake.** The watchlists, mock positions, prices, earnings dates, EPS figures, thesis notes, and risk notes under [`data/`](data/) are made-up sample values for **MCP demo and learning purposes only**. Nothing here is real market data, a real portfolio, or investment advice. Do not use any of it to make financial decisions.

> The GitHub repository is named `marketdesk-mcp`, but the .NET solution, project, namespaces, and assembly all use `MarketDesk.Mcp`.

## What it exposes

The server registers **17 read-only MCP tools**. They fall into two groups: simple
data-access tools that return raw records, and higher-level analysis tools that combine
those records into useful summaries. Every tool reads only from the local JSON files —
nothing writes data, calls the network, or makes recommendations.

### Data-access tools

| Tool | Purpose | Required inputs | Optional inputs | Example Copilot prompt |
| --- | --- | --- | --- | --- |
| `list_watchlists` | Lists all watchlists with their symbols and notes. | — | — | "List my watchlists." |
| `get_watchlist` | Gets a single watchlist by name (case-insensitive). | `name` (string) | — | "Show my 'Core Conviction' watchlist." |
| `list_positions` | Lists all mock positions with cost basis, market value, and unrealized P/L. | — | — | "List all my mock positions." |
| `get_position` | Gets a single mock position by ticker symbol. | `symbol` (string) | — | "What's my mock position in MSFT?" |
| `portfolio_summary` | Aggregates total cost basis, market value, and unrealized P/L. | — | — | "Show my mock portfolio summary." |
| `list_earnings` | Lists all earnings events, ordered by report date. | — | — | "List all earnings events." |
| `list_earnings_for_symbol` | Lists earnings events for one symbol. | `symbol` (string) | — | "Show earnings events for NVDA." |
| `upcoming_earnings` | Lists confirmed earnings within N days from today. | — | `daysAhead` (int, default 30) | "Show upcoming earnings in the next 60 days." |
| `list_thesis_notes` | Lists all investment thesis notes. | — | — | "List all my thesis notes." |
| `get_thesis_notes_for_symbol` | Gets thesis notes for one symbol. | `symbol` (string) | — | "What's my thesis on AMD?" |
| `list_risk_notes` | Lists all risk notes. | — | — | "List all my risk notes." |
| `get_risk_notes_for_symbol` | Gets risk notes for one symbol. | `symbol` (string) | — | "What risks am I tracking on TSLA?" |

### Higher-level analysis tools

| Tool | Purpose | Required inputs | Optional inputs | Example Copilot prompt |
| --- | --- | --- | --- | --- |
| `summarize_ticker_thesis` | Combines position, watchlist, thesis, risk, and upcoming earnings into a bull/bear summary for one symbol. | `symbol` (string) | — | "Summarize my thesis for NVDA." |
| `find_upcoming_earnings_risks` | Lists symbols with earnings in a window, annotated with held/watched status, risk notes, and a local risk level. | — | `daysAhead` (int, default 30) | "Find upcoming earnings risks in the next 30 days." |
| `generate_trade_journal_entry` | Produces a neutral, disciplined journal entry that restates recorded context for an action. Makes no recommendations. | `symbol` (string), `action` (buy/sell/hold/trim/add/watch) | `userReason` (string) | "Generate a trade journal entry for RBLX with action hold." |
| `generate_market_briefing` | Builds a concise personal research briefing across portfolio, watchlist, earnings, and risks. | — | `focus` (portfolio/watchlist/earnings/risks/all, default all), `daysAhead` (int, default 30) | "Generate a 30-day market briefing for my mock portfolio and watchlist." |

## Project structure

```
marketdesk-mcp/
├─ MarketDesk.Mcp.slnx
├─ run-mcp-server.bat          # Windows launcher for Claude Desktop / Claude Code
├─ .vscode/
│  └─ mcp.json                 # VS Code MCP server registration
├─ data/                       # Local JSON data (edit freely)
│  ├─ watchlists.json
│  ├─ positions.json
│  ├─ earnings.json
│  ├─ thesis-notes.json
│  └─ risk-notes.json
└─ src/
   └─ MarketDesk.Mcp/
      ├─ MarketDesk.Mcp.csproj
      ├─ Program.cs            # Host + stdio MCP server bootstrap
      ├─ Models/               # Plain data models
      ├─ Services/             # JSON data store + data path resolution
      └─ Tools/                # MCP tool classes
```

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- VS Code with GitHub Copilot Chat (for using the tools interactively)

## Run it locally

From the repository root:

```pwsh
dotnet run --project src/MarketDesk.Mcp/MarketDesk.Mcp.csproj
```

The server communicates over **stdio**, so when run directly it will wait for an MCP client to connect. Logs are written to **stderr** (stdout is reserved for the MCP protocol).

## Use it from VS Code Copilot Chat

This repo ships a ready-to-use [`.vscode/mcp.json`](.vscode/mcp.json):

```json
{
  "servers": {
    "marketdesk": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "src/MarketDesk.Mcp/MarketDesk.Mcp.csproj"],
      "env": { "MARKETDESK_DATA_DIR": "${workspaceFolder}/data" }
    }
  }
}
```

1. Open this folder in VS Code.
2. Open the `mcp.json` file and click **Start** on the `marketdesk` server (or start it from the MCP servers view).
3. In Copilot Chat (Agent mode), ask things like:
   - "List my watchlists."
   - "What's my mock portfolio summary?"
   - "Show upcoming earnings in the next 60 days."
   - "What's my thesis on NVDA, and what risks am I tracking?"
   - "Use MarketDesk to generate a market briefing for my mock portfolio and watchlist for the next 30 days."

> **Tip:** MCP tools only work in Copilot Chat's **Agent** mode (not Ask/Edit).

### Workspace vs. user (global) scope

The bundled [`.vscode/mcp.json`](.vscode/mcp.json) is **workspace-scoped** — it only
applies when this folder is open, but it travels with the repo so anyone who clones it
gets the server automatically.

To make the server available in **every** VS Code window without copying `mcp.json` into
each workspace, register it at **user scope** instead: open the Command Palette and run
**MCP: Open User Configuration**, then add the entry below. Use **absolute paths** (there
is no `${workspaceFolder}` at user scope):

```json
{
  "servers": {
    "marketdesk": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\marketdesk-mcp\\src\\MarketDesk.Mcp\\MarketDesk.Mcp.csproj"
      ],
      "env": {
        "MARKETDESK_DATA_DIR": "C:\\path\\to\\marketdesk-mcp\\data"
      }
    }
  }
}
```

VS Code merges workspace and user scopes, so you can keep both. Replace
`C:\\path\\to\\marketdesk-mcp` with the absolute path to your clone.

## Use it from Claude Desktop / Claude Code

The exact same stdio server works in Claude. Claude has no "workspace" concept and
launches MCP servers from its app process (where `dotnet` may not be on `PATH`), so the
most reliable approach on Windows is the included [`run-mcp-server.bat`](run-mcp-server.bat)
wrapper. It sets `MARKETDESK_DATA_DIR` and uses absolute paths derived from its own
location, so it keeps working wherever the repo is cloned.

The wrapper **builds the project first and sends that output to stderr, then runs the
compiled DLL** — this matters because the MCP stdio transport requires `stdout` to carry
*only* JSON-RPC. Using `dotnet run` leaks build/restore text to stdout, which Claude
reports as `Unexpected token 'C', "C:\Program"... is not valid JSON`.

### Claude Desktop

1. Open **Settings → Developer → Local MCP servers** and click **Edit Config** (this opens
   `claude_desktop_config.json`).
2. Add a `Market Desk` entry under `mcpServers`, pointing at the batch file (the key is
   the display name Claude shows for the server):

   ```json
   {
     "mcpServers": {
       "Market Desk": {
         "command": "C:\\path\\to\\marketdesk-mcp\\run-mcp-server.bat",
         "args": []
       }
     }
   }
   ```

   Replace `C:\\path\\to\\marketdesk-mcp` with the absolute path to your clone (use
   double backslashes in JSON). If you already have other servers, just add `Market Desk`
   alongside them — don't replace the whole `mcpServers` object.
3. Restart Claude Desktop. The `Market Desk` server should show as **running**, and you
   can ask the same prompts listed above.

> **Why the `.bat` and not `dotnet run` directly?** The wrapper builds to stderr and then
> runs the compiled DLL, keeping `stdout` clean for the JSON-RPC protocol. Running
> `dotnet run` directly leaks build output to stdout and produces the
> `... is not valid JSON` error in Claude. The batch wrapper also guarantees `dotnet`
> resolves and the data path is correct regardless of how Claude launches the process.

### Claude Code (CLI)

From the repo root:

```pwsh
claude mcp add "Market Desk" -- "./run-mcp-server.bat"
```

Then run `claude` and the Market Desk tools will be available in the session.

## Demo Script

A suggested flow for testing in **VS Code Copilot Chat (Agent mode)**. It starts with
simple data lookups and builds up to combined analysis and a higher-level discussion.
Run each prompt in order:

1. **List my watchlists.**
   *(Exercises `list_watchlists` — the simplest read.)*
2. **Show my mock portfolio summary.**
   *(Exercises `portfolio_summary` — aggregation over the positions file.)*
3. **Summarize the thesis for NVDA.**
   *(Exercises `summarize_ticker_thesis` — combines position, watchlist, thesis, risk, and earnings.)*
4. **Find upcoming earnings risks.**
   *(Exercises `find_upcoming_earnings_risks` — joins earnings with risk notes and holdings.)*
5. **Generate a trade journal entry for RBLX with action hold.**
   *(Exercises `generate_trade_journal_entry` — neutral, recorded-context entry; no recommendation.)*
6. **Generate a 30-day market briefing.**
   *(Exercises `generate_market_briefing` — the broadest tool, pulling everything together.)*
7. **Explain how this same pattern could work for enterprise customer orders or support tickets.**
   *(No tool call — Copilot reasons about the pattern itself; see "Enterprise Pattern Demonstrated" below.)*

## Enterprise Pattern Demonstrated

MarketDesk uses mock market-research data, but the **MCP pattern it demonstrates is the
interesting part** — and that pattern applies directly to real enterprise systems. The
domain data could just as easily be:

- CRM customer profiles
- ERP orders and invoices
- inventory systems
- support tickets
- internal knowledge bases
- account health dashboards
- sales opportunity reviews
- project risk registers

In every one of those cases, the shape is the same:

1. **Local or private structured data lives outside the LLM.** The model never has to
   "memorize" your records; they stay in your systems of record.
2. **The MCP server exposes safe, scoped tools.** Each tool does one well-defined,
   permission-appropriate thing instead of handing over raw database access.
3. **Copilot Chat can call those tools when needed.** The agent decides which tools to
   invoke based on the question, using the tool names and descriptions.
4. **The agent produces grounded answers based on tool results.** Responses are anchored
   to real data returned by the tools, not to guesses.
5. **Read-only tools are a good first step before allowing write operations.** Start by
   safely exposing reads; add carefully scoped writes (create/update) only once the
   read-side boundaries and behavior are well understood.

## What I learned

*(Placeholder — fill these in as you go.)*

- How stdio MCP servers are registered in VS Code:
- How C# MCP tools are discovered:
- How tool names and descriptions affect Copilot behavior:
- How local structured data can ground agent responses:
- Why tool boundaries matter:

## Editing the data

Just edit the JSON files in [`data/`](data/). Files are read on each tool call, so changes are picked up without restarting the server. You can point the server at a different folder with the `MARKETDESK_DATA_DIR` environment variable.

## Where this could grow

This is a learning project, but the structure is meant to scale into a real personal research tool:

- Add write tools (create/update notes and positions).
- Swap the JSON-backed `JsonMarketDataStore` for a database or a real market-data provider by adding another `IMarketDataStore` implementation and selecting it with the `MARKETDESK_DATA_SOURCE` setting (defaults to `json`).
- Add MCP resources/prompts for richer Copilot context.
