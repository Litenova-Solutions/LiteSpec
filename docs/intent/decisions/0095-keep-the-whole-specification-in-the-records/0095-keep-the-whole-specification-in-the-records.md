# Keep the Whole Specification in the Records

An agent builds the product from the YAML records alone, and a person reads the same specification in the explanation beside each record. This decision makes the records the complete specification and each explanation a restatement of it, so no build fact lives only in prose.

## Context

Several fixed sections asked for facts no field held. A problem's Failure Cases, a constraint's Rules, research Findings, a screen's States, an entry point's Authentication and a use case's Flow were written only as prose. An agent reading only the YAML missed build facts such as session timeouts, screen states, legal retention rules and idempotent replies, and no check saw them.

The gap was measured on LiteCount. Constraints hold 2,259 words of YAML and 9,866 words of explanation; research holds 7,396 and 31,441; screens hold 1,890 and 10,509; entry points hold 2,064 and 11,395.

## Options

LiteSpec compared 3 options for where the specification lives. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| The records state the whole specification, and each explanation file restates it for people | An agent builds from the YAML alone, every fact has one primary home a check can read, and people still get an elaborated version with context and examples. | Each type gains fields, every explanation repeats its record and can drift, and authors update both in one change. |
| Keep fixed sections that carry facts only in prose | No new fields, and prose carries nuance easily. | An agent reading the records misses build facts such as session timeouts, screen states and legal rules, and no check can see them. |
| Drop explanation files and keep only the records | One copy of every fact and nothing to drift. | People lose the context, reasons and examples that make a specification readable cold. |

## Decision

The YAML record files are the primary and complete source of truth. Every fact an agent needs to build the product stands in a record field.

An explanation file is the secondary source. It states the same specification as its record, restated and elaborated for a person: context, reasons, an example. Repeating a field value in the explanation is allowed and expected.

An explanation file adds no fact that the records lack and contradicts no field. When the two disagree, the record wins and the explanation is a defect to fix.

Keeping them in step is the author's discipline: a change to a record updates its explanation in the same change, and the reverse.

Explanation files stay optional unless the project lists the layer in `explanations.layers` in `.litespec/project.yaml`. No record type requires one on its own any more.

The fixed H2 sections of each record type stay, as the human reading structure. Each section now explains named fields; the section table on the writing page pairs every section with its fields.

## Consequences

The records state the whole specification under `litespec/core/model#keep-the-whole-specification-in-the-records`, and `litespec/core/writing#explain-the-same-specification-the-records-state` replaces `litespec/core/writing#interpret-fields-in-explanations-never-restate-them`, so an explanation file restates and elaborates its record, adds no fact, and contradicts no field. `litespec/core/writing#update-a-record-and-its-explanation-together` requires both to change in one commit.

Intent records gain fields. A problem states `failureCases`, `workaround`, `cost` and `resolutionCriteria`. A constraint states `scope`, `obligations`, `productEffect` and `openPoints`. Research states `findings` and `openQuestions`, and each source may carry a `note`. A feature states `successCriteria` and `excludes`. An actor states `responsibilities` and `knowledge`. A role states `limits`. A decision states `context`.

Behavior records gain fields. A module states `responsibilities` and `excludes`. An aggregate states `limits`. A use case states `steps` and `edgeCases`. A rule states its `rationale`. Module and integration events state `compatibility`. An entry point states `audience`, `session`, `reauthenticationTriggers` and `limits`. A screen states `task` and `states`. A permission set states `excludes`.

The section table on the writing page pairs each explanation section with the fields it explains. `litespec model check` reports `explanation-section-unbacked`, `constraint-obligation-source-unknown` and `research-finding-source-unknown`. Research and decision records no longer always need an explanation file. `litespec context` writes records only by default. Claimwick carries every new field.

## Revisit Conditions

This decision reopens when authors routinely leave explanations out of step with their records, or when a fact proves impossible to state in a field.

Either case means the records no longer hold what the decision gave them. The team then decides again whether the specification stays in the records.
