using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NimbusStation.Core.Aliases;
using NimbusStation.Core.Parsing;
using NimbusStation.Core.Session;

namespace NimbusStation.Infrastructure.Aliases;

/// <summary>
/// Resolves and expands command aliases with support for positional parameters,
/// built-in variables, and alias chaining.
/// </summary>
public sealed partial class AliasResolver : IAliasResolver
{
    private const int MaxRecursionDepth = 10;
    private static readonly string[] SessionRequiredVariables = ["{ticket}", "{session-dir}"];

    private readonly IAliasService _aliasService;
    private readonly ILogger<AliasResolver> _logger;
    private readonly string _sessionsBasePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="AliasResolver"/> class.
    /// </summary>
    public AliasResolver(IAliasService aliasService, ILogger<AliasResolver> logger)
        : this(aliasService, logger, GetDefaultSessionsBasePath())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AliasResolver"/> class with a custom sessions path.
    /// </summary>
    public AliasResolver(IAliasService aliasService, ILogger<AliasResolver> logger, string sessionsBasePath)
    {
        _aliasService = aliasService;
        _logger = logger;
        _sessionsBasePath = sessionsBasePath;
    }

    /// <inheritdoc/>
    public async Task<AliasExpansionResult> ExpandAsync(
        string input,
        Session? currentSession,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return AliasExpansionResult.NoExpansion(input);

        var tokens = InputTokenizer.Tokenize(input, preserveQuotes: true);
        if (tokens.Length == 0)
            return AliasExpansionResult.NoExpansion(input);

        return await ExpandWithChainDetectionAsync(
            commandName: tokens[0],
            arguments: InputTokenizer.GetArguments(tokens),
            currentSession: currentSession,
            expansionChain: [],
            depth: 0,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AliasExpansionResult> TestExpandAsync(
        string aliasName,
        string[] arguments,
        Session? currentSession,
        CancellationToken cancellationToken = default) =>
        ExpandWithChainDetectionAsync(
            commandName: aliasName,
            arguments: arguments,
            currentSession: currentSession,
            expansionChain: [],
            depth: 0,
            cancellationToken: cancellationToken);

    private async Task<AliasExpansionResult> ExpandWithChainDetectionAsync(
        string commandName,
        string[] arguments,
        Session? currentSession,
        HashSet<string> expansionChain,
        int depth,
        CancellationToken cancellationToken)
    {
        if (depth > MaxRecursionDepth)
            return AliasExpansionResult.Failed($"Alias expansion exceeded maximum depth of {MaxRecursionDepth}");

        var expansion = await _aliasService.GetAliasAsync(commandName, cancellationToken);
        if (expansion is null)
        {
            var originalInput = ReconstructInput(commandName, arguments);
            return depth == 0
                ? AliasExpansionResult.NoExpansion(originalInput)
                : AliasExpansionResult.Expanded(originalInput);
        }

        if (!expansionChain.Add(commandName))
        {
            var chain = string.Join(" -> ", expansionChain) + $" -> {commandName}";
            return AliasExpansionResult.Failed($"Circular alias detected: {chain}");
        }

        _logger.LogDebug("Expanding alias '{CommandName}' with {ArgCount} arguments", commandName, arguments.Length);

        var substitutionResult = SubstitutePositionalParameters(aliasName: commandName, expansion: expansion, arguments: arguments);
        if (!substitutionResult.IsSuccess)
            return AliasExpansionResult.Failed(substitutionResult.Error!);

        var variableResult = SubstituteBuiltInVariables(aliasName: commandName, expansion: substitutionResult.Result!, currentSession: currentSession);
        if (!variableResult.IsSuccess)
            return AliasExpansionResult.Failed(variableResult.Error!);

        var expandedText = variableResult.Result!;
        var expandedTokens = InputTokenizer.Tokenize(expandedText, preserveQuotes: true);

        if (expandedTokens.Length > 0)
        {
            var nextCommandName = expandedTokens[0];
            var nextExpansion = await _aliasService.GetAliasAsync(nextCommandName, cancellationToken);

            if (nextExpansion is not null)
            {
                return await ExpandWithChainDetectionAsync(
                    commandName: nextCommandName,
                    arguments: InputTokenizer.GetArguments(expandedTokens),
                    currentSession: currentSession,
                    expansionChain: expansionChain,
                    depth: depth + 1,
                    cancellationToken: cancellationToken);
            }
        }

        return AliasExpansionResult.Expanded(expandedText);
    }

    private static SubstitutionResult SubstitutePositionalParameters(string aliasName, string expansion, string[] arguments)
    {
        var placeholders = PositionalParameterPattern().Matches(expansion);

        if (placeholders.Count == 0)
        {
            return arguments.Length > 0
                ? SubstitutionResult.Success($"{expansion} {string.Join(" ", arguments)}")
                : SubstitutionResult.Success(expansion);
        }

        var maxIndex = placeholders.Select(m => int.Parse(m.Groups[1].Value)).Max();
        var requiredArgs = maxIndex + 1;

        if (arguments.Length < requiredArgs)
        {
            var usageArgs = string.Join(" ", Enumerable.Range(0, requiredArgs).Select(i => $"<arg{i}>"));
            return SubstitutionResult.Failure(
                $"Alias '{aliasName}' requires {requiredArgs} argument(s), but {arguments.Length} provided. Usage: {aliasName} {usageArgs}");
        }

        var result = placeholders.Aggregate(
            expansion,
            (current, match) => current.Replace(match.Value, arguments[int.Parse(match.Groups[1].Value)]));

        return arguments.Length > requiredArgs
            ? SubstitutionResult.Success($"{result} {string.Join(" ", arguments[requiredArgs..])}")
            : SubstitutionResult.Success(result);
    }

    private SubstitutionResult SubstituteBuiltInVariables(string aliasName, string expansion, Session? currentSession)
    {
        if (currentSession is null)
        {
            var missingVar = SessionRequiredVariables.FirstOrDefault(v =>
                expansion.Contains(v, StringComparison.OrdinalIgnoreCase));

            if (missingVar is not null)
                return SubstitutionResult.Failure($"Alias '{aliasName}' requires an active session for {missingVar} variable");
        }

        var result = expansion;

        if (currentSession is not null)
        {
            result = result
                .Replace("{ticket}", currentSession.TicketId, StringComparison.OrdinalIgnoreCase)
                .Replace("{session-dir}", Path.Combine(_sessionsBasePath, currentSession.TicketId), StringComparison.OrdinalIgnoreCase);
        }

        return SubstitutionResult.Success(result
            .Replace("{today}", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{now}", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), StringComparison.OrdinalIgnoreCase)
            .Replace("{user}", Environment.UserName, StringComparison.OrdinalIgnoreCase));
    }

    private static string ReconstructInput(string commandName, string[] arguments) =>
        arguments.Length == 0 ? commandName : $"{commandName} {string.Join(" ", arguments)}";

    private static string GetDefaultSessionsBasePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nimbus", "sessions");

    [GeneratedRegex(@"\{(\d+)\}", RegexOptions.Compiled)]
    private static partial Regex PositionalParameterPattern();

    private sealed record SubstitutionResult(bool IsSuccess, string? Result, string? Error)
    {
        public static SubstitutionResult Success(string result) => new(IsSuccess: true, Result: result, Error: null);
        public static SubstitutionResult Failure(string error) => new(IsSuccess: false, Result: null, Error: error);
    }
}
