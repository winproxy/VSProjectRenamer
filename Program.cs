#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#endif
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

Console.Title = "Project Renamer Cloner";
Console.OutputEncoding = Encoding.UTF8;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔═══════════════════════════════════════╗");
Console.WriteLine("║       PROJECT RENAMER CLONER          ║");
Console.WriteLine("╚═══════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

string Ask(string text, string? def = null)
{
    Console.Write(text);
    var v = Console.ReadLine();
    return string.IsNullOrWhiteSpace(v) ? def ?? "" : v.Trim();
}

var root = Directory.GetCurrentDirectory();
var backupDir = Path.Combine(root, "__renamer_backup__");

if (Directory.Exists(backupDir))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  ⚠ Previous backup found in __renamer_backup__");
    Console.ResetColor();
    var restoreChoice = Ask("  Restore from backup? (Y/N): ", "N");
    if (restoreChoice.Equals("Y", StringComparison.OrdinalIgnoreCase))
    {
        Console.Write("  Restoring...");

        var selfPath = GetProcessPath();
        foreach (var d in Directory.GetDirectories(root))
        {
            var n = Path.GetFileName(d);
            if (n is "__renamer_backup__" or ".git") continue;
            try { Directory.Delete(d, true); } catch { }
        }
        foreach (var f in Directory.GetFiles(root))
        {
            if (string.Equals(f, selfPath, StringComparison.OrdinalIgnoreCase)) continue;
            try { File.Delete(f); } catch { }
        }

        CopyDirectoryRecursive(backupDir, root,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "__renamer_backup__" });

        try { Directory.Delete(backupDir, true); } catch { }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" ✓ Restored successfully.");
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  Press any key to exit...");
        Console.ResetColor();
        Console.ReadKey(true);
        return;
    }
    Console.WriteLine();
}

var oldName = "";
while (string.IsNullOrWhiteSpace(oldName))
{
    oldName = Ask("  Old Project Name (required): ");
    if (oldName.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        oldName.Equals("quit", StringComparison.OrdinalIgnoreCase))
        return;
    if (string.IsNullOrWhiteSpace(oldName))
        Console.WriteLine("  ⚠ Old name cannot be empty. Type 'exit' to quit.\n");
}

var newName = "";
while (string.IsNullOrWhiteSpace(newName))
{
    newName = Ask("  New Project Name (required): ");
    if (newName.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        newName.Equals("quit", StringComparison.OrdinalIgnoreCase))
        return;
    if (string.IsNullOrWhiteSpace(newName))
        Console.WriteLine("  ⚠ New name cannot be empty. Type 'exit' to quit.\n");
}

var key1Old = Ask("  Key1 OLD (optional): ");
var key1New = Ask("  Key1 NEW (optional): ");

var key2Old = Ask("  Key2 OLD (optional): ");
var key2New = Ask("  Key2 NEW (optional): ");

// --- PascalCase Word Splitter ---
List<string> SplitPascalCase(string name)
{
    var words = new List<string>();
    var current = new StringBuilder();

    for (var i = 0; i < name.Length; i++)
    {
        if (i > 0 && char.IsUpper(name[i]))
        {
            if (char.IsLower(name[i - 1]) ||
                (i + 1 < name.Length && char.IsLower(name[i + 1]) && char.IsUpper(name[i - 1])))
            {
                if (current.Length > 0)
                {
                    words.Add(current.ToString());
                    current.Clear();
                }
            }
        }

        current.Append(name[i]);
    }

    if (current.Length > 0)
        words.Add(current.ToString());

    return words;
}

string Capitalize(string s) =>
    s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();

string Decapitalize(string s) =>
    s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);

// --- Build replacement map with all case variants ---
var oldWords = SplitPascalCase(oldName);
var newWords = SplitPascalCase(newName);

var replacements = new Dictionary<string, string>(StringComparer.Ordinal);

var oldPascal = string.Join("", oldWords.Select(Capitalize));
var newPascal = string.Join("", newWords.Select(Capitalize));

replacements[oldPascal] = newPascal;
replacements[Decapitalize(oldPascal)] = Decapitalize(newPascal);
replacements[oldPascal.ToLowerInvariant()] = newPascal.ToLowerInvariant();
replacements[oldPascal.ToUpperInvariant()] = newPascal.ToUpperInvariant();

