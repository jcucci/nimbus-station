using System.Text.RegularExpressions;

namespace NimbusStation.Infrastructure.Configuration.Generators;

/// <summary>
/// Performs variable substitution in template strings.
/// Variables use the format {variableName}.
/// </summary>
public static partial class TemplateSubstitutor
{
    /// <summary>
    /// Substitutes {variable} placeholders with values from the context.
    /// </summary>
    /// <param name="template">The template string with {placeholders}.</param>
    /// <param name="context">Dictionary of variable names to values.</param>
    /// <returns>The expanded string with all variables substituted.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when a required variable is not in the context.</exception>
    public static string Substitute(string template, IReadOnlyDictionary<string, string> context)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return VariablePattern().Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;
            if (context.TryGetValue(variableName, out var value))
                return value;

            throw new KeyNotFoundException($"Template variable '{{{variableName}}}' not found in context");
        });
    }

    /// <summary>
    /// Attempts to substitute variables, returning null if any variable is missing.
    /// </summary>
    /// <param name="template">The template string with {placeholders}.</param>
    /// <param name="context">Dictionary of variable names to values.</param>
    /// <returns>The expanded string, or null if a variable is missing.</returns>
    public static string? TrySubstitute(string template, IReadOnlyDictionary<string, string> context)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        var hasMissing = false;

        var substituted = VariablePattern().Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;
            if (context.TryGetValue(variableName, out var value))
                return value;

            hasMissing = true;
            return match.Value; // Keep original to continue processing
        });

        return hasMissing ? null : substituted;
    }

    /// <summary>
    /// Extracts all variable names from a template.
    /// </summary>
    /// <param name="template">The template string.</param>
    /// <returns>List of variable names found in the template.</returns>
    public static IReadOnlyList<string> ExtractVariables(string template)
    {
        if (string.IsNullOrEmpty(template))
            return [];

        return VariablePattern()
            .Matches(template)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Checks if a template contains any variables.
    /// </summary>
    public static bool HasVariables(string template) =>
        !string.IsNullOrEmpty(template) && VariablePattern().IsMatch(template);

    [GeneratedRegex(@"\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled)]
    private static partial Regex VariablePattern();
}
