# Overview

## Intent

The Maintain section documents the `litespec-maintain` command line: the two commands that keep the LiteSpec standard itself consistent, from authoring checks to the generated provision index. A maintainer opens this index to find the command that checks or rebuilds the standard. On a command page, Name and Synopsis give the quick form, and Description and Examples give the detail.

## Run the Maintainer Tool

In the LiteSpec repository, run `dotnet run --project apps/maintain/src/LiteSpec.Maintain -- <command>`. The tool runs in this repository only and is never packed for consumers: it reads the checkout it runs from, or the `--root` path each command accepts. It needs the .NET 10 SDK and nothing else: no Node runtime. Set `LITESPEC_HOME` to point it at another copy.

Every command accepts `--help`, and `validate` accepts `--format json` for a caller that reads the result rather than the text.

## Maintain the Standard

- [Validate Standards](validate.md): checks the standards pages, templates, and identifiers against the authoring contract.
- [Generate Provisions](provisions.md): rebuilds the provision index from the active standards.
