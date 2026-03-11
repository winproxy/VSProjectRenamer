using System.Text.RegularExpressions;

namespace VSProjectRenamer.Core;

/// <summary>
/// Finds and replaces all GUIDs (UUID v4 format) in a string with freshly generated ones.
/// Each unique source GUID is mapped to a single new GUID so that all occurrences of the
/// same GUID are replaced consistently.
/// </summary>
/// <remarks>
/// For <c>.sln</c> files the tool regenerates only <em>project instance</em> GUIDs (the second
/// GUID on a <c>Project(…)</c> line) and keeps project-type GUIDs (the first GUID) intact, so
/// that Visual Studio can still identify the project language/SDK correctly.
/// For <c>.csproj</c> and similar project files the <c>&lt;ProjectGuid&gt;</c> element and the
/// <c>&lt;UserSecretsId&gt;</c> element are regenerated as well.
/// </remarks>
public static class GuidReGenerator
{
    // Matches lowercase and uppercase GUIDs with or without braces / parentheses.
    // Groups: 1 = opening brace/paren (optional), 2 = the hex GUID, 3 = closing brace/paren (optional)
    private static readonly Regex GuidPattern = new(
        @"(\{|\()?([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(\}|\))?",
        RegexOptions.Compiled);

    // .sln: Project("{type-guid}") = "name", "path", "{instance-guid}"
    // We only want to replace the SECOND GUID (instance), not the first (type).
    // Visual Studio uses the type GUID to identify the project language/SDK.
    private static readonly Regex SlnProjectLine = new(
        @"Project\(""\{(?<typeGuid>[0-9A-Fa-f\-]{36})\}""\)\s*=\s*""[^""]*""\s*,\s*""[^""]*""\s*,\s*""\{(?<instanceGuid>[0-9A-Fa-f\-]{36})\}""",
        RegexOptions.Compiled);

    // <ProjectGuid>{guid}</ProjectGuid>
    private static readonly Regex ProjectGuidElement = new(
        @"(<ProjectGuid>)\s*\{?([0-9A-Fa-f\-]{36})\}?\s*(</ProjectGuid>)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // <UserSecretsId>guid-or-any-string</UserSecretsId>
    private static readonly Regex UserSecretsIdElement = new(
        @"(<UserSecretsId>)\s*[^<]+\s*(</UserSecretsId>)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

    /// <summary>
    /// Regenerates <em>only the instance GUIDs</em> in a <c>.sln</c> file, leaving
    /// project-type GUIDs untouched so that Visual Studio can still open the solution.
    /// The <paramref name="guidMap"/> is populated so that the same instance GUID can be
    /// propagated to the corresponding <c>.csproj</c> files by the caller.
    /// </summary>
    public static string RegenerateSlnInstanceGuids(string slnContent, Dictionary<string, string> guidMap)
    {
        return SlnProjectLine.Replace(slnContent, m =>
        {
            var typeGuid     = m.Groups["typeGuid"].Value;
            var instanceGuid = m.Groups["instanceGuid"].Value;

            var normalised = instanceGuid.ToUpperInvariant();
            if (!guidMap.TryGetValue(normalised, out var newInstanceGuid))
            {
                newInstanceGuid = Guid.NewGuid().ToString("D").ToUpperInvariant();
                guidMap[normalised] = newInstanceGuid;
            }

            // Rebuild the matched line, preserving everything except the instance GUID
            return m.Value.Replace(
                $"{{{instanceGuid}}}",
                $"{{{newInstanceGuid}}}",
                StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Replaces the value of <c>&lt;ProjectGuid&gt;</c> using the supplied
    /// <paramref name="guidMap"/> (populated by <see cref="RegenerateSlnInstanceGuids"/> or
    /// <see cref="RegenerateGuids"/>). If the existing GUID is not in the map a new one is
    /// generated and added.
    /// </summary>
    public static string RegenerateProjectGuid(string csprojContent, Dictionary<string, string> guidMap)
    {
        return ProjectGuidElement.Replace(csprojContent, m =>
        {
            var open  = m.Groups[1].Value;
            var guid  = m.Groups[2].Value;
            var close = m.Groups[3].Value;

            var normalised = guid.ToUpperInvariant();
            if (!guidMap.TryGetValue(normalised, out var newGuid))
            {
                newGuid = Guid.NewGuid().ToString("D").ToUpperInvariant();
                guidMap[normalised] = newGuid;
            }

            return $"{open}{{{newGuid}}}{close}";
        });
    }

    /// <summary>
    /// Rotates the <c>&lt;UserSecretsId&gt;</c> element value to a fresh GUID in each
    /// <c>.csproj</c> (or any XML) file.
    /// </summary>
    public static string RotateUserSecretsId(string content) =>
        UserSecretsIdElement.Replace(content,
            m => $"{m.Groups[1].Value}{Guid.NewGuid().ToString("D")}{m.Groups[2].Value}");

    // -----------------------------------------------------------------------

    private static bool AreBracketsBalanced(string open, string close)
    {
        if (string.IsNullOrEmpty(open) && string.IsNullOrEmpty(close)) return true;
        if (open == "{" && close == "}") return true;
        if (open == "(" && close == ")") return true;
        return false;
    }
}
