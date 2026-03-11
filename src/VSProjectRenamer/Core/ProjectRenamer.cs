using VSProjectRenamer.Models;

namespace VSProjectRenamer.Core;

/// <summary>
/// Orchestrates a rename or clone-and-rename operation on a project directory.
/// </summary>
public sealed class ProjectRenamer
{
    private readonly RenameOptions _options;
    private readonly Action<string> _log;
    private readonly Action<string> _verbose;

    public ProjectRenamer(RenameOptions options, Action<string>? logger = null)
    {
        _options = options;
        _log     = logger ?? Console.WriteLine;
        _verbose = options.Verbose ? _log : _ => { };
    }

    // -----------------------------------------------------------------------
    // Public entry point
    // -----------------------------------------------------------------------

    /// <summary>
    /// Performs the rename (and optional clone) according to <see cref="RenameOptions"/>.
    /// </summary>
    public void Run()
    {
        var sourceDir = Path.GetFullPath(_options.SourceDirectory);
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        // Build naming-variant maps
        var oldVariants = NamingConventionGenerator.Generate(_options.OldName);
        var newVariants = NamingConventionGenerator.Generate(_options.NewName);
        var plan        = new ReplacementPlan(oldVariants, newVariants);

        if (_options.DryRun)
        {
            _log("[DRY-RUN] Substitution pairs:");
            foreach (var (old, next) in plan.Pairs)
                _log($"  {old}  →  {next}");
            _log(string.Empty);
        }

        // Determine working directory
        string workDir;
        if (!string.IsNullOrEmpty(_options.OutputDirectory))
        {
            var targetDir = Path.GetFullPath(_options.OutputDirectory);
            if (!_options.DryRun)
            {
                _log($"Cloning  {sourceDir}");
                _log($"     →   {targetDir}");
                CopyDirectory(sourceDir, targetDir);
            }
            else
            {
                _log($"[DRY-RUN] Would clone {sourceDir} → {targetDir}");
            }
            workDir = targetDir;
        }
        else
        {
            workDir = sourceDir;
            _log($"Renaming in-place: {workDir}");
        }

        // Step 1 – clean build outputs and lock files (optional)
        if (_options.Clean)
        {
            _log("[1] Cleaning build outputs and lock files...");
            if (!_options.DryRun)
                ProjectCleaner.Clean(workDir, CleanExcludedDirectories);
            else
                _log("[DRY-RUN] Would delete bin, obj, node_modules, lock files, etc.");
        }

        // Shared GUID map so the same old GUID becomes the same new GUID across files
        var guidMap = _options.RegenerateGuids ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : null;
        var portMap = _options.RandomizePorts  ? new Dictionary<int, int>() : null;

        // Step 2 – process file contents
        ProcessFileContents(workDir, plan, guidMap, portMap);

        // Step 3 – rename files
        RenameFiles(workDir, plan);

        // Step 4 – rename directories (bottom-up, so children are renamed before parents)
        RenameDirectories(workDir, plan);

        _log("Done.");

        // Step 5 – optional restore
        if (!_options.NoRestore && !_options.DryRun)
            TryRestore(workDir);
    }

    // -----------------------------------------------------------------------
    // File content processing
    // -----------------------------------------------------------------------

    private void ProcessFileContents(
        string directory,
        ReplacementPlan plan,
        Dictionary<string, string>? guidMap,
        Dictionary<int, int>? portMap)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            // Skip files inside ignored directories (e.g. bin, obj, node_modules, .git)
            if (IsInsideIgnoredDirectory(directory, file))
            {
                _verbose($"  skip (ignored dir)  {RelativePath(directory, file)}");
                continue;
            }

            if (!FileTypeFilter.ShouldProcessContents(file))
            {
                _verbose($"  skip (binary)  {RelativePath(directory, file)}");
                continue;
            }