replacements[string.Join("-", oldWords.Select(w => w.ToLowerInvariant()))] =
    string.Join("-", newWords.Select(w => w.ToLowerInvariant()));
replacements[string.Join("-", oldWords.Select(w => w.ToUpperInvariant()))] =
    string.Join("-", newWords.Select(w => w.ToUpperInvariant()));

replacements[string.Join("_", oldWords.Select(w => w.ToLowerInvariant()))] =
    string.Join("_", newWords.Select(w => w.ToLowerInvariant()));
replacements[string.Join("_", oldWords.Select(w => w.ToUpperInvariant()))] =
    string.Join("_", newWords.Select(w => w.ToUpperInvariant()));

replacements[string.Join(".", oldWords.Select(w => w.ToLowerInvariant()))] =
    string.Join(".", newWords.Select(w => w.ToLowerInvariant()));

if (oldWords.Count > 1)
{
    replacements[string.Join(".", oldWords.Select(Capitalize))] =
        string.Join(".", newWords.Select(Capitalize));
}

replacements[string.Join(" ", oldWords.Select(Capitalize))] =
    string.Join(" ", newWords.Select(Capitalize));
replacements[string.Join(" ", oldWords.Select(w => w.ToLowerInvariant()))] =
    string.Join(" ", newWords.Select(w => w.ToLowerInvariant()));

foreach (var key in replacements
             .Where(kv => kv.Key == kv.Value || string.IsNullOrEmpty(kv.Key))
             .Select(kv => kv.Key).ToList())
    replacements.Remove(key);

if (!string.IsNullOrEmpty(key1Old))
    replacements[key1Old] = key1New;
if (!string.IsNullOrEmpty(key2Old))
    replacements[key2Old] = key2New;

var sortedReplacements = replacements
    .OrderByDescending(kv => kv.Key.Length)
    .ToList();

Console.WriteLine("\n  Replacements:");
Console.ForegroundColor = ConsoleColor.DarkGray;
foreach (var (from, to) in sortedReplacements)
    Console.WriteLine($"    {from} → {to}");
Console.ResetColor();
Console.WriteLine();

var confirm = Ask("  Proceed with rename? (Y/N): ", "Y");
if (!confirm.Equals("Y", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("  Cancelled.");
    return;
}

var wantBackup = Ask("  Create backup before proceeding? (Y/N): ", "Y");
if (wantBackup.Equals("Y", StringComparison.OrdinalIgnoreCase))
{
    Console.Write("  Creating backup...");
    if (Directory.Exists(backupDir))
        try { Directory.Delete(backupDir, true); } catch { }

    var backupExcludeDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", ".angular", ".next", ".nuxt", ".turbo",
        ".cache", ".parcel-cache", "coverage", "Pods",
        ".git", "__renamer_backup__"
    };
    CopyDirectoryRecursive(root, backupDir, backupExcludeDirs);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(" ✓");
    Console.ResetColor();
}

// ─────────────────────────────────────────────
//  Helpers
// ─────────────────────────────────────────────
int modifiedCount = 0, fileRenamedCount = 0, dirRenamedCount = 0, errorCount = 0;
var sw = Stopwatch.StartNew();

string? GetProcessPath()
{
#if NET6_0_OR_GREATER
    return Environment.ProcessPath;
#else
    try { return Process.GetCurrentProcess().MainModule?.FileName; }
    catch { return null; }
#endif
}

string RelativePath(string basePath, string fullPath)
{
#if NETFRAMEWORK
    if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
        basePath += Path.DirectorySeparatorChar;
    var baseUri = new Uri(basePath);
    var fullUri = new Uri(fullPath);
    return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString())
              .Replace('/', Path.DirectorySeparatorChar);
#else
    return Path.GetRelativePath(basePath, fullPath);
#endif
}

string ReplaceOrdinalIgnoreCase(string input, string oldValue, string newValue)
{
#if NETFRAMEWORK
    return Regex.Replace(input, Regex.Escape(oldValue), newValue.Replace("$", "$$"), RegexOptions.IgnoreCase);
#else
    return input.Replace(oldValue, newValue, StringComparison.OrdinalIgnoreCase);
#endif
}

