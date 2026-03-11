using VSProjectRenamer.Core;
using VSProjectRenamer.Models;

namespace VSProjectRenamer.Tests;

/// <summary>Integration tests that exercise <see cref="ProjectRenamer"/> on a real (temp) directory tree.</summary>
public class ProjectRenamerIntegrationTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectRenamerIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "VSProjectRenamerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -----------------------------------------------------------------------
    // In-place rename
    // -----------------------------------------------------------------------

    [Fact]
    public void Rename_InPlace_RenamesFileContentsAndNames()
    {
        // Arrange – create a small fake project tree
        var projectDir = Path.Combine(_tempDir, "BookStore");
        Directory.CreateDirectory(Path.Combine(projectDir, "src", "BookStore.Domain"));
        File.WriteAllText(
            Path.Combine(projectDir, "src", "BookStore.Domain", "BookStore.Domain.csproj"),
            "<Project><PropertyGroup><AssemblyName>BookStore.Domain</AssemblyName></PropertyGroup></Project>");
        File.WriteAllText(
            Path.Combine(projectDir, "BookStore.sln"),
            "Project = \"BookStore\"");
        File.WriteAllText(
            Path.Combine(projectDir, "src", "BookStore.Domain", "BookStoreService.cs"),
            "namespace BookStore.Domain { public class BookStoreService {} }");

        var opts = new RenameOptions
        {
            SourceDirectory = projectDir,
            OldName         = "BookStore",
            NewName         = "LibrarySystem",
            NoRestore       = true,
        };

        // Act
        new ProjectRenamer(opts).Run();

        // Assert – file contents replaced
        var sln = File.ReadAllText(Path.Combine(projectDir, "LibrarySystem.sln"));
        Assert.Contains("LibrarySystem", sln);
        Assert.DoesNotContain("BookStore", sln);

        // Assert – files and directories renamed
        Assert.False(File.Exists(Path.Combine(projectDir, "BookStore.sln")));
        Assert.True(File.Exists(Path.Combine(projectDir, "LibrarySystem.sln")));

        Assert.True(Directory.Exists(Path.Combine(projectDir, "src", "LibrarySystem.Domain")));
        Assert.False(Directory.Exists(Path.Combine(projectDir, "src", "BookStore.Domain")));
    }

    // -----------------------------------------------------------------------
    // Clone-and-rename
    // -----------------------------------------------------------------------

    [Fact]
    public void Clone_CreatesNewDirectoryWithRenamedContent()
    {
        // Arrange
        var sourceDir = Path.Combine(_tempDir, "SourceProject");
        var targetDir = Path.Combine(_tempDir, "TargetProject");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "OldProject.sln"), "OldProject solution");
        File.WriteAllText(Path.Combine(sourceDir, "README.md"),       "# OldProject");

        var opts = new RenameOptions
        {
            SourceDirectory = sourceDir,
            OutputDirectory = targetDir,
            OldName         = "OldProject",
            NewName         = "NewProject",
            NoRestore       = true,
        };

        // Act
        new ProjectRenamer(opts).Run();

        // Assert – source is untouched
        Assert.True(File.Exists(Path.Combine(sourceDir, "OldProject.sln")));

        // Assert – target has renamed content
        Assert.True(File.Exists(Path.Combine(targetDir, "NewProject.sln")));
        var readme = File.ReadAllText(Path.Combine(targetDir, "README.md"));
        Assert.Contains("NewProject", readme);
        Assert.DoesNotContain("OldProject", readme);
    }

    // -----------------------------------------------------------------------
    // Dry run
    // -----------------------------------------------------------------------

    [Fact]
    public void Rename_DryRun_DoesNotModifyFiles()
    {
        var projectDir = Path.Combine(_tempDir, "DryRunProject");
        Directory.CreateDirectory(projectDir);
        const string originalContent = "namespace OldProject { }";
        var filePath = Path.Combine(projectDir, "OldProject.cs");
        File.WriteAllText(filePath, originalContent);

        var opts = new RenameOptions
        {
            SourceDirectory = projectDir,
            OldName         = "OldProject",
            NewName         = "NewProject",
            DryRun          = true,
            NoRestore       = true,
        };

        new ProjectRenamer(opts).Run();

        // File must be untouched
        Assert.Equal(originalContent, File.ReadAllText(filePath));
        Assert.True(File.Exists(filePath), "File should not have been renamed in dry-run mode.");
    }

    // -----------------------------------------------------------------------
    // GUID regeneration
    // -----------------------------------------------------------------------

    [Fact]
    public void Rename_RegenerateGuids_ReplacesGuids()
    {
        var projectDir = Path.Combine(_tempDir, "GuidProject");
        Directory.CreateDirectory(projectDir);
        const string oldGuid = "12345678-1234-1234-1234-123456789012";
        File.WriteAllText(
            Path.Combine(projectDir, "project.csproj"),
            $"<ProjectGuid>{{{oldGuid}}}</ProjectGuid>");

        var opts = new RenameOptions
        {
            SourceDirectory = projectDir,
            OldName         = "GuidProject",
            NewName         = "GuidProject",   // name unchanged; only GUIDs change
            RegenerateGuids = true,
            NoRestore       = true,
        };

        new ProjectRenamer(opts).Run();

        var content = File.ReadAllText(Path.Combine(projectDir, "project.csproj"));
        Assert.DoesNotContain(oldGuid, content, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Port randomisation
    // -----------------------------------------------------------------------

    [Fact]
    public void Rename_RandomizePorts_ReplacesPorts()
    {
        var projectDir = Path.Combine(_tempDir, "PortProject");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(
            Path.Combine(projectDir, "appsettings.json"),
            "{ \"port\": 5000 }");

        var opts = new RenameOptions
        {
            SourceDirectory = projectDir,
            OldName         = "PortProject",
            NewName         = "PortProject",
            RandomizePorts  = true,
            NoRestore       = true,
        };

        new ProjectRenamer(opts).Run();

        var content = File.ReadAllText(Path.Combine(projectDir, "appsettings.json"));
        Assert.DoesNotContain("5000", content);
    }
}
