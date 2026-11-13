# Can Editors Check YAML Against LiteSpec Schemas

## Answer

Editors can check a LiteSpec YAML file against its schema, because the YAML language server reads a schema named by a modeline comment at the top of the file (`yaml-language-server`). The server validates draft-04, draft-07, 2019-09 and 2020-12 (`yaml-language-server`), and all eight files under `schemas/` declare draft 2020-12 on their first line. The project schema uses `dependentRequired`, a 2019-09 keyword (`project-schema`), and the record schema uses `$defs` (`record-schema`); draft 2020-12 support covers both. The server's README states that a schema definition may be written in JSON or in YAML (`yaml-language-server`), so a LiteSpec schema written in YAML is supported by the server.

The YAML format itself needs no bridge to JSON. YAML 1.2.2 states that its primary focus was making YAML a strict superset of JSON (`yaml-spec`). Two parsers read YAML for the tools: YamlDotNet 18.1.0 under the MIT license (`yamldotnet-package`) and the `yaml` npm package 2.9.1 under the ISC license (`yaml-npm-license`).

## Notes on Sources

The YAML language server README was read in full on 10 November 2026 (`yaml-language-server`). The YAML 1.2.2 page is long, and the read covered its first 100,000 characters; the superset statement is in section 1.2, which that read included. The data-model sentence was not found in that part, so this record does not claim it. The YamlDotNet license was read from its NuGet listing at version 18.1.0 (`yamldotnet-package`). The `yaml` license was read from the GitHub LICENSE file on the main branch (`yaml-npm-license`), whose text is the ISC license; that file is not the 2.9.1 release tag, and the npm package page returned an error on the read, so the version match is not checked. The schema facts come from the repository files named by key, read on 10 November 2026, and the draft declarations were checked with `head -1 schemas/*.yaml`.
