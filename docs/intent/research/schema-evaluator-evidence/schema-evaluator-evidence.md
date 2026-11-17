# Schema Evaluator Evidence

LiteSpec checks its YAML records and project files against JSON Schema, and a schema evaluator is the code that runs those checks. This research record decides whether LiteSpec keeps a third-party JSON Schema library, or runs its own evaluator. It also tests that evaluator against the library and the official test suite. The license change matters because the native command line compiled the library into each of its executables. The decision then affects every build that the native CLI produces.

## Findings

From version 9.0.0, the JsonSchema.Net library charges a maintenance fee for its binaries [JsonSchema.Net license][jsonschema-net-eula]. No maintained free library fits Native AOT without extra dependencies [LateApexEarlySpeed listing][lateapexearlyspeed] [FormFinch listing][formfinch]. The record infers that LiteSpec evaluates JSON Schema itself. Its evaluator printed the library messages on 9,828 real and mutated instances, except where the library reported the rejected shape of a passing composite [evaluator][schema-evaluator]. It passed 757 of the 761 applicable cases of the JSON Schema Test Suite [test suite][test-suite].

Every JsonSchema.Net release from 9.0.0 on ships under the Open Source Maintenance Fee agreement [JsonSchema.Net license][jsonschema-net-eula]. It does not ship under the MIT license. The same holds for Json.More.Net and JsonPointer.Net. Version 9.0.0 is dated 1 February 2026. Version 8.0.5, dated 28 December 2025, is the last MIT release.

The fee applies to a user with annual gross revenue of US$10,000 or more who uses the published binaries in revenue-generating activities [JsonSchema.Net license][jsonschema-net-eula]. The agreement exempts binaries compiled from the MIT source. LiteSpec tools validated every schema through version 9.6.0 [evaluator][schema-evaluator]. The native CLI compiled the library into each executable.

The record finds two free libraries that remain [LateApexEarlySpeed listing][lateapexearlyspeed] [FormFinch listing][formfinch]. `LateApexEarlySpeed.Json.Schema` 4.2.4 is BSD-3-Clause and maintained. It has about 315,000 downloads. It targets only `netstandard2.1`, depends on `Microsoft.Extensions.Http` 3.1.0 for remote references, and claims no Native AOT support. `FormFinch.JsonSchemaValidation` 1.2.0 has 4 releases and about 1,400 downloads.

LiteSpec 7 schemas use about 25 keywords of draft 2020-12 [evaluator][schema-evaluator]. Every `$ref` is a local `#/$defs/` pointer. None uses a dynamic reference, an anchor, or an unevaluated keyword. LiteSpec already renders every message itself from the library result tree.

The LiteSpec evaluator implements the draft 2020-12 assertions and applicators [evaluator][schema-evaluator]. It does not implement dynamic references, anchors, `unevaluatedProperties`, `unevaluatedItems`, or `contentSchema`.

A comparison harness ran the evaluator and the library-based renderer over 378 real instances [evaluator][schema-evaluator]. The instances are records, project files, glossaries, catalogs, locks, and the manifest. The harness also ran 25 seeded mutations of each instance, which makes 9,828 cases. Of those cases, 8,156 fail. The output matched message for message in 9,666 cases.

The other 162 cases exposed one defect in the library-based renderer [evaluator][schema-evaluator]. When a schema fails for one reason, the renderer also reports the rejected shape of a `oneOf` or `anyOf` that passed. One example is a missing `url` on a research source that names its `path`. The researcher reproduced that defect in the evaluator, and then all 9,828 cases matched.

The evaluator ran 761 draft 2020-12 cases from the JSON Schema Test Suite at commit `245a45a` [test suite][test-suite]. It left out 49 cases that use remote references, `$id`, anchors, or unevaluated keywords. It agreed with the expected result in 757 cases. The other 4 use the ECMAScript Unicode property name `\p{Letter}`, which a .NET regular expression writes as `\p{L}`. No LiteSpec schema uses either form.

The library-based renderer had passed 21 suite cases without a message [test suite][test-suite]. The evaluator reports those cases. A `false` subschema anywhere except in `additionalProperties` rejects the value without a message.

## Source Notes

The record read the JsonSchema.Net 9.6.0 license for the fee terms and the exemption for self-compiled binaries [JsonSchema.Net license][jsonschema-net-eula]. It read the license of every other release from its package metadata.

The record read the LateApexEarlySpeed NuGet listing for license, release history, downloads, and dependencies [LateApexEarlySpeed listing][lateapexearlyspeed].

The record read the FormFinch NuGet listing for license, release history, downloads, and dependencies [FormFinch listing][formfinch].

The record read the official JSON Schema Test Suite for its draft 2020-12 cases [test suite][test-suite].

The [evaluator][schema-evaluator] itself is the fifth source, and the case counts and comparison results are the researcher's own measurements.

[jsonschema-net-eula]: https://www.nuget.org/packages/JsonSchema.Net/9.6.0/License "Read on 2026-11-09"
[lateapexearlyspeed]: https://www.nuget.org/packages/LateApexEarlySpeed.Json.Schema "Read on 2026-11-09"
[formfinch]: https://www.nuget.org/packages/FormFinch.JsonSchemaValidation "Read on 2026-11-09"
[test-suite]: https://github.com/json-schema-org/JSON-Schema-Test-Suite "Read on 2026-11-09"
[schema-evaluator]: ../../../../apps/model/src/LiteSpec.Standards/Schemas/SchemaEvaluator.cs "Read on 2026-11-09"
