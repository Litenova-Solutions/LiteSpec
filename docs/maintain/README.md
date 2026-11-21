# Overview

The Maintain section documents the `litespec-maintain` command line: the three commands that keep the LiteSpec standard itself consistent. A maintainer opens this index to find the command that checks the standard, rebuilds the provision index, or compares releases.

On a command page, Name and Synopsis give the quick form; Description and Examples give the detail.

## Run the Maintainer Tool

In the LiteSpec repository, run `dotnet run --project apps/maintain/src/LiteSpec.Maintain -- <command>`. The tool runs in this repository only and is never packed for consumers.

It reads the checkout it runs from, or the `--root` path each command accepts. It needs the .NET 10 SDK and nothing else: no Node runtime. Set `LITESPEC_HOME` to point it at another copy.

Every command accepts `--help`, and `validate` accepts `--format json` for callers that read the result instead of the text.

## Maintain the Standard

- [Validate Standards](validate.md): checks the standards pages, templates, and identifiers against the authoring contract.
- [Generate Provisions](provisions.md): rebuilds the provision index from the active standards.
- [Generate Provision Changelog](changelog.md): writes the provision changes since a git ref into the changelog.
- [Add a Stack or an Extension](../guide/add-a-stack-or-extension.md): adds a stack folder or an extension page to the release manifest.
