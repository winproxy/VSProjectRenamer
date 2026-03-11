# VSProjectRenamer

A .NET global tool that **renames and clones entire projects** in a single step. It performs case-aware find-and-replace across file contents, file names, and directory names — automatically generating **12 naming convention variants** from a single PascalCase input. Designed for **ABP.io**, **ASP.NET Core**, **Angular**, **React Native**, **Blazor**, and any multi-project .NET solution.

## Features

- 🔄 **Clone + rename in one step** — copy a project to a new directory and rename everything in a single command
- 🔡 **12 naming-convention variants** — automatically derives and replaces all case styles from one PascalCase input
- 📂 **60+ file types** — processes `.cs`, `.csproj`, `.sln`, `.json`, `.yaml`, `.ts`, `.razor`, `Dockerfile`, and many more
- 🔑 **GUID regeneration** — replaces all GUIDs with freshly generated ones (`--regenerate-guids`)
- 🔌 **Port randomization** — assigns new random port numbers in config files (`--randomize-ports`)
- 📦 **Automatic `dotnet restore`** — restores NuGet packages after renaming (opt-out with `--no-restore`)
- 🧪 **Dry-run mode** — preview all planned changes before applying (`--dry-run`)

## Installation

```bash
dotnet tool install --global VSProjectRenamer
```

## Usage

### Rename a project in-place

```bash
vsrename rename <source-dir> <old-name> <new-name> [options]
```

**Example:**
```bash
vsrename rename ./BookStore BookStore LibrarySystem
```

### Clone a project to a new directory and rename it

```bash
vsrename clone <source-dir> <target-dir> <old-name> <new-name> [options]
```

**Example:**
```bash
vsrename clone ./BookStore ./LibrarySystem BookStore LibrarySystem
```

### Options

| Option | Description |
|--------|-------------|
| `-d`, `--dry-run` | Print planned changes without applying them |
| `--no-restore` | Skip `dotnet restore` after the operation |
| `-v`, `--verbose` | Enable verbose output (shows unchanged files) |
| `--randomize-ports` | Replace port numbers in config files with new random values |
| `--regenerate-guids` | Replace all GUIDs with newly generated ones |

## The 12 Naming Convention Variants

Given a single PascalCase input (e.g. `BookStore`), the tool derives and replaces all 12 variants simultaneously:

| # | Convention | Example |
|---|-----------|---------|
| 1 | PascalCase | `BookStore` |
| 2 | camelCase | `bookStore` |
| 3 | snake_case | `book_store` |
| 4 | kebab-case | `book-store` |
| 5 | UPPER_SNAKE_CASE | `BOOK_STORE` |
| 6 | dot.case | `book.store` |
| 7 | Title Case | `Book Store` |
| 8 | UPPERFLATCASE | `BOOKSTORE` |
| 9 | lowerflatcase | `bookstore` |
| 10 | Acronym | `BS` |
| 11 | lower space | `book store` |
| 12 | UPPER SPACE | `BOOK STORE` |

## Supported File Types

The tool processes the contents of 60+ file types including:

`.cs` `.vb` `.fs` `.csproj` `.vbproj` `.fsproj` `.sln` `.props` `.targets` `.config` `.json` `.yaml` `.yml` `.xml` `.toml` `.ini` `.env` `.html` `.cshtml` `.razor` `.aspx` `.ts` `.tsx` `.js` `.jsx` `.css` `.scss` `.sass` `.less` `.md` `.txt` `.sh` `.ps1` `.cmd` `.bat` `.dockerfile` `.proto` `.graphql` `.http` `.editorconfig` `.gitignore` `.tf` `.feature` and more.

All files (regardless of type) are eligible for **name** renaming.

## Skipped Directories

The following directories are excluded from clone operations to avoid copying unnecessary artifacts:

`.git` `.vs` `.idea` `node_modules` `bin` `obj` `.angular` `.next` `.nuxt` `dist` `build` `packages`

## Building from Source

```bash
git clone https://github.com/winproxy/VSProjectRenamer
cd VSProjectRenamer
dotnet build
dotnet test
```

## License

MIT — see [LICENSE](LICENSE)

