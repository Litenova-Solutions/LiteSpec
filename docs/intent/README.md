# Overview

## Intent

The Intent records hold the research and decisions behind LiteSpec's provisions, in the shapes a consumer uses. They inform the provisions and settle nothing themselves: the provision remains the only obligation, and these records are never selected or applied as contract.

## Research and Decisions

- Research (`research/`): the seventeen research records behind this release.
  - [Intent Hierarchy Evidence](research/intent-hierarchy-evidence.md): which Intent links LiteSpec requires and checks, and the outside comparison and measured consumer counts behind decisions 0001 to 0008.
  - [Behavior Containment Evidence](research/behavior-containment-evidence.md): how Behavior records find their parent.
  - [Cross-Layer Trace Evidence](research/cross-layer-trace-evidence.md): where the trace from a goal to a test breaks.
  - [Field Naming Evidence](research/field-naming-evidence.md): where one field name carries two concepts.
  - [Established Traceability Practice](research/established-traceability-practice.md): what established practice requires of hierarchy.
  - [Book Outline Evidence](research/book-outline-evidence.md): where the book outline invents a parent.
  - [Check Coverage Evidence](research/check-coverage-evidence.md): which links the checker enforces and which none reads.
  - [Research Conclusion Evidence](research/research-conclusion-evidence.md): where a research conclusion lives today.
  - [Comparable Rationale Practice](research/comparable-rationale-practice.md): how comparable projects keep rationale beside rules.
  - [Naming Grammar Evidence](research/naming-grammar-evidence.md): the 248-field inventory, the collisions and the word classes behind `decision/0011-one-grammar-for-names-and-values`, `decision/0012-use-cases-read-their-settings` and `decision/0018-one-record-language`.
  - [Lower Layers Practice](research/lower-layers-practice.md): the outside review and the measured consumer counts behind `decision/0014-one-exception-record`, `decision/0015-automations-and-sagas`, `decision/0016-typed-contracts-and-privacy` and `decision/0017-proof-that-holds`.
  - [Record Format Evidence](research/record-format-evidence.md): the format comparison and the measured consumer counts behind records in YAML.
  - [Native Command Line Evidence](research/native-command-line-evidence.md): the trial build, the distribution runs and the timings behind the native command line.
  - [Schema Evaluator Evidence](research/schema-evaluator-evidence.md): the licence terms, the library survey and the message comparison behind LiteSpec's own schema evaluator.
  - [YAML Format Evidence](research/yaml-format-evidence.md): whether editors check a YAML file against its schema, behind `decision/0023-every-handwritten-file-in-yaml`.
  - [Layer Claim Evidence](research/layer-claim-evidence.md): which record layers a project can leave out, behind `decision/0027-projects-claim-record-layers`.
  - [Agent Testing Evidence](research/agent-testing-evidence.md): how agent-written tests fail and what guards them, behind `decision/0032-one-mock-rule-for-every-stack`, `decision/0033-agents-never-weaken-proving-tests` and `decision/0034-measure-mutation-before-gating`.
- Decisions (`decisions/`): the thirty-one decision records this release settles, written beside these research records. Decisions 0001 to 0009 record the hierarchy, naming and book shape; decisions 0011, 0012 and 0014 to 0018 record the record language and the lower-layer designs, with decision 0014 holding the departure record; decision 0020 records records in YAML with explanation files; decision 0021 records the native command line and its installs; decision 0022 records LiteSpec's own schema evaluator. Decisions 0023 to 0035 record the 5.0.0 changes:
  - [Every Hand-Written File in YAML](decisions/0023-every-handwritten-file-in-yaml.md): every file a person edits is YAML, and every exchanged file stays JSON.
  - [Keep the UI Source Lock](decisions/0024-keep-the-ui-source-lock.md): `litespec model render` writes the UI source lock.
  - [Remove Fields Nothing Reads](decisions/0025-remove-fields-nothing-reads.md): fields no tool reads leave their files, and the toolchain pins stay.
  - [One Name for Each Concept](decisions/0026-one-name-for-each-concept.md): each concept has one name, and a word moves only where it named another concept.
  - [Projects Claim the Record Layers They Use](decisions/0027-projects-claim-record-layers.md): the `layers` map replaces the conformance level.
  - [One Default and Two Options per Pattern](decisions/0028-one-default-and-two-pattern-options.md): a pattern offers its Default and at most two options.
  - [Delete the Unwired Language Checker](decisions/0029-delete-the-unwired-language-checker.md): the glossary language checker is removed.
  - [Jobs Extension Limited to Scheduled and Manual Work](decisions/0030-limit-the-jobs-extension.md): the jobs extension drops the ingest pipeline and the workflow-engine subjects.
  - [Remove Only the Timing Variable](decisions/0031-remove-only-the-timing-variable.md): `LITESPEC_TIMING` is removed, and the editor option and the redirect Worker stay.
  - [One Mock Rule for Every Stack](decisions/0032-one-mock-rule-for-every-stack.md): one mock rule holds for every stack, and the coordination test rule is removed.
  - [Agents Never Weaken Proving Tests](decisions/0033-agents-never-weaken-proving-tests.md): a changed proving test is reported as an advisory finding.
  - [Measure Mutation Testing, Add No Gate](decisions/0034-measure-mutation-before-gating.md): the mutation run is measured once, and no gate or tool is added.
  - [Bind the Docs to the Tools and the Example](decisions/0035-bind-docs-to-tools-and-example.md): `validate` checks each tool fact the docs cite, and the reference tables are generated.
