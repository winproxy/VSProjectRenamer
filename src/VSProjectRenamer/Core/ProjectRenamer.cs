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

        // Shared GUID map so the same old GUID becomes the same new GUID across files
        var guidMap = _options.RegenerateGuids ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : null;
        var portMap = _options.RandomizePorts  ? new Dictionary<int, int>() : null;

        // Step 1 – process file contents
        ProcessFileContents(workDir, plan, guidMap, portMap);

        // Step 2 – rename files
        RenameFiles(workDir, plan);

        // Step 3 – rename directories (bottom-up, so children are renamed before parents)
        RenameDirectories(workDir, plan);

        _log("Done.");

        // Step 4 – optional dotnet restore
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
                    updated = GuidReGenerator.RegenerateGuids(updated, guidMap);

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

    // -----------------------------------------------------------------------
    // Rename files
    // -----------------------------------------------------------------------

    private void RenameFiles(string directory, ReplacementPlan plan)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
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
                            .OrderByDescending(d => d.Length)
                            .ToList();

        foreach (var dir in dirs)
        {
            var oldName = Path.GetFileName(dir);
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
        };

    private static bool IsIgnoredDirectory(string dirName) =>
        IgnoredDirectories.Contains(dirName);

    // -----------------------------------------------------------------------
    // dotnet restore
    // -----------------------------------------------------------------------

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
                proc.WaitForExit(120_000);
                if (proc.ExitCode != 0)
                    _log($"  [WARN] dotnet restore exited with code {proc.ExitCode}");
            }
            catch (Exception ex)
            {
                _log($"  [WARN] dotnet restore failed: {ex.Message}");
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
