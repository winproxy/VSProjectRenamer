# Project Renamer Cloner

A .NET global tool that **renames and clones entire projects** in a single step. It performs case-aware find-and-replace across file contents, file names, and directory names — automatically generating **12 naming convention variants** from a single PascalCase input. Designed for **ABP.io**, **ASP.NET Core**, **Angular**, **React Native**, **Blazor**, and any multi-project .NET solution.

> 📖 **[Detailed Usage Guide](USAGE.md)** · 📋 **[Changelog](CHANGELOG.md)**

## Features

### Automatic Case Variant Generation

Enter a PascalCase name and the tool builds every common naming convention automatically:

| Convention | Old | New |
|---|---|---|
| PascalCase | `BookStore` | `MusicShop` |
| camelCase | `bookStore` | `musicShop` |
| lowercase | `bookstore` | `musicshop` |
| UPPERCASE | `BOOKSTORE` | `MUSICSHOP` |
| kebab-case | `book-store` | `music-shop` |
| UPPER-KEBAB | `BOOK-STORE` | `MUSIC-SHOP` |
| snake_case | `book_store` | `music_shop` |
| SCREAMING_SNAKE | `BOOK_STORE` | `MUSIC_SHOP` |
| dot.case | `book.store` | `music.shop` |
| Pascal.Dot | `Book.Store` | `Music.Shop` |
| Title Case | `Book Store` | `Music Shop` |
| lower space | `book store` | `music shop` |

Every occurrence inside **namespaces**, **class names**, **using directives**, **string literals**, **comments**, **configuration files**, **file names**, and **directory names** is replaced.

### 8-Step Pipeline

| Step | What it does |
|---|---|
| **1. Clean** | Deletes `bin`, `obj`, `node_modules`, `.angular`, `.next`, `.nuxt`, `.turbo`, `.cache`, `.parcel-cache`, `coverage`, `Pods`, and frontend lock files |
| **2. Content Replace** | Replaces all case variants inside 60+ file types with a live progress bar |
| **3. File Rename** | Renames files containing the old name with a live progress bar |
| **4. Dir Rename** | Renames directories leaf-first to avoid path conflicts, with a live progress bar |
| **5. Port Randomize** | Randomizes `applicationUrl` ports and `sslPort` in `launchSettings.json`. Also detects ports in `web.config`, `*.csproj.user`, and `applicationhost.config` (IIS Express) |
| **6. Port Propagate** | Propagates port changes to **all** configuration files — URL patterns, property patterns, Docker compose mappings, and IIS Express bindings |
| **7. GUID Regenerate** | Regenerates `.sln` instance GUIDs (preserves type GUIDs), syncs `<ProjectGuid>` in `.csproj`, handles `.slnx`, and regenerates `<UserSecretsId>` |
| **8. Package Restore** | Runs `dotnet restore` and auto-detects `npm` / `yarn` / `pnpm` / `bun install` |

### Supported File Types

<details>
<summary>60+ extensions across all major ecosystems (click to expand)</summary>

- **.NET / ABP** — `.cs`, `.csproj`, `.props`, `.targets`, `.sln`, `.slnx`, `.razor`, `.cshtml`, `.vb`, `.fs`, `.fsproj`, `.vbproj`, `.resx`, `.xaml`, `.axaml`
- **Config** — `.json`, `.jsonc`, `.config`, `.xml`, `.yml`, `.yaml`, `.toml`, `.ini`, `.runsettings`, `.ruleset`, `.DotSettings`
- **Web** — `.html`, `.htm`, `.css`, `.scss`, `.sass`, `.less`
- **JS / TS** — `.js`, `.jsx`, `.ts`, `.tsx`, `.mjs`, `.cjs`
- **Frameworks** — `.vue`, `.svelte`, `.component`, `.service`, `.module`, `.directive`, `.pipe`
- **Android** — `.java`, `.kt`, `.kts`, `.gradle`
- **iOS** — `.swift`, `.m`, `.h`, `.plist`, `.pbxproj`, `.xcscheme`, `.xcworkspacedata`, `.storyboard`, `.xib`, `.strings`, `.entitlements`, `.podspec`
- **Build / CI** — `.dockerfile`, `.env`, `.cmd`, `.bat`, `.ps1`, `.sh`
- **Data** — `.sql`, `.proto`, `.graphql`, `.gql`
- **Docs** — `.md`, `.txt`, `.editorconfig`, `.gitignore`, `.dockerignore`, `.gitattributes`
- **HTTP** — `.http`, `.rest`
- **Extensionless** — `Dockerfile`, `Podfile`, `Gemfile`, `Makefile`, `Procfile`, `.browserslistrc`, `.babelrc`, `.eslintrc`, `.prettierrc`

</details>

### Backup & Restore

- **Pre-rename backup** — before making any changes, the tool offers to create a backup in `__renamer_backup__/`
- **One-key restore** — on the next run, if a backup is detected, the tool offers to restore all files to their pre-rename state
- **Smart exclusions** — backup skips `bin`, `obj`, `node_modules`, `.git`, and other cache folders to keep backup size small

### Safety