string ApplyReplacements(string input)
{
    foreach (var (from, to) in sortedReplacements)
        input = input.Replace(from, to);
    return input;
}

bool ShouldSkip(string path)
{
    var normalized = path.Replace('\\', '/');
    return normalized.Contains("/.git/") ||
           normalized.Contains("/node_modules/") ||
           normalized.Contains("/bin/") ||
           normalized.Contains("/obj/") ||
           normalized.Contains("/__renamer_backup__/");
}

void CopyDirectoryRecursive(string source, string destination, HashSet<string> excludeDirs)
{
    Directory.CreateDirectory(destination);
    foreach (var file in Directory.GetFiles(source))
    {
        var destFile = Path.Combine(destination, Path.GetFileName(file));
        try { File.Copy(file, destFile, true); } catch { }
    }
    foreach (var subDir in Directory.GetDirectories(source))
    {
        var dirName = Path.GetFileName(subDir);
        if (excludeDirs.Contains(dirName)) continue;
        CopyDirectoryRecursive(subDir, Path.Combine(destination, dirName), excludeDirs);
    }
}

int GetWidth()
{
    try { return Console.WindowWidth; } catch { return 80; }
}

void WriteProgress(string label, int current, int total, string? item = null)
{
    var width = GetWidth();
    var pct = total == 0 ? 100 : (int)((double)current / total * 100);
    const int barLen = 30;
    var filled = barLen * pct / 100;

    var bar = $"{new string('█', filled)}{new string('░', barLen - filled)}";
    var text = $"    {label,-9} [{bar}] {pct,3}% ({current}/{total})";

    if (item is not null)
    {
        var space = width - text.Length - 2;
        if (space > 10)
        {
            if (item.Length > space)
                item = "…" + item.Substring(item.Length - (space - 1));
            text += $" {item}";
        }
    }

    text = text.PadRight(width - 1);
    if (text.Length > width - 1)
        text = text.Substring(0, width - 1);

    Console.Write($"\r{text}");
}

void EndProgress() => Console.WriteLine();

var stepNum = 0;
const int totalSteps = 8;

void Step(string title)
{
    stepNum++;
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.Write($"  [{stepNum}/{totalSteps}] ");
    Console.ResetColor();
    Console.WriteLine(title);
}

// ─────────────────────────────────────────────
//  [1/8] Clean build outputs, caches & locks
// ─────────────────────────────────────────────
Step("Cleaning build outputs, caches & lock files...");

var dirsToClean = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "bin", "obj",
    "node_modules", ".angular", ".next", ".nuxt", ".turbo",
    ".cache", ".parcel-cache", "coverage",
    "Pods"
};

foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
{
    if (ShouldSkip(dir + Path.DirectorySeparatorChar)) continue;
    var name = Path.GetFileName(dir);
    if (dirsToClean.Contains(name))
    {
        try { Directory.Delete(dir, true); } catch { }
    }
}

foreach (var lockName in new[] { "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "bun.lockb" })
{
    foreach (var lockFile in Directory.EnumerateFiles(root, lockName, SearchOption.AllDirectories))
    {
        if (ShouldSkip(lockFile)) continue;
        try { File.Delete(lockFile); } catch { }
    }
}

// ─────────────────────────────────────────────
//  Collect files
// ─────────────────────────────────────────────
var textExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".cs", ".csproj", ".props", ".targets", ".sln", ".slnx",
    ".razor", ".cshtml", ".vb", ".fs", ".fsproj", ".vbproj",
    ".resx", ".xaml", ".axaml",
    ".json", ".jsonc", ".config", ".xml", ".yml", ".yaml", ".toml", ".ini", ".conf",
    ".runsettings", ".ruleset", ".DotSettings",
    ".html", ".htm", ".css", ".scss", ".sass", ".less",
    ".js", ".jsx", ".ts", ".tsx", ".mjs", ".cjs",
    ".vue", ".svelte",
    ".component", ".service", ".module", ".directive", ".pipe",
    ".java", ".kt", ".kts", ".gradle",
    ".swift", ".m", ".h",
    ".plist", ".pbxproj", ".xcscheme", ".xcworkspacedata",
    ".storyboard", ".xib", ".strings", ".entitlements",
    ".podspec",
    ".md", ".txt", ".editorconfig", ".gitignore", ".dockerignore",
    ".gitattributes", ".npmrc",
    ".dockerfile", ".env",
    ".cmd", ".bat", ".ps1", ".sh",
    ".proto", ".graphql", ".gql",
    ".sql",
    ".http", ".rest"
};

var knownFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "Dockerfile", "Podfile", "Gemfile", "Makefile", "Procfile",
    ".browserslistrc", ".babelrc", ".eslintrc", ".prettierrc"
};

