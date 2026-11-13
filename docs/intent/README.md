# Overview

## Intent

The Intent records hold the research and decisions behind LiteSpec's provisions, in the shapes a consumer uses. They inform the provisions and settle nothing themselves: the provision remains the only obligation, and these records are never selected or applied as contract.

## Research and Decisions

- Research (`research/`): the seventeen research records behind this release.
  - [Intent Hierarchy Evidence](research/intent-hierarchy-evidence/intent-hierarchy-evidence.md): which Intent links LiteSpec requires and checks, and the outside comparison and measured consumer counts behind decisions 0001 to 0008.
  - [Behavior Containment Evidence](research/behavior-containment-evidence/behavior-containment-evidence.md): how Behavior records find their parent.
  - [Cross-Layer Trace Evidence](research/cross-layer-trace-evidence/cross-layer-trace-evidence.md): where the trace from a goal to a test breaks.
  - [Field Naming Evidence](research/field-naming-evidence/field-naming-evidence.md): where one field name carries two concepts.
  - [Established Traceability Practice](research/established-traceability-practice/established-traceability-practice.md): what established practice requires of hierarchy.
  - [Book Outline Evidence](research/book-outline-evidence/book-outline-evidence.md): where the book outline invents a parent.
  - [Check Coverage Evidence](research/check-coverage-evidence/check-coverage-evidence.md): which links the checker enforces and which none reads.
  - [Research Conclusion Evidence](research/research-conclusion-evidence/research-conclusion-evidence.md): where a research conclusion lives today.
  - [Comparable Rationale Practice](research/comparable-rationale-practice/comparable-rationale-practice.md): how comparable projects keep rationale beside rules.
  - [Naming Grammar Evidence](research/naming-grammar-evidence/naming-grammar-evidence.md): the 248-field inventory, the collisions and the word classes behind `decision/0011-one-grammar-for-names-and-values`, `decision/0012-use-cases-read-their-settings` and `decision/0018-one-record-language`.
  - [Lower Layers Practice](research/lower-layers-practice/lower-layers-practice.md): the outside review and the measured consumer counts behind `decision/0014-one-exception-record`, `decision/0015-automations-and-sagas`, `decision/0016-typed-contracts-and-privacy` and `decision/0017-proof-that-holds`.
  - [Record Format Evidence](research/record-format-evidence/record-format-evidence.md): the format comparison and the measured consumer counts behind records in YAML.
  - [Native Command Line Evidence](research/native-command-line-evidence/native-command-line-evidence.md): the trial build, the distribution runs and the timings behind the native command line.
  - [Schema Evaluator Evidence](research/schema-evaluator-evidence/schema-evaluator-evidence.md): the licence terms, the library survey and the message comparison behind LiteSpec's own schema evaluator.
  - [YAML Format Evidence](research/yaml-format-evidence/yaml-format-evidence.md): whether editors check a YAML file against its schema, behind `decision/0023-every-handwritten-file-in-yaml`.
  - [Layer Claim Evidence](research/layer-claim-evidence/layer-claim-evidence.md): which record layers a project can leave out, behind `decision/0027-projects-claim-record-layers`.
  - [Agent Testing Evidence](research/agent-testing-evidence/agent-testing-evidence.md): how agent-written tests fail and what guards them, behind `decision/0032-one-mock-rule-for-every-stack`, `decision/0033-agents-never-weaken-proving-tests` and `decision/0034-measure-mutation-before-gating`.
