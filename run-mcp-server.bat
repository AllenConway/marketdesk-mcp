@echo off
REM Launches the MarketDesk MCP server for Claude Desktop / Claude Code.
REM %~dp0 expands to this file's folder (the repo root) with a trailing backslash,
REM so the server works no matter where the repo is cloned.

set "MARKETDESK_DATA_DIR=%~dp0data"

REM Build first, sending all build output to stderr (1>&2) so it never reaches
REM stdout. The MCP stdio transport requires stdout to carry only JSON-RPC, and
REM "dotnet run" leaks build/restore text to stdout, which breaks the protocol.
dotnet build "%~dp0src\MarketDesk.Mcp\MarketDesk.Mcp.csproj" -c Release --nologo -v quiet 1>&2

REM Run the compiled assembly directly so only the server's protocol output
REM goes to stdout.
dotnet "%~dp0src\MarketDesk.Mcp\bin\Release\net10.0\MarketDesk.Mcp.dll"
