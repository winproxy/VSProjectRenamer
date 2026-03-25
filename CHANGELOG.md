# Changelog

All notable changes to **Project Renamer Cloner** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/) and this project adheres to [Semantic Versioning](https://semver.org/).

---

## [3.0.0] — 2026-03-25

### Added

- **Multi-Target Framework Support** — the project now targets 8 frameworks:
  - **.NET 10, 9, 8, 7, 6** — full support as a global tool and standalone executable
  - **.NET Framework 4.8, 4.7.2, 4.6.2** — standalone executable only (global tool packaging is not supported for .NET Framework)
- **Conditional Tool Packaging** — `PackAsTool` is automatically disabled for .NET Framework targets; NuGet package includes tool shims for .NET 6+ only.
- **API Compatibility Polyfills** — all .NET 6+ APIs are polyfilled for .NET Framework:
  - `Environment.ProcessPath` → `Process.GetCurrentProcess().MainModule.FileName`
  - `Path.GetRelativePath` → `Uri.MakeRelativeUri`-based implementation
  - `string.Replace(StringComparison)` → `Regex.Replace` fallback
  - `KeyValuePair.Deconstruct` extension for tuple deconstruction
  - Range/index operators (`s[1..]`, `s[^n..]`) → `Substring` calls
- **Supported Frameworks Table** in README showing global tool vs. standalone EXE availability per TFM.

### Changed

- **Version** bumped from 2.1.0 to 3.0.0 (major platform expansion).
- **`<LangVersion>`** set to `latest` to enable modern C# syntax across all targets.
- **Package URLs** migrated from Azure DevOps to GitHub (`https://github.com/winproxy/VSProjectRenamer`).
- **README / USAGE / CHANGELOG links** converted to absolute GitHub URLs so they work correctly on nuget.org.
- **Requirements section** in README updated to document per-framework requirements and a supported frameworks table.
- **USAGE.md** updated with multi-framework build instructions and corrected clone URL.
- **Package description** updated to mention multi-framework support.

### Dependencies

- Added `Microsoft.NETFramework.ReferenceAssemblies` 1.0.3 (for .NET Framework targets, `PrivateAssets=All`).
- Added `System.ValueTuple` 4.5.0 (for .NET Framework 4.6.2 only).

---

## [2.1.0] — 2026-03-11

### Added

- **Backup & Restore** — optional pre-rename backup (`__renamer_backup__`) with one-key restore on next run. Excludes `bin`, `obj`, `node_modules`, `.git`, and cache folders to keep the backup lean.
- **8-Step Pipeline** — the rename pipeline now has 8 steps (was 7):
  1. Clean build outputs, caches & lock files
  2. Content replacement (case-aware)
  3. File renaming
  4. Directory renaming (leaf-first)
  5. Port randomization (`launchSettings.json`)
  6. **Port propagation** to all configuration files (NEW)
  7. GUID & UserSecretsId regeneration
  8. Package restore
- **Port Propagation Step** — after randomizing ports in `launchSettings.json`, the tool now scans all text files and propagates port changes to:
  - URL patterns (`http://localhost:XXXX`)
  - Property patterns (`"port": XXXX`, `Port=XXXX`)
  - Docker compose port mappings
  - IIS Express `applicationhost.config` bindings
- **IIS Express & Legacy .NET Framework Support** — port detection now scans `web.config`, `*.csproj.user`, and `applicationhost.config` for port numbers before randomization.
- **Consistent Port Mapping** — all occurrences of the same original port are mapped to the same new port across all files (deterministic `portMap` dictionary).
- **Port Deduplication Summary** — port remapping table is displayed in the console output during step 6.
- **USAGE.md** — comprehensive usage documentation for NuGet global tool, single-file EXE, and build-from-source workflows.
- **CHANGELOG.md** — this file.

### Changed

- Summary output now includes **Ports remapped** count.
- Step counter updated from `[x/7]` to `[x/8]` throughout the pipeline.
- README updated to reflect the 8-step pipeline, backup/restore, and new port propagation features.

---

## [2.0.0] — 2026-02-01

### Added

- Initial public release as a .NET global tool.
- **12 case-variant generation** from a single PascalCase input (PascalCase, camelCase, lowercase, UPPERCASE, kebab-case, UPPER-KEBAB, snake_case, SCREAMING_SNAKE, dot.case, Pascal.Dot, Title Case, lower space).
- **7-step pipeline**: Clean → Content Replace → File Rename → Dir Rename → Port Randomize → GUID Regenerate → Package Restore.
- 60+ supported file extensions across .NET, web, mobile, and build ecosystems.
- `.sln` / `.slnx` GUID regeneration (preserves type GUIDs).
- `UserSecretsId` rotation in `.csproj` files.
- `launchSettings.json` port randomization.
- Automatic `dotnet restore` and frontend package manager detection (`npm` / `yarn` / `pnpm` / `bun`).
- Self-exclusion of the running executable from processing.
- Longest-first replacement ordering to prevent partial-match collisions.
- Interactive confirmation prompt with full replacement map display.
- Live progress bars for content replacement, file renaming, and directory renaming.
- Key1 / Key2 arbitrary exact-match replacements.
- Single-file EXE publish profile (`win-x64`).
