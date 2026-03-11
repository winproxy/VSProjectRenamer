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

    // -----------------------------------------------------------------------
    // Clean step
    // -----------------------------------------------------------------------

    [Fact]
    public void Rename_Clean_DeletesBuildOutputsBeforeRenaming()
    {
        var projectDir = Path.Combine(_tempDir, "CleanProject");
        Directory.CreateDirectory(projectDir);

        // Create a source file and a bin directory that should be cleaned
        File.WriteAllText(Path.Combine(projectDir, "CleanProject.cs"), "namespace CleanProject {}");
        var binDir = Path.Combine(projectDir, "bin", "Debug");
        Directory.CreateDirectory(binDir);
        File.WriteAllText(Path.Combine(binDir, "CleanProject.dll"), "binary");

        var opts = new RenameOptions
        {
            SourceDirectory = projectDir,
            OldName         = "CleanProject",
            NewName         = "NewProject",
            Clean           = true,
            NoRestore       = true,
        };

        new ProjectRenamer(opts).Run();

        // bin directory must be gone
        Assert.False(Directory.Exists(Path.Combine(projectDir, "bin")));
        // Source file must have been renamed
        Assert.True(File.Exists(Path.Combine(projectDir, "NewProject.cs")));
    }

    // -----------------------------------------------------------------------
    // Directory exclusion (bin/obj/node_modules should not be processed)
    // -----------------------------------------------------------------------

    [Fact]
    public void Rename_IgnoresBinAndObjDirectories()
    {
        var projectDir = Path.Combine(_tempDir, "ExcludeProject");
        Directory.CreateDirectory(projectDir);

        // Source file in project root – must be updated
        File.WriteAllText(Path.Combine(projectDir, "ExcludeProject.cs"), "namespace ExcludeProject {}");

        // Compiled file inside bin – must NOT be renamed or processed
        var binDir = Path.Combine(projectDir, "bin");
        Directory.CreateDirectory(binDir);
        File.WriteAllText(Path.Combine(binDir, "ExcludeProject.xml"), "<assembly>ExcludeProject</assembly>");

        var opts = new RenameOptions
        {
            SourceDirectory = projectDir,
            OldName         = "ExcludeProject",
            NewName         = "RenamedProject",
            NoRestore       = true,
        };

        new ProjectRenamer(opts).Run();

        // Source file was renamed
        Assert.True(File.Exists(Path.Combine(projectDir, "RenamedProject.cs")));
        // Bin file still uses the original name (was skipped)
        Assert.True(File.Exists(Path.Combine(binDir, "ExcludeProject.xml")),
            "Files inside bin/ should not be renamed.");
        var binContent = File.ReadAllText(Path.Combine(binDir, "ExcludeProject.xml"));
        Assert.Equal("<assembly>ExcludeProject</assembly>", binContent);
    }

    // -----------------------------------------------------------------------
    // SLN / CSPROJ GUID regeneration
    // -----------------------------------------------------------------------

    [Fact]
    public void Rename_RegenerateGuids_SlnInstanceGuidReplaced_TypeGuidPreserved()
    {
        var projectDir = Path.Combine(_tempDir, "SlnGuidProject");
        Directory.CreateDirectory(projectDir);

        const string typeGuid     = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
        const string instanceGuid = "12345678-1234-1234-1234-123456789012";
        var slnContent = $"Project(\"{{{typeGuid}}}\") = \"SlnGuidProject\", \"SlnGuidProject.csproj\", \"{{{instanceGuid}}}\"";
        File.WriteAllText(Path.Combine(projectDir, "SlnGuidProject.sln"), slnContent);

        var opts = new RenameOptions
        {
            SourceDirectory = projectDir,
            OldName         = "SlnGuidProject",
            NewName         = "SlnGuidProject",
            RegenerateGuids = true,
            NoRestore       = true,
        };

        new ProjectRenamer(opts).Run();

        var result = File.ReadAllText(Path.Combine(projectDir, "SlnGuidProject.sln"));
        Assert.DoesNotContain(instanceGuid, result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(typeGuid, result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rename_RegenerateGuids_RotatesUserSecretsId()
    {
        var projectDir = Path.Combine(_tempDir, "SecretsProject");
        Directory.CreateDirectory(projectDir);

        const string secretsId = "my-original-secrets-id-12345";
        File.WriteAllText(
            Path.Combine(projectDir, "SecretsProject.csproj"),
            $"<Project><PropertyGroup><UserSecretsId>{secretsId}</UserSecretsId></PropertyGroup></Project>");

        var opts = new RenameOptions
        {
            SourceDirectory = projectDir,
            OldName         = "SecretsProject",
            NewName         = "SecretsProject",
            RegenerateGuids = true,
            NoRestore       = true,
        };

        new ProjectRenamer(opts).Run();

        var content = File.ReadAllText(Path.Combine(projectDir, "SecretsProject.csproj"));
        Assert.DoesNotContain(secretsId, content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<UserSecretsId>", content);
    }
}
