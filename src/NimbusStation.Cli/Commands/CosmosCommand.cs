using System.Text.Json;
using Microsoft.Azure.Cosmos;
using NimbusStation.Cli.Output;
using NimbusStation.Core.Commands;
using NimbusStation.Core.Errors;
using NimbusStation.Core.Session;
using NimbusStation.Infrastructure.Configuration;
using NimbusStation.Providers.Azure.Cosmos;
using NimbusStation.Providers.Azure.Errors;

namespace NimbusStation.Cli.Commands;

/// <summary>
/// Command for executing Cosmos DB operations.
/// </summary>
public sealed class CosmosCommand : ICommand
{
    private readonly ICosmosService _cosmosService;
    private readonly IConfigurationService _configurationService;
    private readonly ISessionService _sessionService;

    private static readonly HashSet<string> _subcommands = ["query"];

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

    /// <inheritdoc/>
    public CommandHelpMetadata HelpMetadata { get; } = new()
    {
        Subcommands =
        [
            new("query \"<SQL>\"", "Execute a SQL query against the active container")
        ],
        Flags =
        [
            new("--max-items N", "Limit results returned (default: 100)")
        ],
        Examples =
        [
            new("cosmos query \"SELECT * FROM c\"", "Query all documents"),
            new("cosmos query \"SELECT * FROM c WHERE c.id = 'abc'\"", "Query with filter"),
            new("cosmos query \"SELECT * FROM c\" --max-items 10", "Query with limit")
        ],
        Notes = "Requires an active cosmos context (use 'use cosmos <alias>' first). Results are saved to the session queries directory."
    };

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

        var spinner = new SpinnerService(context.Options);
        var theme = _configurationService.GetTheme();

        try
        {
            var result = await spinner.RunWithSpinnerAsync(
                "Executing query...",
                () => _cosmosService.ExecuteQueryAsync(activeAlias, sql, maxItems, cancellationToken));

            // Output JSON to stdout for piping (use WriteRaw to avoid markup stripping of JSON brackets)
            var json = JsonSerializer.Serialize(result.Items, _jsonOptions);
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json + Environment.NewLine);
            context.Output.WriteRaw(jsonBytes);

            // Output RU charge to stderr so it doesn't interfere with piped JSON
            if (!context.Options.Quiet)
            {
                context.Output.WriteErrorLine($"Request charge: {result.RequestCharge:F2} RU");

                if (result.HasMoreResults)
                    context.Output.WriteErrorLine($"Note: More results available (showing first {result.Items.Count} items)");
            }

            // Save results with metadata to queries directory
            await SaveQueryResultAsync(context.CurrentSession!, sql, result, cancellationToken);

            return CommandResult.Ok(data: result);
        }
        catch (InvalidOperationException ex)
        {
            var error = AzureErrorMapper.FromInvalidOperation(ex, activeAlias);
            ErrorFormatter.WriteError(error, theme.ErrorColor, context.Options.Quiet);
            return CommandResult.Error(error.Message);
        }
        catch (CosmosException ex)
        {
            var error = AzureErrorMapper.FromCosmosException(ex, aliasName: activeAlias);
            ErrorFormatter.WriteError(error, theme.ErrorColor, context.Options.Quiet);
            return CommandResult.Error(error.Message);
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
                if (int.TryParse(args[i + 1], out var parsed) && parsed > 0)
                    maxItems = parsed;
                i++; // Skip the value (for loop will increment again)
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
