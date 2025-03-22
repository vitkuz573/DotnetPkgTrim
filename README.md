# pkg-trim

`pkg-trim` is a simple .NET CLI tool that analyzes your solution and identifies unused `<PackageVersion>` entries in your `Directory.Packages.props` file. Optionally, it can clean them up for you automatically.

## Features

- Scans all `.csproj` and `Directory.Build.props` files for used `PackageReference`s
- Compares them against the declared `PackageVersion`s in `Directory.Packages.props`
- Identifies unused package versions
- Supports an optional `--fix` flag to automatically remove unused packages

## Installation

```bash
dotnet tool install --global pkg-trim
```

## Usage

```bash
pkg-trim [--sln-dir <path>] [--fix]
```

### Options

- `--sln-dir` or `--solution-directory`  
  Path to the root directory of your solution (where `Directory.Packages.props` is located).  
  If omitted, the tool will use the directory of the executable as default.

- `--fix`  
  If specified, removes the unused packages from `Directory.Packages.props`.  
  Without this flag, the tool only lists the unused entries.

### Example

```bash
pkg-trim --sln-dir /path/to/solution
```

```bash
pkg-trim --fix
```

## How It Works

1. Scans all `.csproj` and `Directory.Build.props` files recursively in the provided solution directory.
2. Collects all used `PackageReference` names.
3. Loads the `Directory.Packages.props` file and checks for `<PackageVersion>` entries that are no longer used.
4. Lists them in the output, and removes them if `--fix` is passed.

## Why

Over time, projects accumulate package versions that are no longer used. Keeping your `Directory.Packages.props` file clean helps:

- Reduce confusion and clutter
- Improve maintainability of your solution

## License

MIT
