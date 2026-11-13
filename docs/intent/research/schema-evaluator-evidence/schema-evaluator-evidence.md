# Schema Evaluator Evidence

LiteSpec checks its YAML records and project files against JSON Schema, and a schema evaluator is the code that runs those checks. This research record decides whether LiteSpec keeps a third-party JSON Schema library, or runs its own evaluator. It also tests that evaluator against the library and the official test suite. The licence change matters because the native command line compiled the library into each of its executables. The decision then affects every build that the native CLI produces.

## Findings

From version 9.0.0, the JsonSchema.Net library charges a maintenance fee for its binaries. No maintained free library fits Native AOT without extra dependencies. So LiteSpec evaluates JSON Schema itself. Its evaluator printed the library's messages on 9,828 real and mutated instances, except where the library reported the rejected shape of a passing composite. It passed 757 of the 761 applicable cases of the JSON Schema Test Suite.

Read on 9 November 2026, every JsonSchema.Net release from 9.0.0 on ships under the Open Source Maintenance Fee agreement. It does not ship under the MIT licence. The same holds for Json.More.Net and JsonPointer.Net. Version 9.0.0 is dated 1 February 2026. Version 8.0.5, dated 28 December 2025, is the last MIT release. The record gives no confidence for these licence claims.

The fee applies to a user with annual gross revenue of US$10,000 or more who uses the published binaries in revenue-generating activities. The agreement exempts binaries compiled from the MIT source. LiteSpec's tools validated every schema through version 9.6.0. The native CLI compiled the library into each executable. The record gives no confidence for these claims.

The record finds 2 free libraries that remain. LateApexEarlySpeed.Json.Schema 4.2.4 is BSD-3-Clause and maintained. It has about 315,000 downloads. It targets only netstandard2.1, depends on Microsoft.Extensions.Http 3.1.0 for remote references, and claims no Native AOT support. FormFinch.JsonSchemaValidation 1.2.0 has 4 releases and about 1,400 downloads. The record gives no confidence for these claims.

LiteSpec's 7 schemas use about 25 keywords of draft 2020-12. Every `$ref` is a local `#/$defs/` pointer. None uses a dynamic reference, an anchor or an unevaluated keyword. LiteSpec already renders every message itself from the library's result tree. The record gives no confidence for these claims.

LiteSpec's evaluator implements the draft 2020-12 assertions and applicators. It does not implement dynamic references, anchors, `unevaluatedProperties`, `unevaluatedItems` or `contentSchema`. The record gives no confidence for this claim.

A comparison harness ran the evaluator and the library-based renderer over 378 real instances. The instances are records, project files, glossaries, catalogs, locks and the manifest. The harness also ran 25 seeded mutations of each instance, which makes 9,828 cases. Of those cases, 8,156 fail. The output matched message for message in 9,666 cases. The record gives no confidence for these counts.

The other 162 cases exposed one defect in the library-based renderer. When a schema fails for one reason, the renderer also reports the rejected shape of a `oneOf` or `anyOf` that passed. One example is a missing `url` on a research source that names its `path`. The record reproduced that defect in the evaluator, and then all 9,828 cases matched. The record gives no confidence for these claims.

The evaluator ran 761 draft 2020-12 cases from the JSON Schema Test Suite at commit `245a45a`. It left out 49 cases that use remote references, `$id`, anchors or unevaluated keywords. It agreed with the expected result in 757 cases. The other 4 use the ECMAScript Unicode property name `\p{Letter}`, which a .NET regular expression writes as `\p{L}`. No LiteSpec schema uses either form. The record gives no confidence for these counts.

The library-based renderer had passed 21 suite cases without a message. The evaluator reports those cases. A `false` subschema anywhere except in `additionalProperties` rejects the value without a message. The record gives no confidence for this claim.

## Source Notes

The record read 5 sources on 9 November 2026. The first is the licence file of JsonSchema.Net 9.6.0, read for the fee terms and the exemption for self-compiled binaries. The licence of every other release was read from its package metadata. The second and third are the NuGet listings of LateApexEarlySpeed.Json.Schema and FormFinch.JsonSchemaValidation. The record read them for licence, release history, downloads and dependencies.

The fourth source is the official JSON Schema Test Suite, read for its draft 2020-12 cases. The fifth is the evaluator itself (`apps/model/src/LiteSpec.Standards/Schemas/SchemaEvaluator.cs`). The case counts and the comparison results are the researcher's own measurements.

The record names no disagreement between sources. It names no claim that it could not confirm.
