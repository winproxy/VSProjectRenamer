using System.CommandLine;
using System.CommandLine.Invocation;
using VSProjectRenamer.Core;
using VSProjectRenamer.Models;

// ─────────────────────────────────────────────────────────────────────────────
// Root command
// ─────────────────────────────────────────────────────────────────────────────

var rootCommand = new RootCommand(
    "VSProjectRenamer — renames and clones .NET projects with 12 naming-convention variants.");

// ─────────────────────────────────────────────────────────────────────────────
// Shared options (reused by both sub-commands)
// ─────────────────────────────────────────────────────────────────────────────

var dryRunOption = new Option<bool>(
    aliases: ["--dry-run", "-d"],
    description: "Print planned changes without applying them.");

var noRestoreOption = new Option<bool>(
    aliases: ["--no-restore"],
    description: "Skip 'dotnet restore' after the operation.");

var verboseOption = new Option<bool>(
    aliases: ["--verbose", "-v"],
    description: "Enable verbose output.");

var randomizePortsOption = new Option<bool>(
    aliases: ["--randomize-ports"],
    description: "Replace port numbers in configuration files with new random values.");

var regenerateGuidsOption = new Option<bool>(
    aliases: ["--regenerate-guids"],
    description: "Replace all GUIDs with newly generated ones.");

var cleanOption = new Option<bool>(
    aliases: ["--clean"],
    description: "Delete build-output directories (bin, obj) and front-end caches (node_modules, .angular, …) before renaming.");

// ─────────────────────────────────────────────────────────────────────────────
// 'rename' sub-command  (in-place rename)
// ─────────────────────────────────────────────────────────────────────────────
//   vsrename rename <project-dir> <old-name> <new-name> [options]

var renameSourceArg = new Argument<DirectoryInfo>(
    name: "source",
    description: "Path to the project directory to rename.");

var renameOldArg = new Argument<string>(
    name: "old-name",
    description: "Existing name to search for (PascalCase).");

var renameNewArg = new Argument<string>(
    name: "new-name",
    description: "Replacement name (PascalCase).");

var renameCommand = new Command("rename", "Rename a project in-place.")
{
    renameSourceArg,
    renameOldArg,
    renameNewArg,
    dryRunOption,
    noRestoreOption,
    verboseOption,
    randomizePortsOption,
    regenerateGuidsOption,
    cleanOption,
};

renameCommand.SetHandler((InvocationContext ctx) =>
{
    var source         = ctx.ParseResult.GetValueForArgument(renameSourceArg);
    var oldName        = ctx.ParseResult.GetValueForArgument(renameOldArg);
    var newName        = ctx.ParseResult.GetValueForArgument(renameNewArg);
    var dryRun         = ctx.ParseResult.GetValueForOption(dryRunOption);
    var noRestore      = ctx.ParseResult.GetValueForOption(noRestoreOption);
    var verbose        = ctx.ParseResult.GetValueForOption(verboseOption);
    var randomizePorts = ctx.ParseResult.GetValueForOption(randomizePortsOption);
    var regenerateGuids= ctx.ParseResult.GetValueForOption(regenerateGuidsOption);
    var clean          = ctx.ParseResult.GetValueForOption(cleanOption);

    var opts = new RenameOptions
    {
        SourceDirectory = source!.FullName,
        OldName         = oldName!,
        NewName         = newName!,
        DryRun          = dryRun,
        NoRestore       = noRestore,
        Verbose         = verbose,
        RandomizePorts  = randomizePorts,
        RegenerateGuids = regenerateGuids,
        Clean           = clean,
    };
    new ProjectRenamer(opts).Run();
});

// ─────────────────────────────────────────────────────────────────────────────
// 'clone' sub-command  (copy then rename)
// ─────────────────────────────────────────────────────────────────────────────
//   vsrename clone <source-dir> <target-dir> <old-name> <new-name> [options]

var cloneSourceArg = new Argument<DirectoryInfo>(
    name: "source",
    description: "Path to the source project directory.");

var cloneTargetArg = new Argument<DirectoryInfo>(
    name: "target",
    description: "Destination directory for the cloned project (must not exist).");

var cloneOldArg = new Argument<string>(
    name: "old-name",
    description: "Existing name to search for (PascalCase).");

var cloneNewArg = new Argument<string>(
    name: "new-name",
    description: "Replacement name (PascalCase).");

var cloneCommand = new Command("clone", "Clone a project to a new directory and rename it.")
{
    cloneSourceArg,
    cloneTargetArg,
    cloneOldArg,
    cloneNewArg,
    dryRunOption,
    noRestoreOption,
    verboseOption,
    randomizePortsOption,
    regenerateGuidsOption,
    cleanOption,
};

cloneCommand.SetHandler((InvocationContext ctx) =>
{
    var source         = ctx.ParseResult.GetValueForArgument(cloneSourceArg);
    var target         = ctx.ParseResult.GetValueForArgument(cloneTargetArg);
    var oldName        = ctx.ParseResult.GetValueForArgument(cloneOldArg);
    var newName        = ctx.ParseResult.GetValueForArgument(cloneNewArg);
    var dryRun         = ctx.ParseResult.GetValueForOption(dryRunOption);
    var noRestore      = ctx.ParseResult.GetValueForOption(noRestoreOption);
    var verbose        = ctx.ParseResult.GetValueForOption(verboseOption);
    var randomizePorts = ctx.ParseResult.GetValueForOption(randomizePortsOption);
    var regenerateGuids= ctx.ParseResult.GetValueForOption(regenerateGuidsOption);
    var clean          = ctx.ParseResult.GetValueForOption(cleanOption);

    if (!dryRun && target!.Exists)
    {
        Console.Error.WriteLine($"Error: target directory already exists: {target.FullName}");
        ctx.ExitCode = 1;
        return;
    }

    var opts = new RenameOptions
    {
        SourceDirectory = source!.FullName,
        OutputDirectory = target!.FullName,
        OldName         = oldName!,
        NewName         = newName!,
        DryRun          = dryRun,
        NoRestore       = noRestore,
        Verbose         = verbose,
        RandomizePorts  = randomizePorts,
        RegenerateGuids = regenerateGuids,
        Clean           = clean,
    };
    new ProjectRenamer(opts).Run();
});

// ─────────────────────────────────────────────────────────────────────────────
// Wire up and invoke
// ─────────────────────────────────────────────────────────────────────────────

rootCommand.AddCommand(renameCommand);
rootCommand.AddCommand(cloneCommand);

return await rootCommand.InvokeAsync(args);
