using VSProjectRenamer.Core;

namespace VSProjectRenamer.Tests;

public class ProjectCleanerTests : IDisposable
{
    private readonly string _tempDir;

    public ProjectCleanerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "VSProjectRenamerCleanerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Clean_DeletesBuildOutputDirectories()
    {
        // Arrange
        var binDir = Path.Combine(_tempDir, "src", "MyProject", "bin");
        var objDir = Path.Combine(_tempDir, "src", "MyProject", "obj");
        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(objDir);
        File.WriteAllText(Path.Combine(binDir, "MyProject.dll"), "binary");
        File.WriteAllText(Path.Combine(objDir, "MyProject.obj"), "binary");

        // Act
        ProjectCleaner.Clean(_tempDir);

        // Assert
        Assert.False(Directory.Exists(binDir));
        Assert.False(Directory.Exists(objDir));
    }

    [Fact]
    public void Clean_DeletesNodeModulesDirectory()
    {
        // Arrange
        var nodeModules = Path.Combine(_tempDir, "frontend", "node_modules");
        Directory.CreateDirectory(nodeModules);
        File.WriteAllText(Path.Combine(nodeModules, "package.json"), "{}");

        // Act
        ProjectCleaner.Clean(_tempDir);

        // Assert
        Assert.False(Directory.Exists(nodeModules));
    }

    [Fact]
    public void Clean_DeletesLockFiles()
    {
        // Arrange
        var packageLock = Path.Combine(_tempDir, "package-lock.json");
        var yarnLock    = Path.Combine(_tempDir, "yarn.lock");
        var pnpmLock    = Path.Combine(_tempDir, "pnpm-lock.yaml");
        File.WriteAllText(packageLock, "{}");
        File.WriteAllText(yarnLock, "# yarn");
        File.WriteAllText(pnpmLock, "# pnpm");

        // Act
        ProjectCleaner.Clean(_tempDir);

        // Assert
        Assert.False(File.Exists(packageLock));
        Assert.False(File.Exists(yarnLock));
        Assert.False(File.Exists(pnpmLock));
    }

    [Fact]
    public void Clean_PreservesExcludedDirectories()
    {
        // Arrange – .git should never be deleted
        var gitDir = Path.Combine(_tempDir, ".git");
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(Path.Combine(gitDir, "config"), "[core]");

        var excludeDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git" };

        // Act
        ProjectCleaner.Clean(_tempDir, excludeDirs);

        // Assert
        Assert.True(Directory.Exists(gitDir));
    }

    [Fact]
    public void Clean_PreservesNonCleanFiles()
    {
        // Arrange – a regular source file should not be touched
        var srcFile = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(srcFile, "using System;");

        // Act
        ProjectCleaner.Clean(_tempDir);

        // Assert
        Assert.True(File.Exists(srcFile));
    }

    [Fact]
    public void CleanDirectories_ContainsExpectedEntries()
    {
        Assert.Contains("bin",          ProjectCleaner.CleanDirectories);
        Assert.Contains("obj",          ProjectCleaner.CleanDirectories);
        Assert.Contains("node_modules", ProjectCleaner.CleanDirectories);
        Assert.Contains(".angular",     ProjectCleaner.CleanDirectories);
        Assert.Contains(".next",        ProjectCleaner.CleanDirectories);
        Assert.Contains("coverage",     ProjectCleaner.CleanDirectories);
    }

    [Fact]
    public void LockFileNames_ContainsExpectedEntries()
    {
        Assert.Contains("package-lock.json", ProjectCleaner.LockFileNames);
        Assert.Contains("yarn.lock",         ProjectCleaner.LockFileNames);
        Assert.Contains("pnpm-lock.yaml",    ProjectCleaner.LockFileNames);
        Assert.Contains("bun.lockb",         ProjectCleaner.LockFileNames);
    }
}
