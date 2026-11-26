# Publish a Local Toolchain

## Name

`litespec-maintain publish-local` packs the checkout's CLI and analyzers into a local feed and moves a consumer onto that build.

## Synopsis

```bash
litespec-maintain publish-local --consumer <path> [--feed <dir>] [--help]
```

## Description

A maintainer hands a consumer the current branch's CLI and analyzers without waiting for NuGet: one command packs both packages at one dev version, points the consumer at them, and restores its tool. The dev version is the manifest release with a `-dev.<yyyyMMddHHmmss>` suffix in UTC, so each run names the build it packed.

The command packs `LiteSpec.Cli` as the top-level tool package, the framework-dependent runtime package, and the native package of the machine the command runs on, and `LiteSpec.Analyzers`, into the feed, which defaults to `.agents/local-feed` under the consumer. It adds a `litespec-local` source to the consumer's `nuget.config`, keeping the other sources, and creates the config when the consumer holds none. It sets the `LiteSpec.Cli` version in `.config/dotnet-tools.json`, creating the manifest when the consumer holds none, and sets `LiteSpec.Analyzers` in every `Directory.Packages.props` under the consumer that declares it. A project file that references the analyzers directly keeps its version, and the command names each such file so the maintainer moves it by hand. It then runs `dotnet tool restore` in the consumer.

The command prints every file it changed. A broken tool manifest or NuGet config fails the run before the first pack, so a slow pack never runs for a consumer the command cannot finish.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--consumer <path>` | none, required | Selects the consumer folder holding `.litespec/project.yaml`. |
| `--feed <dir>` | `.agents/local-feed` under the consumer | Selects the package feed folder; a relative path resolves under the consumer. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The consumer was moved onto the packed build. |
| `1` | A publish step failed: the feed could not be created, a pack failed, a file could not be written, or the restore failed. |
| `2` | The usage is invalid, or the consumer, the release, or a consumer file is unreadable. |

## Examples

Hand a consumer the current branch's build:

```bash
litespec-maintain publish-local --consumer ../consumer
```

Publish into another feed folder:

```bash
litespec-maintain publish-local --consumer ../consumer --feed ../consumer/.agents/next-feed
```

## Underneath

The packs reuse the release settings: the `Release` configuration, `-p:Version` at the dev version, the top-level tool package, the framework-dependent runtime package, and the native package of the machine's runtime identifier. The restore runs `dotnet tool restore` in the consumer. Nothing beneath this command is separately runnable.