bool IsTextFile(string path) =>
    textExtensions.Contains(Path.GetExtension(path)) ||
    knownFileNames.Contains(Path.GetFileName(path)) ||
    Path.GetFileName(path).StartsWith(".env.", StringComparison.OrdinalIgnoreCase);

var selfExe = GetProcessPath();

var files = Directory
    .EnumerateFiles(root, "*", SearchOption.AllDirectories)
    .Where(f => !ShouldSkip(f))
    .Where(f => !string.Equals(f, selfExe, StringComparison.OrdinalIgnoreCase))
    .ToList();

// ─────────────────────────────────────────────
//  [2/8] Content replacement
// ─────────────────────────────────────────────
Step($"Replacing file contents... ({files.Count} files scanned)");

var textFiles = files
    .Where(IsTextFile)
    .ToList();

for (var i = 0; i < textFiles.Count; i++)
{
    var file = textFiles[i];
    WriteProgress("Content", i + 1, textFiles.Count, RelativePath(root, file));

    try
    {
        var content = File.ReadAllText(file, Encoding.UTF8);
        var updated = ApplyReplacements(content);

        if (content != updated)
        {
            File.WriteAllText(file, updated, new UTF8Encoding(false));
            modifiedCount++;
        }
    }
    catch { errorCount++; }
}
EndProgress();

// ─────────────────────────────────────────────
//  [3/8] File renaming
// ─────────────────────────────────────────────
Step("Renaming files...");

var sortedFiles = files.OrderByDescending(f => f.Length).ToList();

for (var i = 0; i < sortedFiles.Count; i++)
{
    var file = sortedFiles[i];
    WriteProgress("Files", i + 1, sortedFiles.Count, RelativePath(root, file));

    if (!File.Exists(file)) continue;

    var dir = Path.GetDirectoryName(file)!;
    var name = Path.GetFileName(file);
    var newFile = ApplyReplacements(name);

    if (name != newFile)
    {
        var newPath = Path.Combine(dir, newFile);
        try
        {
            if (!File.Exists(newPath))
            {
                File.Move(file, newPath);
                fileRenamedCount++;
            }
        }
        catch { errorCount++; }
    }
}
EndProgress();

// ─────────────────────────────────────────────
//  [4/8] Directory renaming
// ─────────────────────────────────────────────
Step("Renaming directories (leaf-first)...");

var directories = Directory
    .EnumerateDirectories(root, "*", SearchOption.AllDirectories)
    .Where(d => !ShouldSkip(d + Path.DirectorySeparatorChar))
    .OrderByDescending(d => d.Length)
    .ToList();

for (var i = 0; i < directories.Count; i++)
{
    var dir = directories[i];
    WriteProgress("Dirs", i + 1, directories.Count, RelativePath(root, dir));

    if (!Directory.Exists(dir)) continue;

    var name = Path.GetFileName(dir);
    if (name.StartsWith(".")) continue;

    var newDirName = ApplyReplacements(name);
    if (name == newDirName) continue;

    var parent = Path.GetDirectoryName(dir)!;
    var newPath = Path.Combine(parent, newDirName);

    try
    {
        if (!Directory.Exists(newPath))
        {
            Directory.Move(dir, newPath);
            dirRenamedCount++;
        }
    }
    catch { errorCount++; }
}
EndProgress();

// ─────────────────────────────────────────────
//  [5/8] Port randomization
// ─────────────────────────────────────────────
Step("Randomizing application ports...");

var portRandom = new Random();
var usedPorts = new HashSet<int>();
var portMap = new Dictionary<int, int>();

