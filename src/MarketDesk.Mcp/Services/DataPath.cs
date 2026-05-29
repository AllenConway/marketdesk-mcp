namespace MarketDesk.Mcp.Services;

/// <summary>
/// Locates the local <c>data</c> directory that holds the JSON files.
/// </summary>
internal static class DataPath
{
    /// <summary>
    /// Resolves the data directory using, in order:
    /// 1. the <c>MARKETDESK_DATA_DIR</c> environment variable, if set;
    /// 2. a <c>data</c> folder found by walking up from the current working
    ///    directory or the application's base directory.
    /// </summary>
    public static string Resolve()
    {
        var configured = Environment.GetEnvironmentVariable("MARKETDESK_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "data");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate the 'data' directory. Set the MARKETDESK_DATA_DIR " +
            "environment variable or run the server from the repository root.");
    }
}