- **Confirmation prompt** — displays the full replacement map and asks `Y/N` before making any changes
- **Self-exclusion** — the running executable is excluded from processing
- **Type GUIDs preserved** — only project instance GUIDs are regenerated; `.sln` type GUIDs (`C#`, `F#`, `VB`, `Solution Folder`, etc.) are never touched
- **Lock files deleted, not modified** — `package-lock.json`, `yarn.lock`, `pnpm-lock.yaml`, `bun.lockb` are removed before content replacement to avoid hash corruption, then regenerated during package restore
- **Longest-first replacement** — replacements are sorted by key length descending to prevent partial-match collisions
- **Consistent port mapping** — the same original port is always mapped to the same new port across all files
- **Port deduplication** — randomized ports are tracked in a `HashSet` so no two profiles share the same port
- **Error counting** — errors are counted and reported in the summary, not silently swallowed

---

## Installation

### Option 1 — .NET Global Tool (recommended)

```bash
dotnet tool install -g ProjectRenamerCloner
```

Then run from any directory:

```bash
project-renamer
```

### Option 2 — Single-File EXE

Download the latest release and place the executable in the root of the project you want to rename.

### Option 3 — Build from Source

```bash
git clone https://github.com/winproxy/VSProjectRenamer.git
cd ProjectRenamerCloner

# global tool
dotnet pack
dotnet tool install -g --add-source ./nupkg ProjectRenamerCloner

# single-file exe
dotnet publish -p:PublishProfile=singlefile
```

---

## Usage

**1.** Open a terminal in the root of the project you want to rename.

**2.** Run the tool:

```bash
project-renamer
```

**3.** Follow the interactive prompts:

```
╔═══════════════════════════════════════╗
║       PROJECT RENAMER CLONER          ║
╚═══════════════════════════════════════╝

  Old Project Name (required): BookStore
  New Project Name (required): MusicShop
  Key1 OLD (optional): Acme
  Key1 NEW (optional): Contoso
  Key2 OLD (optional):
  Key2 NEW (optional):

  Replacements:
    BookStore → MusicShop
    bookStore → musicShop
    bookstore → musicshop
    BOOKSTORE → MUSICSHOP
    book-store → music-shop
    BOOK-STORE → MUSIC-SHOP
    book_store → music_shop
    BOOK_STORE → MUSIC_SHOP
    book.store → music.shop
    Book.Store → Music.Shop
    Book Store → Music Shop
    book store → music shop
    Acme → Contoso

  Proceed with rename? (Y/N): Y
```

**4.** Watch the progress:

```
  [1/8] Cleaning build outputs, caches & lock files...
  [2/8] Replacing file contents... (1842 files scanned)
    Content   [████████████████████░░░░░░░░░░]  67% (412/615) src\MusicShop.Application\Services\OrderService.cs
  [3/8] Renaming files...
    Files     [██████████████████████████████] 100% (1842/1842)
  [4/8] Renaming directories (leaf-first)...
    Dirs      [██████████████████████████████] 100% (58/58)
  [5/8] Randomizing application ports...
  [6/8] Propagating port changes to configuration files...
    :5000 → :5234
    :5001 → :7189
    :44300 → :44352
    Ports     [██████████████████████████████] 100% (615/615)
  [7/8] Regenerating GUIDs & UserSecretsId...
  [8/8] Restoring packages...
    dotnet restore
    npm install → angular

  ╔═══════════════════════════════════════╗
  ║       PROJECT CLONE COMPLETED         ║
  ╚═══════════════════════════════════════╝

    Files modified:  312
    Files renamed:   47
    Dirs renamed:    12
    Ports remapped:  3
    Elapsed:         00:08.42
```

---

## Typical Workflows

### ABP.io Projects

ABP solutions follow a `CompanyName.ProjectName` convention. Enter the **project name** part and use `Key1` for the company prefix:

```
Old Project Name: BookStore
New Project Name: MusicShop
Key1 OLD: Acme
Key1 NEW: Contoso
```

Result: `Acme.BookStore.Domain` → `Contoso.MusicShop.Domain` everywhere.

### Cloning a Template

1. Copy your template solution into a new folder.
2. Open a terminal in the new folder.
3. Run `project-renamer`, enter the template name as **Old** and your real project name as **New**.
4. The tool renames everything, randomizes ports (so both projects can run side-by-side), regenerates GUIDs, and restores packages.

### Extra Replacements

Use `Key1` and `Key2` for arbitrary exact-match replacements beyond the auto-generated case variants:

```
Key1 OLD: my-old-database
Key1 NEW: my-new-database
Key2 OLD: sk_old_stripe_key
Key2 NEW: sk_new_stripe_key
```

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- **Optional:** Node.js / Bun (for frontend package restore)
- **Platform:** Windows (path separator logic uses `\`)

---

## Documentation

| Document | Description |
|----------|-------------|
| [USAGE.md](USAGE.md) | Detailed installation & usage guide (NuGet tool, EXE, build from source, workflows, tips) |
| [CHANGELOG.md](CHANGELOG.md) | Version history and release notes |

---

## License

This project is licensed under the [MIT License](LICENSE).

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-improvement`)
3. Commit your changes (`git commit -m 'Add my improvement'`)
4. Push to the branch (`git push origin feature/my-improvement`)
5. Open a Pull Request
