using System.Text.Json;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Providers.Azure.Cosmos;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for executing Cosmos DB operations.
/// </summary>
public sealed class CosmosCommand : ICommand
{
    private readonly ICosmosService _cosmosService;
    private readonly IConfigurationService _configurationService;
    private readonly ISessionService _sessionService;

    private static readonly HashSet<string> _subcommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "query"
    };

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <inheritdoc/>
    public string Name => "cosmos";

    /// <inheritdoc/>
    public string Description => "Execute Cosmos DB operations";

    /// <inheritdoc/>
    public string Usage => "cosmos query \"<SQL>\" [--max-items N]";

    /// <inheritdoc/>
    public IReadOnlySet<string> Subcommands => _subcommands;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosCommand"/> class.
    /// </summary>
    /// <param name="cosmosService">The Cosmos DB service.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="sessionService">The session service for file operations.</param>
    public CosmosCommand(
        ICosmosService cosmosService,
        IConfigurationService configurationService,
        ISessionService sessionService)
    {
        _cosmosService = cosmosService ?? throw new ArgumentNullException(nameof(cosmosService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(
        string[] args,
        CommandContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.HasActiveSession)
            return CommandResult.Error("No active session. Use 'session start <ticket>' first.");

        if (args.Length == 0)
            return CommandResult.Error($"Usage: {Usage}");

        var subcommand = args[0].ToLowerInvariant();
        var subArgs = args.Skip(1).ToArray();

        return subcommand switch
        {
            "query" => await HandleQueryAsync(subArgs, context, cancellationToken),
            _ => CommandResult.Error($"Unknown subcommand '{args[0]}'. Available: query")
        };
    }

    private async Task<CommandResult> HandleQueryAsync(
        string[] args,
        CommandContext context,
        CancellationToken cancellationToken)
    {
        var activeAlias = context.CurrentSession?.ActiveContext?.ActiveCosmosAlias;
        if (string.IsNullOrEmpty(activeAlias))
            return CommandResult.Error("No active cosmos context. Use 'use cosmos <alias>' first.");

        var aliasConfig = _configurationService.GetCosmosAlias(activeAlias);
        if (aliasConfig is null)
            return CommandResult.Error($"Cosmos alias '{activeAlias}' not found in config.");

        var (sql, maxItems) = ParseQueryArgs(args);
        if (string.IsNullOrWhiteSpace(sql))
            return CommandResult.Error("Usage: cosmos query \"<SQL>\" [--max-items N]");

        try
        {
            var result = await _cosmosService.ExecuteQueryAsync(
                activeAlias,
                sql,
                maxItems,
                cancellationToken);

            // Output JSON to stdout for piping (use WriteRaw to avoid markup stripping of JSON brackets)
            var json = JsonSerializer.Serialize(result.Items, _jsonOptions);
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json + Environment.NewLine);
            context.Output.WriteRaw(jsonBytes);

            // Output RU charge to stderr so it doesn't interfere with piped JSON
            context.Output.WriteErrorLine($"Request charge: {result.RequestCharge:F2} RU");

            if (result.HasMoreResults)
                context.Output.WriteErrorLine($"Note: More results available (showing first {result.Items.Count} items)");

            // Save results with metadata to queries directory
            await SaveQueryResultAsync(context.CurrentSession!, sql, result, cancellationToken);

            return CommandResult.Ok(data: result);
        }
        catch (InvalidOperationException ex)
        {
            // Key not configured or env var not set
            return CommandResult.Error(ex.Message);
        }
        catch (Microsoft.Azure.Cosmos.CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var retryAfter = ex.RetryAfter?.TotalSeconds ?? 1;
            return CommandResult.Error($"Request was throttled. Try again in {retryAfter:F0} seconds.");
        }
        catch (Microsoft.Azure.Cosmos.CosmosException ex)
        {
            return CommandResult.Error($"Cosmos DB error: {ex.Message}");
        }
    }

    private static (string? sql, int maxItems) ParseQueryArgs(string[] args)
    {
        string? sql = null;
        int maxItems = 100;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--max-items" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out var parsed))
                    maxItems = parsed;
                i++; // Skip the value
            }
            else if (!args[i].StartsWith("--"))
            {
                sql = args[i];
            }
        }

        return (sql, maxItems);
    }

    private async Task SaveQueryResultAsync(
        Session session,
        string sql,
        CosmosQueryResult result,
        CancellationToken cancellationToken)
    {
        var queriesDir = _sessionService.GetQueriesDirectory(session.TicketId);
        Directory.CreateDirectory(queriesDir);

        var timestamp = DateTimeOffset.UtcNow;
        var filename = $"{timestamp:yyyy-MM-ddTHHmmss}Z.json";
        var filepath = Path.Combine(queriesDir, filename);

        var savedResult = new
        {
            sql,
            timestamp = timestamp.ToString("O"),
            requestCharge = result.RequestCharge,
            itemCount = result.Items.Count,
            results = result.Items
        };

        var json = JsonSerializer.Serialize(savedResult, _jsonOptions);
        await File.WriteAllTextAsync(filepath, json, cancellationToken);
    }
}
