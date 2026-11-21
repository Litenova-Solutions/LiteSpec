# Install the CLI

Install the `litespec` command line for a consumer repository or a machine: pick the method that fits, check the download, and run it in continuous integration.

Each LiteSpec release ships the command line in two forms: the `LiteSpec.Cli` .NET tool on nuget.org, and one standalone archive per platform on the [GitHub release](https://github.com/Litenova-Solutions/LiteSpec/releases). Both hold the same native executable, compiled ahead of time for the platform, and both carry the LiteSpec release beside it, so the executable needs no .NET runtime and no network. This guide adds no rule of its own.

## Prerequisites

Start by picking the install method that fits the machine. The table compares the five methods: what each one needs installed first, where it records the release, and how you spell the command afterward.

| Method | Needs | Pins the release in | Runs as |
|:---|:---|:---|:---|
| [Local .NET tool](#1-install-the-local-net-tool) | .NET 10 SDK | `.config/dotnet-tools.json` | `dotnet litespec <command>` |
| [Global .NET tool](#2-install-the-global-net-tool) | .NET 10 SDK | the machine | `litespec <command>` |
| [One-off run](#3-run-a-release-without-installing-it) | .NET 10 SDK | the command line | `dnx LiteSpec.Cli@<version> <command>` |
| [Install script](#4-install-with-the-install-script) | `curl` or `wget`, or PowerShell | `reviewedLiteSpecVersion` | `litespec <command>` |
| [Manual download](#5-download-and-install-an-archive) | a browser or `curl` | the install folder | `litespec <command>` |

The `init` command writes the project for the method you chose: `litespec init --install tool` writes a project that runs the local .NET tool, while `litespec init --install standalone` writes one that runs the standalone binary. The [init command reference](../tools/init.md) lists what each option writes.

Two more tools apply to some projects. A project with a React or Svelte frontend needs Node at the version the manifest pins (`stack.node`), because the CLI reads those frontends through a bundled Node reader. Running `model check --since` needs `git`, because the command reads the change set from git history.

## Procedure

Sections 1 to 5 are alternative install methods: follow exactly one. The last three sections cover continuous integration, the shipped platforms, and release candidates.

### 1. Install the Local .NET Tool

Choose the local tool when the repository should pin the release, so every machine and every CI run restores the same version. From the consumer root, create the tool manifest when the repository has none, then install the release you want:

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --version 7.0.0
dotnet litespec --version
```

Commit `.config/dotnet-tools.json`. Teammates and CI jobs then run `dotnet tool restore` once and run `dotnet litespec <command>` from the consumer root. The `LiteSpec.Cli` package names one native package per platform, and the SDK installs the one that matches the machine. On a platform with no native build, you get a framework-dependent package that runs on the .NET 10 runtime.

### 2. Install the Global .NET Tool

Choose the global tool to put `litespec` on the PATH for every folder on the machine, at one release:

```bash
dotnet tool install --global LiteSpec.Cli --version 7.0.0
litespec --version
```

Move it to another release with `dotnet tool update --global LiteSpec.Cli --version <version>`, and remove it with `dotnet tool uninstall --global LiteSpec.Cli`.

### 3. Run a Release Without Installing It

Use `dnx` for a quick look at a repository, because it runs a release without installing it:

```bash
dnx LiteSpec.Cli@7.0.0 model check
```

It asks before the first download. `dnx --yes` answers for it.

### 4. Install with the Install Script

Use the install scripts to download the archive for the machine from the GitHub release, check it against the release `SHA256SUMS`, and install it. Run in a consumer root, they install the release that `.litespec/project.yaml` reviews. Run elsewhere, they install the latest release.

On Linux and macOS, `install.sh` keeps each release in `~/.litespec/<version>` and links the active one at `~/.litespec/bin/litespec`:

```bash
curl -fsSL https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.sh | sh
export PATH="$HOME/.litespec/bin:$PATH"
litespec --version
```

Add the `export` line to `~/.profile` or `~/.zshrc`. Install a named release with `sh -s -- --version 7.0.0`, and install to another folder with `--install-dir <dir>`. `LITESPEC_VERSION` and `LITESPEC_INSTALL_DIR` set the same values from the environment.

On Windows, `install.ps1` keeps each release in `%LOCALAPPDATA%\Programs\LiteSpec\<version>` and puts the active one on the user PATH:

```powershell
irm https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.ps1 | iex
```

Set `$env:LITESPEC_VERSION` before the command to install a named release, then open a new terminal to read the updated PATH. Running either script again with another version installs it beside the others and makes it the active one. To remove a release, delete its folder. To remove LiteSpec, delete the install folder and its PATH entry.

### 5. Download and Install an Archive

Each [GitHub release](https://github.com/Litenova-Solutions/LiteSpec/releases) ships `litespec-<version>-<rid>.tar.gz` for Linux and macOS, `litespec-<version>-<rid>.zip` for Windows, and `SHA256SUMS`. Inside each archive sits one folder with the executable, the release it carries, and the license. Keep that folder whole and put it on the PATH, because the executable reads the release from the folder beside it.

Check what you downloaded against its checksum and its build provenance attestation, which ties the archive to the release workflow that built it:

```bash
sha256sum --check --ignore-missing SHA256SUMS
gh attestation verify litespec-7.0.0-linux-x64.tar.gz --repo Litenova-Solutions/LiteSpec
```

A browser marks its downloads as coming from the internet. On macOS the archive is not notarized, so clear that mark before the first run with `xattr -dr com.apple.quarantine <folder>`. On Windows SmartScreen asks before the first run of an unsigned executable, so run `Unblock-File` on the archive before you extract it to clear the mark. Downloads through the install scripts and the .NET tool carry no such mark, so neither prompt appears with them.

### 6. Install the CLI in Continuous Integration

Match the CI install to the way the repository pins the release. With the local .NET tool:

```yaml
- uses: actions/setup-dotnet@v6
  with:
    dotnet-version: "10.0.x"
- run: dotnet tool restore
- run: dotnet litespec model check --record-proof
```

With the standalone binary, the install script reads `reviewedLiteSpecVersion`, and in GitHub Actions it adds the binary to the PATH of later steps:

```yaml
- run: curl -fsSL https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.sh | sh
- run: litespec model check --record-proof
```

### 7. Check the Platform

The release ships a native executable for these runtime identifiers:

| Runtime identifier | Runs on |
|:---|:---|
| `win-x64`, `win-arm64` | The Windows versions .NET 10 supports |
| `osx-x64`, `osx-arm64` | The macOS versions .NET 10 supports |
| `linux-x64`, `linux-arm64` | Linux with glibc 2.28 or later, such as RHEL 8, Debian 10, and Ubuntu 20.04 |
| `linux-musl-x64`, `linux-musl-arm64` | Alpine 3.21 or later |

The [.NET 10 supported OS list](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md) names which Windows and macOS versions those cover. A Linux executable needs only the C library: no ICU, no OpenSSL, and no .NET runtime. Every other platform runs the framework-dependent .NET tool on the .NET 10 runtime.

### 8. Install a Release Candidate

Install a release candidate, such as `7.0.0-rc.1`, to test a release before it ships. A candidate publishes the same packages and archives as the release, at the suffixed version, from the release branch, and its GitHub release is marked as a prerelease. The install script and `dotnet tool update` keep the latest release when you name no version, so name the candidate to install it.

The local .NET tool takes the candidate version and pins it in `.config/dotnet-tools.json`, so `dotnet tool restore` restores the same candidate everywhere:

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --version 7.0.0-rc.1
dotnet litespec --version
```

`dotnet litespec --version` prints `7.0.0-rc.1`. Name the candidate in each project that references the analyzers as well, because the release pin does not restore the candidate analyzers:

```xml
<PackageReference Include="LiteSpec.Analyzers" Version="7.0.0-rc.1" PrivateAssets="all" />
```

The standalone binary takes the same version: pass `sh -s -- --version 7.0.0-rc.1` with the install script, or set `$env:LITESPEC_VERSION = '7.0.0-rc.1'` before `install.ps1` on Windows. The archives of a candidate carry the suffix in their names, such as `litespec-7.0.0-rc.1-linux-x64.tar.gz`.

A candidate's `AGENTS.md` links name the release version, and the site serves that release once it ships, so those links resolve only after the release. The consumer records the candidate in `reviewedLiteSpecVersion`, which `litespec init` writes when you run it with the candidate installed.

## Check

Confirm the install with these checks:

- `litespec --version`, or `dotnet litespec --version` for the local .NET tool, prints the release you installed.
- `litespec model check --help` prints the usage text and exits zero.
- From a consumer root that holds `.litespec/project.yaml`, `litespec model check` reads the project with the installed release.
