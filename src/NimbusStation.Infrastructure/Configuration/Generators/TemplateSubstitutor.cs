using System.Text.RegularExpressions;

namespace NimbusStation.Infrastructure.Configuration.Generators;

/// <summary>
/// Performs template variable substitution for alias generation.
/// Variables use the format {variableName}.
/// </summary>
public static partial class TemplateSubstitutor
{
    [GeneratedRegex(@"\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled)]
    private static partial Regex TemplateVariableRegex();

    /// <summary>
    /// Substitutes variables in a template string with values from the provided dictionary.
    /// Variables are specified as {variable_name}. Unresolved variables are left as-is.
    /// </summary>
    /// <param name="template">The template string containing {variable} placeholders.</param>
    /// <param name="variables">Dictionary of variable names to their values.</param>
    /// <returns>The template with all found variables substituted.</returns>
    public static string Substitute(string template, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return TemplateVariableRegex().Replace(template, match =>
        {
            string varName = match.Groups[1].Value;
            return variables.TryGetValue(varName, out string? value) ? value : match.Value;
        });
    }

    /// <summary>
    /// Substitutes variables in a template string with values from the provided dictionary.
    /// Variables are specified as {variable_name}. Unresolved variables are left as-is.
    /// </summary>
    /// <param name="template">The template string containing {variable} placeholders.</param>
    /// <param name="context">Read-only dictionary of variable names to their values.</param>
    /// <returns>The template with all found variables substituted.</returns>
    public static string Substitute(string template, IReadOnlyDictionary<string, string> context)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return TemplateVariableRegex().Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;
            return context.TryGetValue(variableName, out var value) ? value : match.Value;
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

        var substituted = TemplateVariableRegex().Replace(template, match =>
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
    /// Checks if a template has any unresolved variables.
    /// </summary>
    /// <param name="result">The substituted result string.</param>
    /// <returns>True if there are unresolved {variable} patterns.</returns>
    public static bool HasUnresolvedVariables(string result) =>
        !string.IsNullOrEmpty(result) && TemplateVariableRegex().IsMatch(result);

    /// <summary>
    /// Checks if a template contains any variables.
    /// </summary>
    public static bool HasVariables(string template) =>
        !string.IsNullOrEmpty(template) && TemplateVariableRegex().IsMatch(template);

    /// <summary>
    /// Gets the list of variable names referenced in a template.
    /// </summary>
    /// <param name="template">The template string.</param>
    /// <returns>List of variable names found in the template.</returns>
    public static IReadOnlyList<string> GetVariables(string template)
    {
        if (string.IsNullOrEmpty(template))
            return [];

        return TemplateVariableRegex()
            .Matches(template)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Extracts all variable names from a template.
    /// </summary>
    /// <param name="template">The template string.</param>
    /// <returns>List of variable names found in the template.</returns>
    public static IReadOnlyList<string> ExtractVariables(string template) => GetVariables(template);
}