int GetOrCreateNewPort(int originalPort)
{
    if (portMap.TryGetValue(originalPort, out var existing))
        return existing;

    int rangeStart = originalPort / 1000 * 1000;
    int rangeEnd = rangeStart + 1000;
    if (rangeStart < 1024) { rangeStart = 1024; rangeEnd = 2048; }

    int port;
    var attempts = 0;
    do
    {
        port = portRandom.Next(rangeStart, rangeEnd);
        attempts++;
        if (attempts > 500) { usedPorts.Add(port); break; }
    } while (!usedPorts.Add(port));

    portMap[originalPort] = port;
    return port;
}

foreach (var launch in Directory.EnumerateFiles(root, "launchSettings.json", SearchOption.AllDirectories))
{
    if (ShouldSkip(launch)) continue;
    try
    {
        var json = File.ReadAllText(launch);

        json = Regex.Replace(
            json,
            @"""applicationUrl""\s*:\s*""(?<urls>[^""]*)""",
            m =>
            {
                var urls = m.Groups["urls"].Value;
                if (string.IsNullOrWhiteSpace(urls)) return m.Value;

                var newUrls = Regex.Replace(urls, @"(https?)://([^:]+):(\d+)", portMatch =>
                {
                    var scheme = portMatch.Groups[1].Value;
                    var host = portMatch.Groups[2].Value;
                    var originalPort = int.Parse(portMatch.Groups[3].Value);
                    var port = GetOrCreateNewPort(originalPort);
                    return $"{scheme}://{host}:{port}";
                });

                return $"\"applicationUrl\": \"{newUrls}\"";
            },
            RegexOptions.IgnoreCase);

        json = Regex.Replace(
            json,
            @"""sslPort""\s*:\s*(\d+)",
            m =>
            {
                var oldPort = int.Parse(m.Groups[1].Value);
                if (oldPort == 0) return m.Value;
                return $"\"sslPort\": {GetOrCreateNewPort(oldPort)}";
            });

        File.WriteAllText(launch, json, new UTF8Encoding(false));
    }
    catch { }
}

// Extract ports from legacy .NET Framework config files
foreach (var pattern in new[] { "web.config", "*.csproj.user" })
{
    foreach (var file in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
    {
        if (ShouldSkip(file)) continue;
        try
        {
            var content = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(content, @"https?://[^\s""'<>]+?:(\d+)", RegexOptions.IgnoreCase))
            {
                var port = int.Parse(m.Groups[1].Value);
                if (port > 1024) GetOrCreateNewPort(port);
            }
        }
        catch { }
    }
}

foreach (var appHost in Directory.EnumerateFiles(root, "applicationhost.config", SearchOption.AllDirectories))
{
    if (ShouldSkip(appHost)) continue;
    try
    {
        var content = File.ReadAllText(appHost);
        foreach (Match m in Regex.Matches(content, @"bindingInformation=""[^""]*?:(\d+):", RegexOptions.IgnoreCase))
        {
            var port = int.Parse(m.Groups[1].Value);
            if (port > 1024) GetOrCreateNewPort(port);
        }
    }
    catch { }
}

// ─────────────────────────────────────────────
//  [6/8] Propagate port changes to all configs
// ─────────────────────────────────────────────
Step("Propagating port changes to configuration files...");

if (portMap.Count > 0)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    foreach (var (op, np) in portMap.OrderBy(kv => kv.Key))
        Console.WriteLine($"    :{op} → :{np}");
    Console.ResetColor();

    var configFiles = Directory
        .EnumerateFiles(root, "*", SearchOption.AllDirectories)
        .Where(f => !ShouldSkip(f))
        .Where(f => !string.Equals(f, selfExe, StringComparison.OrdinalIgnoreCase))
        .Where(f => IsTextFile(f))
        .ToList();

    var portReplacementList = portMap
        .OrderByDescending(kv => kv.Key.ToString().Length)
        .ThenByDescending(kv => kv.Key)
        .Select(kv =>
        {
            var op = Regex.Escape(kv.Key.ToString());
            var np = kv.Value.ToString();
            return new
            {
                UrlPattern = new Regex(
                    $@"((?:https?://[^\s""'<>]*?|localhost|127\.0\.0\.1|0\.0\.0\.0):){op}(?!\d)",
                    RegexOptions.Compiled),
                UrlRepl = $"${{1}}{np}",
                PropPattern = new Regex(
                    $@"([""']?[\w]*[Pp][Oo][Rr][Tt][""']?\s{{0,5}}[:=]\s{{0,5}}){op}(?!\d)",
                    RegexOptions.Compiled),
                PropRepl = $"${{1}}{np}",
                DockerPattern = new Regex(
                    $@"(?<=[""'\s\-]){op}(?=:\d+[""'\s,\]\r\n])",
                    RegexOptions.Compiled),
                DockerRepl = np,
                IisPattern = new Regex(
                    $@"(bindingInformation\s*=\s*""[^""]*?:){op}(?=:[^""]*"")",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                IisRepl = $"${{1}}{np}"
            };
        })
        .ToList();

    for (var i = 0; i < configFiles.Count; i++)
    {
        var file = configFiles[i];
        WriteProgress("Ports", i + 1, configFiles.Count, RelativePath(root, file));

        try
        {
            var content = File.ReadAllText(file, Encoding.UTF8);
            var updated = content;

            foreach (var pr in portReplacementList)
            {
                updated = pr.UrlPattern.Replace(updated, pr.UrlRepl);
                updated = pr.PropPattern.Replace(updated, pr.PropRepl);
                updated = pr.DockerPattern.Replace(updated, pr.DockerRepl);
                updated = pr.IisPattern.Replace(updated, pr.IisRepl);
            }

            if (content != updated)
            {
                File.WriteAllText(file, updated, new UTF8Encoding(false));
                modifiedCount++;
            }
        }
        catch { errorCount++; }
    }
    EndProgress();
}
else
{
    Console.WriteLine("    No port changes to propagate.");
}

// ─────────────────────────────────────────────
//  [7/8] GUID & UserSecretsId regeneration
// ─────────────────────────────────────────────
Step("Regenerating GUIDs & UserSecretsId...");

var processedProjectFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

var projectGuidRegex = new Regex(
    @"(<ProjectGuid>)\s*\{?[0-9A-Fa-f\-]{36}\}?\s*(</ProjectGuid>)",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

// .sln: Instance GUID regeneration + .csproj sync
var slnProjectRegex = new Regex(
    @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]*""\s*,\s*""(?<path>[^""]*)""\s*,\s*""\{(?<guid>[^}]+)\}""",
    RegexOptions.Compiled);

foreach (var sln in Directory.EnumerateFiles(root, "*.sln", SearchOption.AllDirectories))
{
    if (ShouldSkip(sln)) continue;
    var slnDir = Path.GetDirectoryName(sln)!;
    var text = File.ReadAllText(sln);
    var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (Match match in slnProjectRegex.Matches(text))
    {
        var oldGuid = match.Groups["guid"].Value;
        if (guidMap.ContainsKey(oldGuid)) continue;

        var newGuid = Guid.NewGuid().ToString("D").ToUpper();
        guidMap[oldGuid] = newGuid;

        var projectPath = match.Groups["path"].Value;
        var fullPath = Path.GetFullPath(Path.Combine(slnDir, projectPath));

        if (File.Exists(fullPath))
        {
            processedProjectFiles.Add(fullPath);
            try
            {
                var proj = File.ReadAllText(fullPath);
                var updatedProj = projectGuidRegex.Replace(proj, $"$1{{{newGuid}}}$2");

                if (proj != updatedProj)
                    File.WriteAllText(fullPath, updatedProj, new UTF8Encoding(false));
            }
            catch { }
        }
    }

    foreach (var (oldGuid, newGuid) in guidMap)
        text = ReplaceOrdinalIgnoreCase(text, oldGuid, newGuid);

    File.WriteAllText(sln, text, new UTF8Encoding(false));
}

// .slnx: XML-based solution format
var slnxProjectPathRegex = new Regex(
    @"<Project\s+[^>]*Path\s*=\s*""(?<path>[^""]+)""",
    RegexOptions.Compiled);

foreach (var slnx in Directory.EnumerateFiles(root, "*.slnx", SearchOption.AllDirectories))
{
    if (ShouldSkip(slnx)) continue;
    var slnxDir = Path.GetDirectoryName(slnx)!;
    var text = File.ReadAllText(slnx);

    foreach (Match match in slnxProjectPathRegex.Matches(text))
    {
        var projectPath = match.Groups["path"].Value;
        var fullPath = Path.GetFullPath(Path.Combine(slnxDir, projectPath));

        if (!File.Exists(fullPath)) continue;
        if (processedProjectFiles.Contains(fullPath)) continue;

        processedProjectFiles.Add(fullPath);

        try
        {
            var proj = File.ReadAllText(fullPath);
            if (!projectGuidRegex.IsMatch(proj)) continue;

            var newGuid = Guid.NewGuid().ToString("D").ToUpper();
            var updatedProj = projectGuidRegex.Replace(proj, $"$1{{{newGuid}}}$2");

            if (proj != updatedProj)
                File.WriteAllText(fullPath, updatedProj, new UTF8Encoding(false));
        }
        catch { }
    }
}

// UserSecretsId
var userSecretsRegex = new Regex(
    @"(<UserSecretsId>)\s*[^<]+\s*(</UserSecretsId>)",
    RegexOptions.Compiled);

foreach (var csproj in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
{
    if (ShouldSkip(csproj)) continue;
    try
    {
        var content = File.ReadAllText(csproj);
        if (!userSecretsRegex.IsMatch(content)) continue;

        var updated = userSecretsRegex.Replace(content,
            m => $"{m.Groups[1].Value}{Guid.NewGuid():D}{m.Groups[2].Value}");

        if (content != updated)
            File.WriteAllText(csproj, updated, new UTF8Encoding(false));
    }
    catch { }
}

// ─────────────────────────────────────────────
//  [8/8] Package restore
// ─────────────────────────────────────────────
Step("Restoring packages...");

Console.WriteLine("    dotnet restore");
try
{
    var p = new Process();
    p.StartInfo.FileName = "dotnet";
    p.StartInfo.Arguments = "restore";
    p.StartInfo.UseShellExecute = false;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.RedirectStandardError = true;
    p.Start();
    p.StandardOutput.ReadToEnd();
    p.WaitForExit();
}
catch { }

foreach (var pkgJson in Directory.EnumerateFiles(root, "package.json", SearchOption.AllDirectories))
{
    if (ShouldSkip(pkgJson)) continue;

    var pkgDir = Path.GetDirectoryName(pkgJson)!;
    var pm = File.Exists(Path.Combine(pkgDir, "bun.lockb"))        ? "bun" :
             File.Exists(Path.Combine(pkgDir, "pnpm-lock.yaml")) ? "pnpm" :
             File.Exists(Path.Combine(pkgDir, "yarn.lock"))      ? "yarn" : "npm";

    Console.WriteLine($"    {pm} install → {RelativePath(root, pkgDir)}");
    try
    {
        var p = new Process();
        p.StartInfo.FileName = pm;
        p.StartInfo.Arguments = "install";
        p.StartInfo.WorkingDirectory = pkgDir;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.Start();
        p.StandardOutput.ReadToEnd();
        p.WaitForExit();
    }
    catch { }
}

// ─────────────────────────────────────────────
//  Summary
// ─────────────────────────────────────────────
sw.Stop();

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("  ╔═══════════════════════════════════════╗");
Console.WriteLine("  ║       PROJECT CLONE COMPLETED         ║");
Console.WriteLine("  ╚═══════════════════════════════════════╝");
Console.ResetColor();

Console.WriteLine();
Console.Write("    Files modified:  ");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine($"{modifiedCount}");
Console.ResetColor();

Console.Write("    Files renamed:   ");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine($"{fileRenamedCount}");
Console.ResetColor();

Console.Write("    Dirs renamed:    ");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine($"{dirRenamedCount}");
Console.ResetColor();

Console.Write("    Ports remapped:  ");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine($"{portMap.Count}");
Console.ResetColor();

if (errorCount > 0)
{
    Console.Write("    Errors:          ");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"{errorCount}");
    Console.ResetColor();
}

Console.Write("    Elapsed:         ");
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine($"{sw.Elapsed:mm\\:ss\\.ff}");
Console.ResetColor();

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("  Press any key to exit...");
Console.ResetColor();
Console.ReadKey(true);

#if NETFRAMEWORK
static class NetFrameworkCompat
{
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }
}
#endif