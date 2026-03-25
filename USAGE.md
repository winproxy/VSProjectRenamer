# Usage Guide

This document explains how to install and use **Project Renamer Cloner** as a **NuGet global tool**, a **single-file EXE**, or by **building from source**.

---

## Table of Contents

- [1. NuGet Global Tool (Recommended)](#1-nuget-global-tool-recommended)
  - [Install](#install)
  - [Update](#update)
  - [Uninstall](#uninstall)
  - [Run](#run)
- [2. Single-File EXE](#2-single-file-exe)
  - [Build](#build)
  - [Run](#run-1)
- [3. Build from Source](#3-build-from-source)
- [4. Interactive Prompts](#4-interactive-prompts)
- [5. The 8-Step Pipeline](#5-the-8-step-pipeline)
- [6. Backup & Restore](#6-backup--restore)
- [7. Typical Workflows](#7-typical-workflows)
- [8. Tips & Best Practices](#8-tips--best-practices)

---

## 1. NuGet Global Tool (Recommended)

### Install

```bash
dotnet tool install -g ProjectRenamerCloner
```

> Requires [.NET 6 SDK](https://dotnet.microsoft.com/download) or later. The tool supports .NET 6, 7, 8, 9, and 10.

### Update

```bash
dotnet tool update -g ProjectRenamerCloner
```

### Uninstall

```bash
dotnet tool uninstall -g ProjectRenamerCloner
```

### Run

Open a terminal in the root of the project you want to rename and run:

```bash
project-renamer
```

The tool uses the **current working directory** as the project root.

---

## 2. Single-File EXE

### Build

```bash
dotnet publish -p:PublishProfile=singlefile
```

This produces a self-contained `win-x64` executable in the `bin/Release/net10.0/win-x64/publish/` directory.

> **Tip:** To build for a specific framework, use:
> ```bash
> dotnet publish -f net8.0 -r win-x64 --self-contained -p:PublishSingleFile=true
> ```
> For .NET Framework targets (`net48`, `net472`, `net462`), build with:
> ```bash
> dotnet build -f net48 -c Release
> ```
> The output EXE will be in `bin/Release/net48/`.

### Run

1. Copy the published `.exe` into the root of the project you want to rename.
2. Double-click or run from terminal:

```bash
.\ProjectRenamerCloner.exe
```

> **Note:** The tool automatically excludes its own executable from processing, so it's safe to place it inside the target project.

---

## 3. Build from Source

```bash
# Clone the repository
git clone https://github.com/winproxy/VSProjectRenamer.git
cd VSProjectRenamer

# Install as a global tool from local source
dotnet pack
dotnet tool install -g --add-source ./nupkg ProjectRenamerCloner

# Or build a single-file EXE
dotnet publish -p:PublishProfile=singlefile
```

To update from local source after making changes:

```bash
dotnet pack
dotnet tool update -g --add-source ./nupkg ProjectRenamerCloner
```

---

## 4. Interactive Prompts

When you run the tool, you will be guided through an interactive prompt:

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
```

| Prompt | Description |
|--------|-------------|
| **Old Project Name** | The current PascalCase name of the project. **Required.** |
| **New Project Name** | The new PascalCase name. **Required.** |
| **Key1 OLD / NEW** | An optional exact-match string pair (e.g., company name, database name). |
| **Key2 OLD / NEW** | A second optional exact-match string pair. |

After entering the names, the tool displays the full replacement map (12 case variants + any Key1/Key2 pairs) and asks for confirmation:

```
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
  Create backup before proceeding? (Y/N): Y
```

Type `exit` or `quit` at the name prompts to cancel.

---

## 5. The 8-Step Pipeline

| Step | Name | Description |
|------|------|-------------|
| **1** | **Clean** | Deletes `bin`, `obj`, `node_modules`, `.angular`, `.next`, `.nuxt`, `.turbo`, `.cache`, `.parcel-cache`, `coverage`, `Pods`, and frontend lock files (`package-lock.json`, `yarn.lock`, `pnpm-lock.yaml`, `bun.lockb`). |
| **2** | **Content Replace** | Scans 60+ file types and performs case-aware find-and-replace with a live progress bar. |
| **3** | **File Rename** | Renames files containing the old name (sorted longest-first to avoid conflicts). |
| **4** | **Dir Rename** | Renames directories leaf-first to prevent path conflicts. |
| **5** | **Port Randomize** | Randomizes `applicationUrl` and `sslPort` in `launchSettings.json`. Also detects ports in `web.config`, `*.csproj.user`, and `applicationhost.config` (IIS Express). |
| **6** | **Port Propagate** | Propagates port changes to **all** configuration files — URL patterns, property patterns, Docker compose mappings, and IIS bindings. |
| **7** | **GUID Regenerate** | Regenerates `.sln` instance GUIDs (preserves type GUIDs), syncs `<ProjectGuid>` in `.csproj`, handles `.slnx`, and regenerates `<UserSecretsId>`. |
| **8** | **Package Restore** | Runs `dotnet restore` and auto-detects `npm` / `yarn` / `pnpm` / `bun install`. |

---

## 6. Backup & Restore

Before making any changes, the tool offers to create a backup:

```
  Create backup before proceeding? (Y/N): Y
  Creating backup... ✓
```

The backup is stored in `__renamer_backup__/` inside the project root. On the next run, if a backup is detected:

```
  ⚠ Previous backup found in __renamer_backup__
  Restore from backup? (Y/N):
```

Choosing **Y** restores all files and directories to their pre-rename state and removes the backup folder.

> **What's excluded from backup:** `bin`, `obj`, `node_modules`, `.angular`, `.next`, `.nuxt`, `.turbo`, `.cache`, `.parcel-cache`, `coverage`, `Pods`, `.git`, and `__renamer_backup__` itself.

---

## 7. Typical Workflows

### ABP.io / Multi-Layer Projects

ABP solutions use a `CompanyName.ProjectName` convention. Enter the **project name** part and use **Key1** for the company prefix:

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
3. Run `project-renamer`.
4. Enter the template name as **Old** and your real project name as **New**.
5. The tool renames everything, randomizes ports (so both projects can run side-by-side), regenerates GUIDs, and restores packages.

### Extra Replacements

Use **Key1** and **Key2** for arbitrary exact-match replacements beyond the auto-generated case variants:

```
Key1 OLD: my-old-database
Key1 NEW: my-new-database
Key2 OLD: sk_old_stripe_key
Key2 NEW: sk_new_stripe_key
```

---

## 8. Tips & Best Practices

| Tip | Details |
|-----|---------|
| **Always use PascalCase** | Enter names in PascalCase (e.g., `BookStore`). The tool generates all other variants automatically. |
| **Create a backup** | Always say **Y** to the backup prompt, especially on the first run. |
| **Run from the project root** | The tool operates on the current working directory. Make sure you're in the right folder. |
| **Close your IDE** | Close Visual Studio / Rider before running to avoid file lock conflicts. |
| **Check the summary** | Review the final summary for error count. If errors > 0, check for locked files. |
| **Port conflicts** | If randomized ports conflict with existing services, re-run or manually edit `launchSettings.json`. |
| **Git clean state** | Run the tool on a clean Git state so you can easily `git diff` and review changes. |
| **Self-contained EXE** | The single-file EXE is self-contained — no .NET SDK required on the target machine. |