- Decisions (`decisions/`): the forty decision records this release settles, written beside these research records. Decisions 0001 to 0009 record the hierarchy, naming and book shape; decisions 0011, 0012 and 0014 to 0018 record the record language and the lower-layer designs, with decision 0014 holding the departure record; decision 0020 records records in YAML with explanation files; decision 0021 records the native command line and its installs; decision 0022 records LiteSpec's own schema evaluator. Decisions 0023 to 0035 record the 5.0.0 changes, decision 0036 records the 5.1.0 pin change, decisions 0037 to 0040 record the 5.2.0 agent protocol, rules retrieval, skill and decision kinds, decision 0041 records the 5.3.0 vision record, decision 0042 records the 5.3.0 folder for each named record, and decisions 0043 and 0044 record the 5.3.0 explanation sections and the failure cases of a problem:
  - [Every Hand-Written File in YAML](decisions/0023-every-handwritten-file-in-yaml/0023-every-handwritten-file-in-yaml.md): every file a person edits is YAML, and every exchanged file stays JSON.
  - [Keep the UI Source Lock](decisions/0024-keep-the-ui-source-lock/0024-keep-the-ui-source-lock.md): `litespec model render` writes the UI source lock.
  - [Remove Fields Nothing Reads](decisions/0025-remove-fields-nothing-reads/0025-remove-fields-nothing-reads.md): fields no tool reads leave their files, and the toolchain pins stay.
  - [One Name for Each Concept](decisions/0026-one-name-for-each-concept/0026-one-name-for-each-concept.md): each concept has one name, and a word moves only where it named another concept.
  - [Projects Claim the Record Layers They Use](decisions/0027-projects-claim-record-layers/0027-projects-claim-record-layers.md): the `layers` map replaces the conformance level.
  - [One Default and Two Options per Pattern](decisions/0028-one-default-and-two-pattern-options/0028-one-default-and-two-pattern-options.md): a pattern offers its Default and at most two options.
  - [Delete the Unwired Language Checker](decisions/0029-delete-the-unwired-language-checker/0029-delete-the-unwired-language-checker.md): the glossary language checker is removed.
  - [Jobs Extension Limited to Scheduled and Manual Work](decisions/0030-limit-the-jobs-extension/0030-limit-the-jobs-extension.md): the jobs extension drops the ingest pipeline and the workflow-engine subjects.
  - [Remove Only the Timing Variable](decisions/0031-remove-only-the-timing-variable/0031-remove-only-the-timing-variable.md): `LITESPEC_TIMING` is removed, and the editor option and the redirect Worker stay.
  - [One Mock Rule for Every Stack](decisions/0032-one-mock-rule-for-every-stack/0032-one-mock-rule-for-every-stack.md): one mock rule holds for every stack, and the coordination test rule is removed.
  - [Agents Never Weaken Proving Tests](decisions/0033-agents-never-weaken-proving-tests/0033-agents-never-weaken-proving-tests.md): a changed proving test is reported as an advisory finding.
  - [Measure Mutation Testing, Add No Gate](decisions/0034-measure-mutation-before-gating/0034-measure-mutation-before-gating.md): the mutation run is measured once, and no gate or tool is added.
  - [Bind the Docs to the Tools and the Example](decisions/0035-bind-docs-to-tools-and-example/0035-bind-docs-to-tools-and-example.md): `validate` checks each tool fact the docs cite, and the reference tables are generated.
  - [Pin Third-Party Packages to Their Major](decisions/0036-pin-third-party-packages-to-their-major/0036-pin-third-party-packages-to-their-major.md): the manifest pins the major of each third-party package, and exact versions only where a check depends on them.
  - [Generate the Agent Protocol](decisions/0037-generate-the-agent-protocol/0037-generate-the-agent-protocol.md): one template generates the agent protocol region in `AGENTS.md`, on the site and in `llms.txt`, and `model check` reports drift.
  - [Narrow Rules Output per File and Change Set](decisions/0038-narrow-rules-output-per-file-and-change-set/0038-narrow-rules-output-per-file-and-change-set.md): `rules` collapses a record's provisions on a code path, answers for a change set and a provision ID, and evaluates `when` conditions.
  - [Ship a LiteSpec Skill](decisions/0039-ship-a-litespec-skill/0039-ship-a-litespec-skill.md): `skill add` writes generated copies of one release-neutral skill for Claude Code, OpenCode and `.agents`, at project or global scope.
  - [Require Decisions Only for Six Kinds of Choice](decisions/0040-require-decisions-only-for-six-kinds-of-choice/0040-require-decisions-only-for-six-kinds-of-choice.md): a technical decision records a choice of one of six kinds, code may follow a project convention, and generated indexes group decisions and departures.
  - [Record Product Vision as a Vision Record](decisions/0041-record-product-vision/0041-record-product-vision.md): a vision states where the product heads, decisions and requirements cite it, and nothing builds from it.
  - [Keep Each Named Record in Its Own Folder](decisions/0042-record-named-folders/0042-record-named-folders.md): each named record sits in a folder named for it, beside its explanation file.
  - [Use Plain Noun Sections and an Opening Paragraph](decisions/0043-use-plain-noun-sections/0043-use-plain-noun-sections.md): each record type has one fixed list of plain noun sections, and each explanation file opens with one paragraph.
  - [State Each Problem by Its Failure Cases](decisions/0044-state-problems-by-failure-case/0044-state-problems-by-failure-case.md): a problem lists each failure case with one resolution criterion for each, keeps feasibility out of its evidence and claims no more than its evidence shows.
