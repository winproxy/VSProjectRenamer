using VSProjectRenamer.Core;

namespace VSProjectRenamer.Tests;

public class GuidReGeneratorTests
{
    [Fact]
    public void RegenerateGuids_ReplacesGuidInText()
    {
        const string oldGuid = "12345678-1234-1234-1234-123456789012";
        var input   = $"ProjectGuid = {{{oldGuid}}}";
        var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = GuidReGenerator.RegenerateGuids(input, guidMap);

        Assert.DoesNotContain(oldGuid, result, StringComparison.OrdinalIgnoreCase);
        Assert.Single(guidMap);
    }

    [Fact]
    public void RegenerateGuids_SameGuidReplacedConsistently()
    {
        const string oldGuid = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE";
        var input   = $"{oldGuid} and {oldGuid} again";
        var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = GuidReGenerator.RegenerateGuids(input, guidMap);

        // The new GUID should appear twice (both occurrences replaced consistently)
        Assert.Single(guidMap);
        var newGuid = guidMap[oldGuid];
        Assert.Equal(2, CountOccurrences(result, newGuid));
    }

    [Fact]
    public void RegenerateGuids_LowercaseGuidPreservesCase()
    {
        const string oldGuid = "aabbccdd-eeff-0011-2233-445566778899";
        var input   = oldGuid;
        var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = GuidReGenerator.RegenerateGuids(input, guidMap);

        // Result should be lowercase (matching input style)
        Assert.True(result.ToCharArray()
            .Where(char.IsLetter)
            .All(char.IsLower), "Lowercase GUID should stay lowercase.");
    }

    [Fact]
    public void RegenerateGuids_NoGuids_ReturnsOriginal()
    {
        const string input = "Hello, World! No GUIDs here.";
        var result = GuidReGenerator.RegenerateGuids(input);
        Assert.Equal(input, result);
    }

    // -----------------------------------------------------------------------

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
