# Own Schema Evaluator

LiteSpec checks its files against JSON Schema, a standard way to describe the shape of a document. The schema evaluator is the code that runs those checks and writes the messages a person reads. This decision moves that code out of a third-party library and into LiteSpec, so LiteSpec owns the checker and pays no license fee for it.

## Context

The tools validate every schema through JsonSchema.Net 9.6.0, and the native CLI compiles that library into each executable it ships. Every JsonSchema.Net release from 9.0.0 on, along with Json.More.Net and JsonPointer.Net, ships under the Open Source Maintenance Fee agreement instead of the MIT license. The agreement charges a fee to a user with US$10,000 or more in annual gross revenue who uses the published binaries in revenue-generating activities. A binary compiled from the MIT source is exempt.

Version 8.0.5 is the last MIT release. The two free alternatives fit poorly. LateApexEarlySpeed.Json.Schema is maintained under BSD-3-Clause, but it targets only netstandard2.1, pulls in Microsoft.Extensions.Http 3.1.0 for remote references, and claims no Native AOT support. FormFinch.JsonSchemaValidation has 4 releases and about 1,400 downloads.

LiteSpec's 7 schemas use about 25 keywords of draft 2020-12. Every `$ref` is a local `#/$defs/` pointer, and no schema uses a dynamic reference, an anchor or an unevaluated keyword. LiteSpec already writes every message itself from the library's result tree.

## Options

LiteSpec compared 4 options for the schema evaluator. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| LiteSpec's own evaluator for the keywords its schemas use | No package and no fee, Native AOT support by design, and each message written where the failure is found. | LiteSpec maintains the evaluator, and any keyword it leaves out stays unavailable. |
| Pin JsonSchema.Net 8.0.5 | The full vocabulary with a small change. | Frozen at December 2025 with no later fixes, and every 9.x release must be held back. |
| Build json-everything from its MIT source | The full vocabulary with current fixes and no fee. | The repository carries third-party source, and every update to it. |
| Adopt LateApexEarlySpeed.Json.Schema | A maintained BSD-3-Clause library with the full vocabulary. | It targets netstandard2.1, pulls in an old dependency, claims no Native AOT support and needs a new message renderer. |

## Decision

LiteSpec evaluates JSON Schema itself, in `SchemaEvaluator.cs` in the shared library. The evaluator implements draft 2020-12 without dynamic references, anchors, `unevaluatedProperties`, `unevaluatedItems` and `contentSchema`. The keyword gate rejects any schema that uses one of those.

The evaluator writes each message where it finds the failure, in the shapes the checks already print. A `false` subschema reports the value as not allowed, a property-name failure names the property, and a percent-encoded reference resolves.

## Consequences

JsonSchema.Net, Json.More.Net and JsonPointer.Net leave the repository and every shipped binary. A schema that needs one of the keywords the evaluator leaves out cannot pass the keyword gate.

A passing `oneOf` or `anyOf` reports nothing from the shapes it rejected. The evaluator reports a `false` subschema anywhere, not only under `additionalProperties`, so a value that a `false` subschema rejects always gets a message.

## Evidence

`research/schema-evaluator-evidence` holds the licence terms, the library survey, the message comparison and the JSON Schema Test Suite results behind this choice, with high confidence. The research is the researcher's own measurement, read on 2026-11-09.

In the message comparison, the evaluator matched the library's messages in 9,666 of 9,828 real and mutated instances. In the other 162, the library reported the rejected shape of a passing `oneOf` or `anyOf`, which is a defect in the library's renderer. The research states that the match became complete once that defect was reproduced in the evaluator, which differs from the choice above.

Against the JSON Schema Test Suite, the evaluator agreed with the expected result in 757 of the 761 applicable draft 2020-12 cases. The 4 misses use the ECMAScript property name `\p{Letter}`, which a .NET regular expression writes as `\p{L}`, and no LiteSpec schema uses either form. The library-based renderer had passed 21 suite cases without a message, and the evaluator reports them.

## Revisit Conditions

This decision reopens when a schema needs a keyword the evaluator leaves out, or when a maintained free evaluator fits Native AOT with no extra dependencies.

The evaluator leaves out dynamic references, anchors and the unevaluated keywords. A schema that needs one of them is the case that would change the choice.
