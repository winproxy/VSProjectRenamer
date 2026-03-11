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

    // -----------------------------------------------------------------------
    // .sln instance GUID regeneration
    // -----------------------------------------------------------------------

    [Fact]
    public void RegenerateSlnInstanceGuids_ReplacesInstanceGuid()
    {
        const string typeGuid     = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"; // C# project type
        const string instanceGuid = "12345678-1234-1234-1234-123456789012";
        var input = $"Project(\"{{{typeGuid}}}\") = \"MyProject\", \"src\\MyProject.csproj\", \"{{{instanceGuid}}}\"";

        var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var result  = GuidReGenerator.RegenerateSlnInstanceGuids(input, guidMap);

        // Instance GUID must be replaced
        Assert.DoesNotContain(instanceGuid, result, StringComparison.OrdinalIgnoreCase);
        // Type GUID must NOT be replaced
        Assert.Contains(typeGuid, result, StringComparison.OrdinalIgnoreCase);
        // Map must contain exactly one entry (the instance GUID)
        Assert.Single(guidMap);
    }

    [Fact]
    public void RegenerateSlnInstanceGuids_SameInstanceGuidReplacedConsistently()
    {
        const string typeGuid     = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
        const string instanceGuid = "AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE";
        var line1 = $"Project(\"{{{typeGuid}}}\") = \"A\", \"A.csproj\", \"{{{instanceGuid}}}\"";
        var line2 = $"Project(\"{{{typeGuid}}}\") = \"B\", \"B.csproj\", \"{{{instanceGuid}}}\"";
        var input = $"{line1}\n{line2}";

        var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var result  = GuidReGenerator.RegenerateSlnInstanceGuids(input, guidMap);

        // Both lines should use the same new GUID
        Assert.Single(guidMap);
        var newGuid = guidMap[instanceGuid.ToUpperInvariant()];
        Assert.Equal(2, CountOccurrences(result, newGuid));
    }

    // -----------------------------------------------------------------------
    // <ProjectGuid> regeneration
    // -----------------------------------------------------------------------

    [Fact]
    public void RegenerateProjectGuid_ReplacesProjectGuidElement()
    {
        const string oldGuid = "AABBCCDD-EEFF-0011-2233-445566778899";
        var input   = $"<Project><PropertyGroup><ProjectGuid>{{{oldGuid}}}</ProjectGuid></PropertyGroup></Project>";
        var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = GuidReGenerator.RegenerateProjectGuid(input, guidMap);

        Assert.DoesNotContain(oldGuid, result, StringComparison.OrdinalIgnoreCase);
        Assert.Single(guidMap);
    }

    [Fact]
    public void RegenerateProjectGuid_UsesExistingMapEntry()
    {
        const string oldGuid = "AABBCCDD-EEFF-0011-2233-445566778899";
        const string newGuid = "11223344-5566-7788-9900-AABBCCDDEEFF";
        var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [oldGuid.ToUpperInvariant()] = newGuid
        };
        var input = $"<ProjectGuid>{{{oldGuid}}}</ProjectGuid>";

        var result = GuidReGenerator.RegenerateProjectGuid(input, guidMap);

        Assert.Contains(newGuid, result, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // <UserSecretsId> rotation
    // -----------------------------------------------------------------------

    [Fact]
    public void RotateUserSecretsId_ReplacesValue()
    {
        const string oldId = "old-secrets-id-that-is-not-a-guid";
        var input  = $"<UserSecretsId>{oldId}</UserSecretsId>";
        var result = GuidReGenerator.RotateUserSecretsId(input);

        Assert.DoesNotContain(oldId, result, StringComparison.OrdinalIgnoreCase);
        // The replacement should look like a GUID
        Assert.Matches(
            @"<UserSecretsId>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}</UserSecretsId>",
            result);
    }

    [Fact]
    public void RotateUserSecretsId_NoElement_ReturnsOriginal()
    {
        const string input = "<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup></Project>";
        var result = GuidReGenerator.RotateUserSecretsId(input);
        Assert.Equal(input, result);
    }
}
