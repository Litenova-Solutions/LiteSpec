# Overview

## Intent

The Tools section documents the LiteSpec command line: one command for each consumer check, generator, and lookup, from project validation to generated files. A developer or coding assistant opens this index to find the command that produces the check or file the task needs. On a command page, Name and Synopsis give the quick form, and Description and Examples give the detail. The commands that maintain the standard itself live in the [Maintain overview](../maintain/README.md).

## Install the CLI

Each LiteSpec release ships the `litespec` command line in two forms: the `LiteSpec.Cli` .NET tool on nuget.org, and a standalone archive per platform on the [GitHub release](https://github.com/Litenova-Solutions/LiteSpec/releases). Both hold the same native executable, compiled ahead of time for the platform, and both carry the LiteSpec release beside it, so the executable needs no .NET runtime and no network. A team with the .NET SDK installs the .NET tool; a team without it installs the standalone binary.

| Method | Needs | Pins the release in | Runs as |
|:---|:---|:---|:---|
| [Local .NET tool](#local-net-tool) | .NET 10 SDK | `.config/dotnet-tools.json` | `dotnet litespec <command>` |
| [Global .NET tool](#global-net-tool) | .NET 10 SDK | the machine | `litespec <command>` |
| [One-off run](#one-off-run) | .NET 10 SDK | the command line | `dnx LiteSpec.Cli@<version> <command>` |
| [Install script](#install-script) | `curl` or `wget`, or PowerShell | `reviewedLiteSpecVersion` | `litespec <command>` |
| [Manual download](#manual-download) | a browser or `curl` | the install folder | `litespec <command>` |

`litespec init --install tool` writes a project that runs the local .NET tool, and `litespec init --install standalone` one that runs the standalone binary; the [init page](init.md) states what each writes.

### Local .NET Tool

The local tool pins the release in the repository, so every machine and every CI run restores the same one. From the consumer root, create the tool manifest when the repository has none, then install the release:

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --version 4.1.0
dotnet litespec --version
```

Commit `.config/dotnet-tools.json`. A teammate or a CI job then runs `dotnet tool restore` once and `dotnet litespec <command>` from the consumer root. The `LiteSpec.Cli` package names one native package per platform, and the SDK installs the one for the machine. A platform with no native build gets a framework-dependent package that runs on the .NET 10 runtime.

### Global .NET Tool

A global tool puts `litespec` on the PATH for every folder on the machine, at one release:

```bash
dotnet tool install --global LiteSpec.Cli --version 4.1.0
litespec --version
```

`dotnet tool update --global LiteSpec.Cli --version <version>` moves it to another release, and `dotnet tool uninstall --global LiteSpec.Cli` removes it.

### One-Off Run

`dnx` runs a release without installing it, which suits a quick look at a repository:

```bash
dnx LiteSpec.Cli@4.1.0 model check
```

It asks before the first download; `dnx --yes` answers for it.

### Install Script

The install scripts download the archive for the machine from the GitHub release, check it against the release's `SHA256SUMS`, and install it. Run in a consumer root, they install the release `.litespec/project.json` reviews; elsewhere, the latest release.

On Linux and macOS, `install.sh` keeps each release in `~/.litespec/<version>` and links the active one at `~/.litespec/bin/litespec`:

```bash
curl -fsSL https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.sh | sh
export PATH="$HOME/.litespec/bin:$PATH"
litespec --version
```

Add the `export` line to `~/.profile` or `~/.zshrc`. `sh -s -- --version 4.1.0` installs a named release, and `--install-dir <dir>` another folder; `LITESPEC_VERSION` and `LITESPEC_INSTALL_DIR` set the same from the environment.

On Windows, `install.ps1` keeps each release in `%LOCALAPPDATA%\Programs\LiteSpec\<version>` and puts the active one on the user PATH:

```powershell
irm https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.ps1 | iex
```

Set `$env:LITESPEC_VERSION` before the command to install a named release, and open a new terminal to read the updated PATH. Running either script again with another version installs it beside the others and makes it the active one. To remove a release, delete its folder; to remove LiteSpec, delete the install folder and its PATH entry.

### Manual Download

Each [GitHub release](https://github.com/Litenova-Solutions/LiteSpec/releases) holds `litespec-<version>-<rid>.tar.gz` for Linux and macOS, `litespec-<version>-<rid>.zip` for Windows, and `SHA256SUMS`. An archive holds one folder with the executable, the release it carries, and the licence. Keep the folder whole, because the executable reads the release beside it, and put the folder on the PATH.

Check a download against its checksum and its build provenance attestation, which ties it to the release workflow that built it:

```bash
sha256sum --check --ignore-missing SHA256SUMS
gh attestation verify litespec-4.1.0-linux-x64.tar.gz --repo Litenova-Solutions/LiteSpec
```

A browser marks a download as coming from the internet. On macOS, the archive is not notarized, so clear the mark before the first run with `xattr -dr com.apple.quarantine <folder>`. On Windows, SmartScreen asks before the first run of an unsigned executable; `Unblock-File` on the archive before extracting it clears the mark. The install scripts and the .NET tool download without the mark, so neither prompt appears.

### Continuous Integration

A CI job installs the CLI the way the repository pins it. With the local .NET tool:

```yaml
- uses: actions/setup-dotnet@v6
  with:
    dotnet-version: "10.0.x"
- run: dotnet tool restore
- run: dotnet litespec model check
```

With the standalone binary, the install script reads `reviewedLiteSpecVersion` and, in GitHub Actions, adds the binary to the PATH of later steps:

```yaml
- run: curl -fsSL https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.sh | sh
- run: litespec model check
```

### Platform Requirements

The release builds a native executable for these runtime identifiers:

| Runtime identifier | Runs on |
|:---|:---|
| `win-x64`, `win-arm64` | The Windows versions .NET 10 supports |
| `osx-x64`, `osx-arm64` | The macOS versions .NET 10 supports |
| `linux-x64`, `linux-arm64` | Linux with glibc 2.28 or later, such as RHEL 8, Debian 10 and Ubuntu 20.04 |
| `linux-musl-x64`, `linux-musl-arm64` | Alpine 3.21 or later |

The [.NET 10 supported OS list](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md) names the Windows and macOS versions. A Linux executable needs only the C library: no ICU, no OpenSSL, and no .NET runtime. Any other platform runs the framework-dependent .NET tool on the .NET 10 runtime. A project with a React or Svelte frontend also needs Node at the version the manifest pins (`stack.node`), because the CLI reads those frontends with a bundled Node reader, and `model check --since` needs `git`.

## Run the CLI

Run every command from the consumer root, the folder that holds `.litespec/project.json`. The command pages write the consumer form as `litespec <command>`; with the local .NET tool, type `dotnet litespec <command>`. In the LiteSpec repository, run `dotnet run --project apps/cli/src/LiteSpec.Cli -- <command>`.

The CLI carries its release beside its own executable; set `LITESPEC_HOME` to point it at another copy. Set `LITESPEC_TIMING` to print the phase timings of `litespec model` on standard error.

Every command accepts `--help`, and `model check` accepts `--format json` for a caller that reads the result rather than the text.

## Consumer Repository

- [Initialize Consumer](init.md): writes `.litespec/project.json` for a new consumer from a preset.
- [Scaffold Record](new.md): creates a record file and reserves its identifier, allocates the next decision number, or appends a result, scenario, or list entry to its owning file.
- [Rename Record](rename.md): renames a record identifier and rewrites every link.
- [Model Builder](model.md): checks the project model with the consumer pass, formats record files, renders generated outputs, and answers record queries.
- [Book](book.md): generates the project book from the consumer root: one offline shell with the outline and security views.
- [Rules Query](rules.md): lists the provisions that govern a path or record identifier, grouped by strength.