            try
            {
                var original = File.ReadAllText(file);
                var updated  = plan.Apply(original);

                if (guidMap is not null)
                    updated = ApplyGuidRegeneration(file, updated, guidMap);

                if (portMap is not null)
                    updated = PortRandomizer.RandomizePorts(updated, portMap);

                if (updated == original)
                {
                    _verbose($"  unchanged  {RelativePath(directory, file)}");
                    continue;
                }

                _log($"  update  {RelativePath(directory, file)}");
                if (!_options.DryRun)
                    File.WriteAllText(file, updated);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _log($"  [WARN] Could not process {file}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Applies GUID regeneration to <paramref name="content"/> using file-type-aware logic:
    /// <list type="bullet">
    ///   <item><c>.sln</c> — only instance GUIDs are replaced; project-type GUIDs are preserved.</item>
    ///   <item><c>.csproj</c> / <c>.vbproj</c> / <c>.fsproj</c> — <c>&lt;ProjectGuid&gt;</c>
    ///         is regenerated and <c>&lt;UserSecretsId&gt;</c> is rotated.</item>
    ///   <item>All other files — every GUID is replaced generically.</item>
    /// </list>
    /// </summary>
    private static string ApplyGuidRegeneration(string filePath, string content,
                                                Dictionary<string, string> guidMap)
    {
        var ext = Path.GetExtension(filePath);

        if (ext.Equals(".sln", StringComparison.OrdinalIgnoreCase))
            return GuidReGenerator.RegenerateSlnInstanceGuids(content, guidMap);

        if (ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".vbproj", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".fsproj", StringComparison.OrdinalIgnoreCase))
        {
            var updated = GuidReGenerator.RegenerateProjectGuid(content, guidMap);
            updated     = GuidReGenerator.RotateUserSecretsId(updated);
            return updated;
        }

        return GuidReGenerator.RegenerateGuids(content, guidMap);
    }

    // -----------------------------------------------------------------------
    // Rename files
    // -----------------------------------------------------------------------

    private void RenameFiles(string directory, ReplacementPlan plan)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            // Skip files inside ignored directories
            if (IsInsideIgnoredDirectory(directory, file))
                continue;

            var oldName = Path.GetFileName(file);
            var newName = plan.Apply(oldName);
            if (newName == oldName) continue;

            var newPath = Path.Combine(Path.GetDirectoryName(file)!, newName);
            _log($"  rename file  {RelativePath(directory, file)}  →  {newName}");

            if (!_options.DryRun)
                File.Move(file, newPath);
        }
    }

    // -----------------------------------------------------------------------
    // Rename directories (bottom-up)
    // -----------------------------------------------------------------------

    private void RenameDirectories(string rootDirectory, ReplacementPlan plan)
    {
        // Enumerate all sub-directories, sort deepest-first so children are renamed before parents.
        var dirs = Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.AllDirectories)
                            .Where(d => !IsInsideIgnoredDirectory(rootDirectory, d))
                            .OrderByDescending(d => d.Length)
                            .ToList();

        foreach (var dir in dirs)
        {
            var oldName = Path.GetFileName(dir);
            // Never rename ignored directories themselves
            if (IsIgnoredDirectory(oldName)) continue;

            var newName = plan.Apply(oldName);
            if (newName == oldName) continue;

            var parent  = Path.GetDirectoryName(dir)!;
            var newPath = Path.Combine(parent, newName);
            _log($"  rename dir   {RelativePath(rootDirectory, dir)}  →  {newName}");

            if (!_options.DryRun)
                Directory.Move(dir, newPath);
        }
    }

    // -----------------------------------------------------------------------
    // Clone helper
    // -----------------------------------------------------------------------

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            var destFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var subDir in Directory.EnumerateDirectories(source))
        {
            var dirName = Path.GetFileName(subDir);
            // Skip common build-output and dependency directories
            if (IsIgnoredDirectory(dirName)) continue;

            CopyDirectory(subDir, Path.Combine(destination, dirName));
        }
    }

    private static readonly HashSet<string> IgnoredDirectories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".vs", ".idea",
            "node_modules",
            "bin", "obj",
            ".angular", ".next", ".nuxt",
            "dist", "build", "out",
            "packages",
            "__renamer_backup__",
        };

    // Directories that the clean step must never remove (version control, backup).
    private static readonly HashSet<string> CleanExcludedDirectories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            "__renamer_backup__",
        };

    private static bool IsIgnoredDirectory(string dirName) =>
        IgnoredDirectories.Contains(dirName);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="fullPath"/> is located inside one of the
    /// <see cref="IgnoredDirectories"/> relative to <paramref name="rootDirectory"/>.
    /// </summary>
    private static bool IsInsideIgnoredDirectory(string rootDirectory, string fullPath)
    {
        // Walk the path segments between root and the file/dir to check for ignored ancestors.
        var relative = Path.GetRelativePath(rootDirectory, fullPath);
        // Split on both separators for platform safety
        var parts = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                                   StringSplitOptions.RemoveEmptyEntries);
        // Check every segment except the last (the file/dir name itself)
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (IgnoredDirectories.Contains(parts[i]))
                return true;
        }
        return false;
    }

    // -----------------------------------------------------------------------
    // dotnet restore + frontend package managers
    // -----------------------------------------------------------------------

    private const int DotnetRestoreTimeoutMs        = 120_000; // 2 minutes
    private const int PackageManagerTimeoutMs       = 300_000; // 5 minutes

    private void TryRestore(string directory)
    {
        var slnFiles = Directory.GetFiles(directory, "*.sln", SearchOption.AllDirectories)
                       .Concat(Directory.GetFiles(directory, "*.slnx", SearchOption.AllDirectories))
                       .ToList();

        var restoreTargets = slnFiles.Count > 0
            ? slnFiles
            : Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories).ToList();

        foreach (var target in restoreTargets)
        {
            _log($"dotnet restore {target}");
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("dotnet", $"restore \"{target}\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                };
                using var proc = System.Diagnostics.Process.Start(psi)!;
                proc.WaitForExit(DotnetRestoreTimeoutMs);
                if (proc.ExitCode != 0)
                    _log($"  [WARN] dotnet restore exited with code {proc.ExitCode}");
            }
            catch (Exception ex)
            {
                _log($"  [WARN] dotnet restore failed: {ex.Message}");
            }
        }

        // Frontend package managers: detect lock file to choose the right tool
        foreach (var pkgJson in Directory.EnumerateFiles(directory, "package.json", SearchOption.AllDirectories))
        {
            if (IsInsideIgnoredDirectory(directory, pkgJson)) continue;

            var pkgDir = Path.GetDirectoryName(pkgJson)!;
            var pm = File.Exists(Path.Combine(pkgDir, "bun.lockb"))        ? "bun"  :
                     File.Exists(Path.Combine(pkgDir, "pnpm-lock.yaml"))   ? "pnpm" :
                     File.Exists(Path.Combine(pkgDir, "yarn.lock"))        ? "yarn" : "npm";

            _log($"{pm} install → {RelativePath(directory, pkgDir)}");
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(pm, "install")
                {
                    WorkingDirectory       = pkgDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                };
                using var proc = System.Diagnostics.Process.Start(psi)!;
                proc.WaitForExit(PackageManagerTimeoutMs);
                if (proc.ExitCode != 0)
                    _log($"  [WARN] {pm} install exited with code {proc.ExitCode}");
            }
            catch (Exception ex)
            {
                _log($"  [WARN] {pm} install failed: {ex.Message}");
            }
        }
    }

    // -----------------------------------------------------------------------
    // Utilities
    // -----------------------------------------------------------------------

    private static string RelativePath(string root, string fullPath)
    {
        var rel = Path.GetRelativePath(root, fullPath);
        return rel.Length < fullPath.Length ? rel : fullPath;
    }
}
