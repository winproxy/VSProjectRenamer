namespace VSProjectRenamer.Core;

/// <summary>
/// Determines which files should have their <em>contents</em> processed.
/// </summary>
public static class FileTypeFilter
{
    /// <summary>File extensions (lower-case, with leading dot) whose contents are processed.</summary>
    public static readonly IReadOnlySet<string> TextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // .NET / C#
        ".cs", ".vb", ".fs", ".fsx", ".fsi",
        ".csproj", ".vbproj", ".fsproj", ".shproj",
        ".sln", ".slnx",
        ".props", ".targets", ".tasks",
        ".config", ".nuspec",
        ".resx", ".resw",
        // Web
        ".html", ".htm", ".cshtml", ".razor", ".aspx", ".ascx", ".master", ".ashx",
        // TypeScript / JavaScript
        ".ts", ".tsx", ".js", ".jsx", ".mjs", ".cjs",
        // Stylesheets
        ".css", ".scss", ".sass", ".less",
        // Data / Config
        ".json", ".jsonc",
        ".yaml", ".yml",
        ".xml", ".xsl", ".xslt", ".xsd",
        ".toml",
        ".ini",
        ".env",
        // Documentation
        ".md", ".mdx", ".txt", ".rst",
        // Scripts
        ".sh", ".bash", ".zsh",
        ".ps1", ".psm1", ".psd1",
        ".cmd", ".bat",
        // Docker / CI
        ".dockerfile",
        ".tf", ".tfvars",
        // API / Protocol
        ".http", ".rest",
        ".proto",
        ".graphql", ".gql",
        // Misc
        ".editorconfig",
        ".gitignore", ".gitattributes",
        ".npmrc", ".yarnrc",
        ".babelrc", ".eslintrc",
        ".prettierrc",
        ".nswag",
        ".abp",
        ".appsettings",
        ".pubxml",
        ".feature",   // SpecFlow / Gherkin
        ".gradle",    // Gradle build scripts
        ".rb",        // Ruby
        ".py",        // Python (sometimes in tooling)
    };

    /// <summary>
    /// Extension-less file names (exact, case-insensitive) whose contents are processed.
    /// </summary>
    public static readonly IReadOnlySet<string> TextFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Dockerfile",
        "docker-compose",
        ".env",
        ".gitignore",
        ".gitattributes",
        ".editorconfig",
        "Makefile",
        "makefile",
        "Procfile",
    };

    /// <summary>Returns <c>true</c> when the file's contents should be processed for text substitution.</summary>
    public static bool ShouldProcessContents(string filePath)
    {
        var ext  = Path.GetExtension(filePath);
        var name = Path.GetFileName(filePath);

        if (!string.IsNullOrEmpty(ext) && TextExtensions.Contains(ext))
            return true;

        if (TextFileNames.Contains(name))
            return true;

        return false;
    }
}
