namespace VSProjectRenamer.Core;

/// <summary>
/// Deletes build-output directories, front-end caches, and lock files from a project tree
/// before a rename operation so that generated/hashed content is not processed unnecessarily.
/// </summary>
public static class ProjectCleaner
{
    /// <summary>
    /// Directory names (exact, case-insensitive) that are deleted recursively when found
    /// anywhere inside the project tree.
    /// </summary>
    public static readonly IReadOnlySet<string> CleanDirectories =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // .NET build outputs
            "bin", "obj",
            // JavaScript / TypeScript
            "node_modules",
            ".angular", ".next", ".nuxt", ".turbo",
            ".cache", ".parcel-cache",
            // Test coverage
            "coverage",
            // iOS / macOS
            "Pods",
        };

    /// <summary>
    /// Lock-file names (exact, case-insensitive) that are deleted when found anywhere
    /// inside the project tree. They will be regenerated after the rename by the package
    /// restore step.
    /// </summary>
    public static readonly IReadOnlySet<string> LockFileNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "package-lock.json",
            "yarn.lock",
            "pnpm-lock.yaml",
            "bun.lockb",
        };

    /// <summary>
    /// Removes all <see cref="CleanDirectories"/> and <see cref="LockFileNames"/> found
    /// under <paramref name="rootDirectory"/>, skipping the provided
    /// <paramref name="excludeDirectories"/> set.
    /// </summary>
    /// <param name="rootDirectory">Root directory to clean.</param>
    /// <param name="excludeDirectories">
    /// Directory names that are never entered or deleted (e.g. <c>.git</c>,
    /// <c>__renamer_backup__</c>).
    /// </param>
    public static void Clean(string rootDirectory, IReadOnlySet<string>? excludeDirectories = null)
    {
        excludeDirectories ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Delete matching directories (top-down; Directory.Delete is recursive so we skip
        // directories that are already children of a deleted parent by checking Exists).
        foreach (var dir in Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(dir);
            if (excludeDirectories.Contains(name)) continue;
            if (!CleanDirectories.Contains(name)) continue;
            if (!Directory.Exists(dir)) continue; // already removed as child of earlier entry

            try { Directory.Delete(dir, recursive: true); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* best-effort */ }
        }

        // Delete lock files
        foreach (var file in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (!LockFileNames.Contains(name)) continue;

            try { File.Delete(file); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* best-effort */ }
        }
    }
}
