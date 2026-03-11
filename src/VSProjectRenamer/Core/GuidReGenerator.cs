using System.Text.RegularExpressions;

namespace VSProjectRenamer.Core;

/// <summary>
/// Finds and replaces all GUIDs (UUID v4 format) in a string with freshly generated ones.
/// Each unique source GUID is mapped to a single new GUID so that all occurrences of the
/// same GUID are replaced consistently.
/// </summary>
public static class GuidReGenerator
{
    // Matches lowercase and uppercase GUIDs with or without braces / parentheses.
    // Groups: 1 = opening brace/paren (optional), 2 = the hex GUID, 3 = closing brace/paren (optional)
    private static readonly Regex GuidPattern = new(
        @"(\{|\()?([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(\}|\))?",
        RegexOptions.Compiled);

    /// <summary>
    /// Replaces every GUID in <paramref name="content"/> with a new GUID.
    /// The same source GUID is always replaced by the same new GUID within a single call.
    /// </summary>
    /// <param name="content">Text that may contain GUIDs.</param>
    /// <param name="guidMap">
    /// Optional dictionary to accumulate or pre-seed old→new GUID mappings.
    /// Pass the same instance across multiple files to keep GUIDs consistent.
    /// </param>
    /// <returns>Updated text, or the original string unchanged if no GUIDs were found.</returns>
    public static string RegenerateGuids(string content, Dictionary<string, string>? guidMap = null)
    {
        guidMap ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return GuidPattern.Replace(content, m =>
        {
            var open  = m.Groups[1].Value;
            var guid  = m.Groups[2].Value;
            var close = m.Groups[3].Value;

            // Only process when brackets are balanced (or both absent)
            if (!AreBracketsBalanced(open, close))
                return m.Value;

            var normalised = guid.ToUpperInvariant();
            if (!guidMap.TryGetValue(normalised, out var newGuid))
            {
                newGuid = Guid.NewGuid().ToString().ToUpperInvariant();
                guidMap[normalised] = newGuid;
            }

            // Preserve the original case style of the GUID digits
            var formatted = guid.Any(char.IsLower)
                ? newGuid.ToLowerInvariant()
                : newGuid;

            return $"{open}{formatted}{close}";
        });
    }

    private static bool AreBracketsBalanced(string open, string close)
    {
        if (string.IsNullOrEmpty(open) && string.IsNullOrEmpty(close)) return true;
        if (open == "{" && close == "}") return true;
        if (open == "(" && close == ")") return true;
        return false;
    }
}
