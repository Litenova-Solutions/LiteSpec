# Own Schema Evaluator

## Context

LiteSpec's tools validated every schema through JsonSchema.Net, and the native CLI compiles it into each executable it ships. `research/schema-evaluator-evidence` shows that every JsonSchema.Net release from 9.0.0 on charges a maintenance fee for its binaries to users with US$10,000 or more in annual revenue, which LiteSpec does not adopt, and that neither free library fits: one claims no Native AOT support and pulls in an old dependency tree, the other is four releases old. LiteSpec's schemas use about 25 keywords with local references only, and LiteSpec already writes every message itself.

## Decision

LiteSpec evaluates JSON Schema itself, in `SchemaEvaluator.cs` in the shared library: draft 2020-12 without dynamic references, anchors, `unevaluatedProperties`, `unevaluatedItems` and `contentSchema`, and the keyword gate rejects any of those in a schema. The evaluator writes each message where it finds the failure, in the shapes the checks already print. A passing `oneOf` or `anyOf` reports nothing from the shapes it rejected, a `false` subschema reports the value as not allowed, a property-name failure names the property, and a percent-encoded reference resolves.

## Evidence with Confidence

`research/schema-evaluator-evidence` holds the licence terms, the library survey, the message comparison over 9,828 real and mutated instances and the JSON Schema Test Suite results behind this choice, high confidence: the evaluator printed the library's messages in every case but the library's own defect, and agreed with the test suite in 757 of 761 applicable cases.
