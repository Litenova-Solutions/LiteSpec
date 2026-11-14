# Can Editors Check YAML Against LiteSpec Schemas

LiteSpec keeps its YAML files, such as records and project settings, and each file names the JSON Schema it must match. This research record asks whether an editor can check those YAML files against their schemas, and which JSON Schema drafts the editor's checker must read. It uses the YAML language server as the checker. It matters because an editor that checks the schema shows errors while the file is being written, before the build runs.

## Findings

Editors can check a LiteSpec YAML file against its schema. The YAML language server reads a schema named by a modeline comment at the top of the file. It validates draft 2020-12, which every LiteSpec schema declares. The YAML format needs no bridge to JSON.

The server reads a schema named by a modeline comment at the top of the file. The record gives no confidence for this claim.

The server validates draft-04, draft-07, 2019-09 and 2020-12. All 8 files under `schemas/` declare draft 2020-12 on their first line. The project schema uses `dependentRequired`, a keyword from 2019-09. The record schema uses `$defs`. Draft 2020-12 support covers both. The record gives no confidence for these claims.

The server's README states that a schema definition may be written in JSON or in YAML. So a LiteSpec schema written in YAML is supported by the server. The record gives no confidence for this claim.

YAML 1.2.2 states that its primary focus was to make YAML a strict superset of JSON. So the YAML format needs no bridge to JSON. The record gives no confidence for this claim.

The tools use 2 parsers to read YAML. YamlDotNet 18.1.0 is under the MIT licence. The `yaml` npm package 2.9.1 is under the ISC licence. The record gives no confidence for these claims.

## Source Notes

The record read the YAML language server README in full on 10 November 2026. It read the YAML 1.2.2 page on the same day. The page is long, so the read covered only its first 100,000 characters. The superset statement is in section 1.2, which the read included. The data-model sentence was not in that part, so the record does not claim it.

The YamlDotNet licence was read from its NuGet listing at version 18.1.0. The `yaml` licence was read from the GitHub LICENSE file on the main branch. That text is the ISC licence. The file is not the 2.9.1 release tag. The npm package page returned an error on the read, so the record did not check that the version matches.

The schema facts come from the repository files named in the sources, read on 10 November 2026. The record checked the draft declarations on the first line of each schema file.
