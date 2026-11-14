# Tie Each Research Finding to Its Source

A research record collects what outside sources establish about one question. This decision ties each finding to the source that states it and marks each inference as an inference, so a reader knows what to recheck.

## Context

Findings often carried prose attributions, and one named no listed source. Other findings named no source at all. The source checks passed when each key was linked once anywhere in the file, so a closing sentence that linked every key satisfied them while no finding was tied to a source.

The read date was treated as the date a rule applied. The provision that asked for it said that the read date shows when the outside fact was true, but a read date shows only when someone read the source.

Confidence came as high, medium or low, and the provision defined none of the three. The labels explained nothing about what a source says.

Open questions sat in Source Notes, mixed with what each source covers, and a constraint copied them into its own open points.

## Options

LiteSpec compared 3 options for how a finding states its strength. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Each finding cites its key and says whether the source states it | A reader sees what the source says and what the writer infers. | Every finding is rewritten, and each inference gets its own sentence. |
| Three defined confidence levels | One short label per finding. | A label does not say what the source states. |
| Structured findings with claim, source and kind fields | A check reads each field. | A finding's conditions have no field to sit in. |

## Decision

Each paragraph under Findings states one claim with its conditions, cites its source key with a reference link, and says whether the source states the claim or the writer infers it. A sentence that states an inference names it as one.

Open Questions is its own section, and it holds what the sources do not establish, each as a question naming what to investigate. Source Notes holds what each source covers, the audience it addresses and where sources disagree.

The conclusion states in one sentence what the sources establish, with the conditions the findings carry. A consequence for the product belongs to the constraint, decision or requirement that cites the research.

## Consequences

`finding-source-missing` reports a Findings paragraph with no source link. It closes the gap that the three existing source checks leave open. Each source may carry `publishedOn`, the date it states it was published. The book shows it beside `readOn`.

`litespec/core/intent#state-each-research-finding-with-its-source` is required. Its review question asks whether a finding states an inference as a source statement, or drops a condition the source gives. Research records answer only their own question, and a finding that answers another question moves to another record.

## Revisit Conditions

This decision holds until consumers report that most findings are written as inferences, so the sentence adds nothing to the claim. It also holds until a finding cannot hold its conditions in one paragraph. Either report reopens the choice.
