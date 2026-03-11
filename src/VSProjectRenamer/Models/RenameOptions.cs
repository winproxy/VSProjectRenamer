namespace VSProjectRenamer.Models;

/// <summary>Options that control how a rename or clone operation is performed.</summary>
public sealed class RenameOptions
{
    /// <summary>Source project directory to rename or clone from.</summary>
    public required string SourceDirectory { get; init; }

    /// <summary>
    /// Output directory for clone operations.
    /// When <c>null</c> the rename is performed in-place inside <see cref="SourceDirectory"/>.
    /// </summary>
    public string? OutputDirectory { get; init; }

    /// <summary>The existing name to search for (PascalCase).</summary>
    public required string OldName { get; init; }

    /// <summary>The replacement name (PascalCase).</summary>
    public required string NewName { get; init; }

    /// <summary>When <c>true</c> the tool prints planned changes but does not apply them.</summary>
    public bool DryRun { get; init; }

    /// <summary>When <c>true</c> dotnet restore is skipped after the rename.</summary>
    public bool NoRestore { get; init; }

    /// <summary>Enable verbose output.</summary>
    public bool Verbose { get; init; }

    /// <summary>Randomize TCP port numbers found in configuration files.</summary>
    public bool RandomizePorts { get; init; }

    /// <summary>Replace all GUIDs with newly generated ones.</summary>
    public bool RegenerateGuids { get; init; }

    /// <summary>
    /// When <c>true</c>, deletes build-output directories (<c>bin</c>, <c>obj</c>),
    /// front-end caches (<c>node_modules</c>, <c>.angular</c>, <c>.next</c>, …), and
    /// lock files (<c>package-lock.json</c>, <c>yarn.lock</c>, …) before renaming.
    /// </summary>
    public bool Clean { get; init; }
}
