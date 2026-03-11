namespace VSProjectRenamer.Core;

/// <summary>
/// Builds an ordered list of (oldValue, newValue) substitution pairs from two
/// <see cref="NameVariants"/> instances, then applies them to strings.
/// </summary>
/// <remarks>
/// Pairs are ordered longest-first so that longer variants (e.g. UPPER_SNAKE_CASE) are
/// replaced before their shorter sub-strings (e.g. flat-case).
/// </remarks>
public sealed class ReplacementPlan
{
    private readonly IReadOnlyList<(string Old, string New)> _pairs;

    /// <summary>Creates a replacement plan for the given old/new variant sets.</summary>
    public ReplacementPlan(NameVariants oldVariants, NameVariants newVariants)
    {
        // Zip the 12 variants pairwise, deduplicate, and sort longest-first.
        var oldAll = oldVariants.All().ToArray();
        var newAll = newVariants.All().ToArray();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pairs = new List<(string Old, string New)>();

        for (var i = 0; i < oldAll.Length; i++)
        {
            var oldVal = oldAll[i];
            var newVal = newAll[i];
            if (oldVal == newVal) continue;             // nothing to replace
            if (string.IsNullOrEmpty(oldVal)) continue;
            if (!seen.Add(oldVal)) continue;            // already in the list

            pairs.Add((oldVal, newVal));
        }

        // Sort longest match first to avoid partial replacements.
        pairs.Sort((a, b) => b.Old.Length.CompareTo(a.Old.Length));
        _pairs = pairs;
    }

    /// <summary>Applies all substitutions to <paramref name="text"/> and returns the result.</summary>
    public string Apply(string text)
    {
        foreach (var (old, next) in _pairs)
            text = text.Replace(old, next, StringComparison.Ordinal);
        return text;
    }

    /// <summary>Exposes the raw substitution pairs (for diagnostics / dry-run output).</summary>
    public IReadOnlyList<(string Old, string New)> Pairs => _pairs;
}
