using System.Text.RegularExpressions;

namespace NimbusStation.Infrastructure.Configuration.Generators;

/// <summary>
/// Performs template variable substitution for alias generation.
/// </summary>
public static partial class TemplateSubstitutor
{
    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.Compiled)]
    private static partial Regex TemplateVariableRegex();

    /// <summary>
    /// Substitutes variables in a template string with values from the provided dictionary.
    /// Variables are specified as {variable_name}.
    /// </summary>
    /// <param name="template">The template string containing {variable} placeholders.</param>
    /// <param name="variables">Dictionary of variable names to their values.</param>
    /// <returns>The template with all variables substituted.</returns>
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
    /// Checks if a template has any unresolved variables.
    /// </summary>
    /// <param name="result">The substituted result string.</param>
    /// <returns>True if there are unresolved {variable} patterns.</returns>
    public static bool HasUnresolvedVariables(string result) =>
        TemplateVariableRegex().IsMatch(result);

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
}
