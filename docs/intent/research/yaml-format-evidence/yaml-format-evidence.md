# Can Editors Check YAML Against LiteSpec Schemas

LiteSpec keeps its YAML files, such as records and project settings, and each file names the JSON Schema it must match. This research record asks whether an editor can check those YAML files against their schemas, and which JSON Schema drafts the editor's checker must read. It uses the YAML language server as the checker. It matters because an editor that checks the schema shows errors while the file is being written, before the build runs.

## Findings

Editors can check a LiteSpec YAML file against its schema [language server README][yaml-language-server]. The YAML language server reads a schema named by a modeline comment at the top of the file. It validates draft 2020-12, which every LiteSpec schema declares. The YAML format needs no bridge to JSON [YAML specification][yaml-spec].

The server validates `draft-04`, `draft-07`, `2019-09`, and `2020-12` [language server README][yaml-language-server]. All 8 files under `schemas/` declare draft 2020-12 on their first line [project schema][project-schema] [record schema][record-schema]. The project schema uses `dependentRequired`, a keyword from 2019-09. The record schema uses `$defs`. The record infers that draft 2020-12 support covers both keywords.

The server README allows a schema definition in JSON or in YAML [language server README][yaml-language-server]. The record infers that the server supports a LiteSpec schema written in YAML.

YAML 1.2.2 states that its primary focus was to make YAML a strict superset of JSON [YAML specification][yaml-spec]. The record infers that the YAML format needs no bridge to JSON.

The tools use two parsers to read YAML [YamlDotNet listing][yamldotnet-package] [yaml license][yaml-npm-license]. YamlDotNet 18.1.0 is under the MIT license. The `yaml` npm package 2.9.1 is under the ISC license.

## Source Notes

The record read the YAML language server README in full on 10 November 2026 [language server README][yaml-language-server].

The record read the YAML 1.2.2 page on 10 November 2026, covering only its first 100,000 characters, which include the superset statement in section 1.2 but not the data-model sentence [YAML specification][yaml-spec].

The record read the YamlDotNet license from its NuGet listing at version 18.1.0 [YamlDotNet listing][yamldotnet-package].

The record read the `yaml` license from the GitHub LICENSE file on the main branch, which holds the ISC license but is not the 2.9.1 release tag [yaml license][yaml-npm-license]. The npm package page returned an error on the read, so the record did not check that the version matches.

The record read the [project schema][project-schema] on 10 November 2026 and checked its draft declaration on the first line.

The record read the [record schema][record-schema] on 10 November 2026 and checked its draft declaration on the first line.

[yaml-language-server]: https://github.com/redhat-developer/yaml-language-server "Read on 2026-11-10"
[yaml-spec]: https://yaml.org/spec/1.2.2/ "Read on 2026-11-10"
[yamldotnet-package]: https://www.nuget.org/packages/YamlDotNet/18.1.0 "Read on 2026-11-10"
[yaml-npm-license]: https://github.com/eemeli/yaml/blob/main/LICENSE "Read on 2026-11-10"
[project-schema]: ../../../../schemas/litespec-project.schema.yaml "Read on 2026-11-10"
[record-schema]: ../../../../schemas/record.schema.yaml "Read on 2026-11-10"
