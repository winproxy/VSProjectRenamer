using System.Text;
using System.Text.RegularExpressions;

namespace VSProjectRenamer.Core;

/// <summary>
/// Parses a name (PascalCase, camelCase, kebab-case, snake_case …) into its constituent
/// words and generates all 12 naming-convention variants.
/// </summary>
public static class NamingConventionGenerator
{
    // Matches word boundaries inside a PascalCase / camelCase identifier:
    //   lowercase → uppercase        "myProject"  → "my|Project"
    //   uppercase seq → uppercase+lowercase  "ABPFramework" → "ABP|Framework"
    private static readonly Regex PascalBoundary =
        new(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

    // Splits on common non-alphanumeric delimiters: dash, underscore, dot, space
    private static readonly Regex DelimiterSplit =
        new(@"[-_.\s]+", RegexOptions.Compiled);

    /// <summary>
    /// Generates 12 naming-convention variants from the supplied name.
    /// The input may be in any common case (PascalCase, camelCase, snake_case, kebab-case, etc.).
    /// </summary>
    /// <param name="name">Source name in any common naming convention.</param>
    /// <returns>A <see cref="NameVariants"/> record with all 12 variants.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public static NameVariants Generate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must not be empty or whitespace.", nameof(name));

        var words = SplitIntoWords(name);

        // 1. PascalCase  – MyProjectName
        var pascal = string.Concat(words.Select(CapitalizeFirst));

        // 2. camelCase   – myProjectName
        var camel = char.ToLowerInvariant(pascal[0]) + pascal[1..];

        // 3. snake_case  – my_project_name
        var snake = string.Join("_", words.Select(w => w.ToLowerInvariant()));

        // 4. kebab-case  – my-project-name
        var kebab = string.Join("-", words.Select(w => w.ToLowerInvariant()));

        // 5. UPPER_SNAKE_CASE – MY_PROJECT_NAME
        var upperSnake = string.Join("_", words.Select(w => w.ToUpperInvariant()));

        // 6. dot.case – my.project.name
        var dot = string.Join(".", words.Select(w => w.ToLowerInvariant()));

        // 7. Title Case – My Project Name
        var title = string.Join(" ", words.Select(CapitalizeFirst));

        // 8. UPPERFLATCASE – MYPROJECTNAME
        var upperFlat = pascal.ToUpperInvariant();

        // 9. lowerflatcase – myprojectname
        var lowerFlat = pascal.ToLowerInvariant();

        // 10. Acronym – MPN  (first character of each word, uppercase)
        var acronym = new string(words.Select(w => char.ToUpperInvariant(w[0])).ToArray());

        // 11. lower space – my project name
        var lowerSpace = string.Join(" ", words.Select(w => w.ToLowerInvariant()));

        // 12. UPPER SPACE – MY PROJECT NAME
        var upperSpace = string.Join(" ", words.Select(w => w.ToUpperInvariant()));

        return new NameVariants(
            pascal, camel, snake, kebab, upperSnake,
            dot, title, upperFlat, lowerFlat,
            acronym, lowerSpace, upperSpace);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Splits <paramref name="input"/> into words, handling all common naming conventions.
    /// </summary>
    public static string[] SplitIntoWords(string input)
    {
        // Step 1 – split on delimiters (-, _, ., space)
        var parts = DelimiterSplit.Split(input);

        // Step 2 – split each part on PascalCase boundaries
        var words = new List<string>();
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            var subWords = PascalBoundary.Split(part);
            words.AddRange(subWords.Where(w => !string.IsNullOrEmpty(w)));
        }

        return words.Count > 0 ? words.ToArray() : [input];
    }

    private static string CapitalizeFirst(string word)
    {
        if (string.IsNullOrEmpty(word)) return word;
        // Preserve the remaining characters as-is (important for all-caps words like "ABP")
        if (char.IsUpper(word[0])) return word;
        return char.ToUpperInvariant(word[0]) + word[1..];
    }
}
